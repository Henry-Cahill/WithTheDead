# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 200.6 s, started 2026-10-24 08:38:34.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 4,252 ms |
| the same square again, warm, pass by pass | 2,318 ms |
| logged while building | 4 errors, 116 warnings, 232 edit-mode artefacts |
| spawn | -84.2, 13.8, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 66 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 516 containers, 21 lights, 332 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 15,125 | 5,530 | 5,530 | 4,626 | 3,310,859 | 1,962,847 | 120 | 11,994 | 247 (0) | 5,236 | 4,162 | 1,843 |
| RaidOverhead | 457 | 339 | 339 | 339 | 323,687 | 166,411 | 128 | 1,350 | 0 (0) | 0 | 339 | 0 |
| RaidScenery | 119 | 118 | 118 | 118 | 84,853 | 84,184 | 38 | 337 | 0 (0) | 0 | 73 | 0 |
| **all** | 15,701 | 5,987 | 5,987 | 5,083 | 3,719,399 | 2,213,442 | 217 | 13,681 | 247 (0) | 5,236 | 4,574 | 1,843 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 104 | 4% | 1 | 104 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 39 | 2% | 1 | 39 |
| cemeteries | 4 | 0% | 4 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 14 | 1% | 10 | 3 |
| snow tone | 14 | 1% | 1 | 14 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 2 | 0% | 1 | 2 |
| streets | 33 | 1% | 1 | 33 |
| sidewalks | 28 | 1% | 1 | 28 |
| berms and driveways | 10 | 0% | 1 | 10 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 128 | 6% | 21 | 19 |
| interior plans | 149 | 6% | 7 | 26 |
| interior shell | 719 | 31% | 116 | 24 |
| interior walls | 173 | 7% | 116 | 16 |
| interior doors | 532 | 23% | 116 | 17 |
| cutaway | 70 | 3% | 15 | 10 |
| utility lines | 46 | 2% | 1 | 46 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 8 | 0% | 1 | 8 |
| street lights | 7 | 0% | 1 | 7 |
| signs | 4 | 0% | 1 | 4 |
| street furniture | 18 | 1% | 1 | 18 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 27 | 1% | 1 | 27 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 2 | 0% | 1 | 2 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 6 | 0% | 1 | 6 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 12 | 1% | 15 | 2 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 59 | 3% | 22 | 11 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 34 | 1% | 10 | 6 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 17 | 1% | 1 | 17 |
| (emit the outdoor meshes) | 41 | 2% | 1 | 41 |
| **total** | **2,318** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **59** | cutaway 12 | 0 | 99 | 68 | 95,372 | 50,284 | 3 | 69 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **36** | buildings 10 | 0 | 95 | 64 | 133,759 | 76,841 | 5 | 115 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 163 + 9 | **108** | cutaway 30 | 0 | 99 | 54 | 179,769 | 93,711 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 440 + 51 | **73** | buildings 20 | 0 | 100 | 54 | 94,666 | 50,719 | 0 | 40 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 12 + 1 | **19** | streets 4 | 0 | 67 | 39 | 60,103 | 44,413 | 0 | 2 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 8.4 | 1 | 8.4 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 2.1 | 1 | 2.1 |
| junctions | 1.6 | 1 | 1.6 |
| streets | 8.2 | 1 | 8.2 |
| sidewalks | 8.6 | 1 | 8.6 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.7 | 2 | 4.1 |
| cutaway | 12.2 | 2 | 12.1 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 1.3 | 1 | 1.3 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.2 | 1 | 0.2 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.6 | 1 | 0.6 |
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
| wall drifts | 1.5 | 1 | 1.5 |
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
| yards | 3.7 | 2 | 2.1 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 0.4 | 1 | 0.4 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.3 | 2 | 10.0 |
| cutaway | 10.7 | 2 | 9.7 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.2 | 4 | 0.4 |
| driveway cars | 0.5 | 1 | 0.5 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.1 | 2 | 1.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.2 | 1 | 0.2 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.9 | 1 | 1.9 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 10.7 | 1 | 10.7 |
| water | 15.6 | 1 | 15.6 |
| fields | 8.3 | 1 | 8.3 |
| parking | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.2 | 2 | 1.1 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 0.9 | 1 | 0.9 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 4.6 | 1 | 4.6 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.3 | 2 | 8.3 |
| cutaway | 30.1 | 1 | 30.1 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 3.6 | 1 | 3.6 |
| nature plan | 0.7 | 1 | 0.7 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 6.2 | 1 | 6.2 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.7 | 3 | 0.9 |
| parking lot cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.9 | 1 | 2.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.9 | 1 | 1.9 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.4 | 1 | 1.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 8.7 | 2 | 8.7 |
| snow tone | 1.9 | 1 | 1.9 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 6.7 | 1 | 6.7 |
| sidewalks | 2.0 | 1 | 2.0 |
| berms and driveways | 0.9 | 1 | 0.9 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 0.7 | 1 | 0.7 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 20.4 | 1 | 20.4 |
| cutaway | 12.8 | 1 | 12.8 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.6 | 1 | 0.6 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.4 | 4 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 2.3 | 3 | 0.9 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.9 | 1 | 0.9 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 6.3 | 1 | 6.3 |

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
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 3.6 | 1 | 3.6 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.3 | 1 | 0.3 |
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
| park litter | 0.6 | 1 | 0.6 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 3.9 | 4 | 1.6 |
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
| overhead | 0.2 | 1 | 0.2 |
| outdoor | 1.9 | 1 | 1.9 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 58 | 20 |
| 186_141 | yes | 17 | 2 |
| 185_141 | yes | 13 | 2 |
| 185_142 | yes | 15 | 5 |
| 183_148 | yes | 163 | 9 |
| 196_145 | yes | 440 | 51 |
| 210_134 | yes | 12 | 1 |
| 182_148 | yes | 125 | 20 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 766 ms (slowest 136 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **65** | cutaway 25 | 0 | 99 | 54 | 179,769 | 93,711 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **104** | interior plans 26 | 0 | 100 | 75 | 242,361 | 126,463 | 0 | 129 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 125 + 20 | **96** | cutaway 32 | 0 | 100 | 56 | 110,356 | 61,805 | 0 | 66 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **86** | cutaway 35 | 0 | 101 | 53 | 267,161 | 138,369 | 0 | 83 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **119** | fences and walls 34 | 0 | 106 | 61 | 245,587 | 128,383 | 1 | 47 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **59** | cutaway 27 | 0 | 86 | 52 | 190,554 | 101,967 | 0 | 55 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **62** | cutaway 14 | 0 | 100 | 54 | 157,791 | 83,737 | 0 | 49 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **41** | cutaway 7 | 0 | 85 | 44 | 104,889 | 60,042 | 0 | 14 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **136** | cutaway 76 | 0 | 79 | 46 | 211,115 | 113,812 | 0 | 44 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 858 | 495 | 495 | 495 | 1,709,583 | 908,289 | 125 | 1,974 | 1 (0) | 552 | 341 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (95,372 vertices, 50,284 triangles, 99 objects, 68 meshes; hash 0a0e9b6f77fcb0e8 vs 0a0e9b6f77fcb0e8; 76 colliders, lights and loot containers, hash b567c81d8feac664 vs b567c81d8feac664). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 59 | 37 / 1 / 0.1 | 0 / 0.0 | 61 / 92,441 / 1.9 | 3.4 | 55.6 | 44 / 3.1 |
| rural-5km | Full | 36 | 83 / 1 / 0.1 | 0 / 0.0 | 63 / 131,159 / 2.5 | 4.1 | 31.4 | 43 / 0.3 |
| louisville-downtown | Full | 108 | 102 / 1 / 0.1 | 0 / 0.0 | 52 / 177,153 / 4.2 | 9.1 | 99.2 | 37 / 0.2 |
| lexington-downtown | Full | 73 | 66 / 1 / 0.1 | 0 / 0.0 | 51 / 92,012 / 6.3 | 7.6 | 64.9 | 38 / 0.2 |
| hazard | Full | 19 | 164 / 1 / 0.1 | 0 / 0.0 | 34 / 56,674 / 1.1 | 1.9 | 17.1 | 28 / 0.2 |
| louisville 0,0 | Full | 65 | 102 / 1 / 0.1 | 0 / 0.0 | 52 / 177,153 / 4.3 | 6.1 | 58.4 | 37 / 0.2 |
| louisville 1,0 | Full | 104 | 70 / 1 / 0.1 | 0 / 0.0 | 73 / 239,691 / 5.6 | 8.1 | 96.1 | 46 / 0.3 |
| louisville -1,0 | Full | 96 | 142 / 1 / 0.1 | 0 / 0.0 | 50 / 107,279 / 2.6 | 4.1 | 91.4 | 37 / 0.2 |
| louisville 0,1 | Full | 86 | 83 / 1 / 0.1 | 0 / 0.0 | 50 / 264,498 / 5.4 | 6.8 | 78.7 | 36 / 0.2 |
| louisville 0,-1 | Full | 119 | 82 / 1 / 0.1 | 0 / 0.0 | 57 / 242,773 / 4.6 | 6.3 | 112.4 | 37 / 0.3 |
| louisville 1,1 | Full | 59 | 67 / 1 / 0.1 | 0 / 0.0 | 49 / 187,911 / 3.4 | 4.7 | 53.8 | 35 / 0.2 |
| louisville -1,1 | Full | 62 | 117 / 1 / 0.1 | 0 / 0.0 | 51 / 155,080 / 3.2 | 4.6 | 57.8 | 37 / 0.2 |
| louisville 1,-1 | Full | 41 | 89 / 1 / 0.1 | 0 / 0.0 | 41 / 102,221 / 2.5 | 4.2 | 36.3 | 32 / 0.2 |
| louisville -1,-1 | Full | 136 | 110 / 1 / 0.1 | 0 / 0.0 | 42 / 208,443 / 4.1 | 5.4 | 130.4 | 30 / 0.2 |

The Louisville block's NavMesh: 327 sources collected in 2 ms; baked at once (as one 384 m square) in 151 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 40 | 14 | 35% | cutaway 12.5 | streets 6.9 | 95,372 / 11,363 | 99 / 37 | 68 / 32 | 69 / 1 | 3 / 0 |
| rural-5km | 26 | 6 | 22% | cutaway 9.4 | streets 1.4 | 132,668 / 8,003 | 95 / 38 | 64 / 33 | 115 / 1 | 5 / 0 |
| louisville-downtown | 92 | 12 | 14% | cutaway 45.4 | buildings 3.6 | 193,209 / 15,139 | 99 / 36 | 54 / 31 | 65 / 1 | 0 / 0 |
| lexington-downtown | 56 | 12 | 22% | cutaway 16.0 | streets 6.2 | 130,569 / 11,694 | 94 / 32 | 53 / 27 | 42 / 1 | 0 / 0 |
| hazard | 21 | 8 | 37% | streets 3.5 | streets 3.0 | 66,921 / 8,355 | 72 / 33 | 39 / 28 | 2 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 73,873, normals 69,223, UVs 78,946; largest difference 3.93E-06 m, 1.2E-05 (normals), 0.000614 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 68.20 | 34.10 | make 59.58 + switch on 8.62 ms; 1602 objects; destroy 4.06 ms; 3,000 rays 2.71 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 16.39 | 8.20 | make 11.55 + switch on 4.84 ms; 10 objects; destroy 2.63 ms; 3,000 rays 2.44 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.07 | 5.54 | make 3.38 + switch on 7.69 ms; 2 objects; destroy 0.09 ms; 3,000 rays 2.04 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.16 | 1.08 | make 2.14 + switch on 0.02 ms (+ 10.04 ms baking on the worker); 2 objects; destroy 0.04 ms; 3,000 rays 2.02 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 9.52 | 238.05 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 16.13 | 403.13 | objects 1.48, allocate 0.57, pack 12.69, apply 1.39 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 2.01 | 50.20 | objects 1.78, allocate 0.02, pack 5.58 (worker), apply 0.21 ms |
| primitives | Box, turned, flat colour | 20,000 | 21.39 | 1.07 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 87.88 | 4.39 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 38.21 | 1.91 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 81.48 | 4.07 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 16.12 | 0.81 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 47.59 | 2.38 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 5.88 | 0.29 | 80,000 vertices |
| primitives | Tri | 20,000 | 3.54 | 0.18 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 10.15 | 2.03 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 28.97 | 5.79 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 30.29 | 10.10 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 30.96 | 10.32 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.10 | 7.05 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 14.83 | 14.83 |  |
| objects | BoxCollider added to one object | 2,000 | 40.48 | 20.24 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 24.69 | 123.44 |  |
| objects | RoomLight.Spawn | 100 | 11.91 | 119.14 |  |
| objects | switching all of the above on | 1 | 27.42 | 27416.70 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 9.26 | 9261.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.72 | 11.81 | worst 0.12 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 6.88 | 688.40 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 402.86 | 402858.50 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 389.30 | 389295.30 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.20 | 20200.50 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,275 ms, 3,870,339 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (25) | 0.48 | 0.03 | 5.27 | 9.63 | 40.51 | 25 / 8 / 1 / 1 | cutaway 39.9 | 5 (5 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 / 2,214 / 4,347,784 | 2 | 296 / 911 |
| drive-east | 2400 (135) | 0.43 | 0.03 | 5.10 | 8.44 | 19.13 | 128 / 30 / 2 / 0 | trees 16.1 | 35 (35 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,283 / 6,476,902 | 2 | 250 / 999 |
| teleport-louisville | 720 (157) | 2.77 | 0.04 | 16.32 | 33.82 | 73.04 | 157 / 77 / 37 / 10 | yards 68.2 | 25 (25 / 0) | 0 | 30 | 0.2 | 1.0 | 11 / 59 | 50 / 3,284 / 7,975,619 | 4 | 274 / 1,054 |
| drive-louisville | 1800 (111) | 0.70 | 0.04 | 5.54 | 17.17 | 59.54 | 108 / 45 / 21 / 5 | buildings 58.3 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,546 / 8,569,884 | 6 | 291 / 1,081 |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (3x, worst 39.9 ms), paths (1x, worst 6.2 ms), buses and cruisers (1x, worst 4.9 ms), parking (1x, worst 4.4 ms), wall drifts (1x, worst 4.0 ms), yards (1x, worst 3.5 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: trees (9x, worst 16.1 ms), cutaway (14x, worst 12.9 ms), yards (2x, worst 11.9 ms), interior plans (1x, worst 11.1 ms), buildings (2x, worst 10.2 ms), outdoor (2x, worst 4.8 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: yards (1x, worst 68.2 ms), fields (13x, worst 51.4 ms), fences and walls (1x, worst 39.2 ms), cutaway (22x, worst 37.4 ms), buildings (20x, worst 31.0 ms), parking (1x, worst 29.1 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: buildings (13x, worst 58.3 ms), cutaway (12x, worst 42.1 ms), road network (1x, worst 15.6 ms), fields (7x, worst 14.1 ms), streets (3x, worst 13.3 ms), outdoor (1x, worst 10.7 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 10,047 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 424 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.481 | 0% | 7,133 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.102 | 0% | 7,114 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 73 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.098 | 0% | 70 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.476 | 0% | 71 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 67 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 74 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 73 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 59 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 56 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 67 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 76 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.471 | 0% | 52 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 63 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 88 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 64 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 211 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.213 | 18% | 109 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.413 | 0% | 69 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 21% | 83 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 142 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 62 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 57 |

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
| setup | yes | 129 |  |
| square: KentuckyMap.Build | yes | 4,256 |  |
| square: count | yes | 78 |  |
| square: pick spots | yes | 83 |  |
| square: pictures | yes | 28,263 |  |
| square: discard | yes | 118 |  |
| square: passes (warm rebuild) | yes | 2,433 |  |
| chunks: stress test | yes | 1,120 |  |
| chunks: Louisville 3x3 | yes | 1,755 |  |
| chunks: determinism | yes | 137 |  |
| perf: Lite against Full | yes | 406 |  |
| perf: determinism across build orders | yes | 345 |  |
| perf: what things cost | yes | 1,693 |  |
| perf: walking and driving through the state | yes | 159,807 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
