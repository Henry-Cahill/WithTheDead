# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 194.9 s, started 2026-10-24 10:04:37.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,355 ms |
| the same square again, warm, pass by pass | 4,970 ms |
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
| (read the area) | 226 | 5% | 1 | 226 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 1 | 0% | 1 | 1 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 49 | 1% | 1 | 49 |
| cemeteries | 5 | 0% | 4 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 13 | 0% | 10 | 2 |
| snow tone | 14 | 0% | 1 | 14 |
| road network | 9 | 0% | 1 | 9 |
| junctions | 3 | 0% | 1 | 3 |
| streets | 71 | 1% | 1 | 71 |
| sidewalks | 32 | 1% | 1 | 32 |
| berms and driveways | 62 | 1% | 1 | 62 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 3 | 0% | 1 | 3 |
| buildings | 268 | 5% | 21 | 79 |
| interior plans | 374 | 8% | 7 | 117 |
| interior shell | 1,859 | 37% | 116 | 211 |
| interior walls | 496 | 10% | 116 | 64 |
| interior doors | 1,084 | 22% | 116 | 59 |
| cutaway | 117 | 2% | 15 | 17 |
| utility lines | 33 | 1% | 1 | 33 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 10 | 0% | 1 | 10 |
| street lights | 6 | 0% | 1 | 6 |
| signs | 6 | 0% | 1 | 6 |
| street furniture | 17 | 0% | 1 | 17 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 7 | 0% | 1 | 7 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 4 | 0% | 1 | 4 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 4 | 0% | 1 | 4 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 10 | 0% | 15 | 2 |
| driveway cars | 25 | 0% | 3 | 12 |
| buses and cruisers | 1 | 0% | 1 | 1 |
| kerbside cars | 35 | 1% | 22 | 3 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 48 | 1% | 10 | 13 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 2 | 0% | 1 | 2 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 17 | 0% | 1 | 17 |
| (emit the outdoor meshes) | 51 | 1% | 1 | 51 |
| **total** | **4,970** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **333** | cutaway 113 | 0 | 99 | 68 | 95,372 | 50,284 | 3 | 69 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **89** | buildings 22 | 10 | 95 | 64 | 133,759 | 76,841 | 5 | 115 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 313 + 17 | **277** | cutaway 94 | 0 | 99 | 54 | 179,769 | 93,711 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 888 + 93 | **109** | buildings 38 | 0 | 100 | 54 | 94,666 | 50,719 | 0 | 40 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 7 + 2 | **26** | outdoor 4 | 0 | 67 | 39 | 60,103 | 44,413 | 0 | 2 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.9 | 1 | 2.9 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.2 | 2 | 0.2 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 2.0 | 1 | 2.0 |
| junctions | 29.9 | 1 | 29.9 |
| streets | 11.2 | 1 | 11.2 |
| sidewalks | 2.9 | 1 | 2.9 |
| berms and driveways | 9.2 | 1 | 9.2 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.7 | 2 | 7.0 |
| cutaway | 113.1 | 2 | 112.9 |
| utility lines | 2.3 | 1 | 2.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 3.3 | 1 | 3.3 |
| signs | 4.6 | 1 | 4.6 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 1.1 | 1 | 1.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.7 | 1 | 1.7 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.5 | 1 | 0.5 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.4 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 42.3 | 1 | 42.3 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 6.7 | 1 | 6.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.3 | 1 | 0.3 |
| points | 0.2 | 1 | 0.2 |
| overhead | 1.0 | 1 | 1.0 |
| outdoor | 83.8 | 1 | 83.8 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 8.5 | 2 | 6.1 |
| snow tone | 13.8 | 1 | 13.8 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 3.4 | 1 | 3.4 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 5.2 | 1 | 5.2 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 22.8 | 2 | 22.4 |
| cutaway | 20.7 | 2 | 18.3 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 1.7 | 1 | 1.7 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.5 | 4 | 0.5 |
| driveway cars | 2.0 | 1 | 2.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.4 | 2 | 1.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.3 | 1 | 0.3 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 3.2 | 1 | 3.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.9 | 1 | 0.9 |
| ground | 39.3 | 1 | 39.3 |
| water | 31.5 | 1 | 31.5 |
| fields | 19.6 | 1 | 19.6 |
| parking | 1.5 | 1 | 1.5 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 2.4 | 2 | 2.3 |
| snow tone | 2.3 | 1 | 2.3 |
| road network | 0.8 | 1 | 0.8 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 1.5 | 1 | 1.5 |
| berms and driveways | 0.9 | 1 | 0.9 |
| road markings | 1.1 | 1 | 1.1 |
| paths | 7.6 | 1 | 7.6 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 21.2 | 2 | 13.3 |
| cutaway | 93.7 | 1 | 93.7 |
| utility lines | 1.1 | 1 | 1.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.7 | 1 | 0.7 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 6.4 | 1 | 6.4 |
| nature plan | 1.1 | 1 | 1.1 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 8.9 | 1 | 8.9 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.1 |
| driveway cars | 0.3 | 1 | 0.3 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 22.5 | 3 | 14.4 |
| parking lot cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.8 | 1 | 3.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.3 | 1 | 0.3 |
| points | 0.0 | 1 | 0.0 |
| overhead | 1.0 | 1 | 1.0 |
| outdoor | 3.5 | 1 | 3.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 2.2 | 1 | 2.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 12.2 | 2 | 12.2 |
| snow tone | 2.6 | 1 | 2.6 |
| road network | 0.9 | 1 | 0.9 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 7.7 | 1 | 7.7 |
| sidewalks | 2.7 | 1 | 2.7 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.9 | 1 | 0.9 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 37.9 | 1 | 37.9 |
| cutaway | 16.2 | 1 | 16.2 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.4 | 1 | 1.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.2 | 1 | 1.2 |
| street furniture | 1.4 | 1 | 1.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.7 | 1 | 1.7 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.6 | 4 | 0.5 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 4.3 | 3 | 1.5 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.4 | 1 | 3.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.4 | 1 | 0.4 |
| points | 2.3 | 1 | 2.3 |
| overhead | 0.7 | 1 | 0.7 |
| outdoor | 4.3 | 1 | 4.3 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 2.3 | 1 | 2.3 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.9 | 1 | 1.9 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 4.0 | 1 | 4.0 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.9 | 1 | 0.9 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.3 | 1 | 1.3 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.7 | 1 | 0.7 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 4.8 | 4 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 2.4 | 3 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.4 | 1 | 0.4 |
| outdoor | 4.5 | 1 | 4.5 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 124 | 24 |
| 186_141 | yes | 21 | 86 |
| 185_141 | yes | 19 | 38 |
| 185_142 | yes | 49 | 4 |
| 183_148 | yes | 313 | 17 |
| 196_145 | yes | 888 | 93 |
| 210_134 | yes | 7 | 2 |
| 182_148 | yes | 269 | 23 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 1,178 ms (slowest 200 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **101** | cutaway 40 | 0 | 99 | 54 | 179,769 | 93,711 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **195** | buildings 41 | 1 | 100 | 75 | 242,361 | 126,463 | 0 | 129 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 269 + 23 | **92** | cutaway 24 | 1 | 100 | 56 | 110,356 | 61,805 | 0 | 66 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **145** | cutaway 50 | 1 | 101 | 53 | 267,161 | 138,369 | 0 | 83 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **200** | fences and walls 46 | 1 | 106 | 61 | 245,587 | 128,383 | 1 | 47 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **108** | cutaway 47 | 0 | 86 | 52 | 190,554 | 101,967 | 0 | 55 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **112** | buildings 21 | 1 | 100 | 54 | 157,791 | 83,737 | 0 | 49 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **74** | cutaway 10 | 0 | 85 | 44 | 104,889 | 60,042 | 0 | 14 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **152** | cutaway 70 | 0 | 79 | 46 | 211,115 | 113,812 | 0 | 44 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 858 | 495 | 495 | 495 | 1,709,583 | 908,289 | 125 | 1,974 | 1 (0) | 552 | 341 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (95,372 vertices, 50,284 triangles, 99 objects, 68 meshes; hash 0a0e9b6f77fcb0e8 vs 0a0e9b6f77fcb0e8; 76 colliders, lights and loot containers, hash b567c81d8feac664 vs b567c81d8feac664). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 333 | 37 / 1 / 0.1 | 0 / 0.0 | 61 / 92,441 / 112.3 | 141.3 | 191.8 | 44 / 5.0 |
| rural-5km | Full | 89 | 83 / 1 / 0.1 | 0 / 0.0 | 63 / 131,159 / 4.6 | 8.3 | 80.5 | 43 / 1.5 |
| louisville-downtown | Full | 277 | 102 / 1 / 0.2 | 0 / 0.0 | 52 / 177,153 / 21.1 | 25.8 | 251.1 | 37 / 0.4 |
| lexington-downtown | Full | 109 | 66 / 1 / 0.1 | 0 / 0.0 | 51 / 92,012 / 3.5 | 6.7 | 102.0 | 38 / 0.6 |
| hazard | Full | 26 | 164 / 1 / 0.2 | 0 / 0.0 | 34 / 56,674 / 2.4 | 4.4 | 21.6 | 28 / 0.3 |
| louisville 0,0 | Full | 101 | 102 / 1 / 0.2 | 0 / 0.0 | 52 / 177,153 / 9.0 | 12.4 | 88.3 | 37 / 0.5 |
| louisville 1,0 | Full | 195 | 70 / 1 / 0.2 | 0 / 0.0 | 73 / 239,691 / 11.6 | 18.2 | 176.1 | 46 / 0.5 |
| louisville -1,0 | Full | 92 | 142 / 1 / 0.2 | 0 / 0.0 | 50 / 107,279 / 5.0 | 8.5 | 82.8 | 37 / 0.4 |
| louisville 0,1 | Full | 145 | 83 / 1 / 0.2 | 0 / 0.0 | 50 / 264,498 / 11.8 | 15.3 | 129.8 | 36 / 0.4 |
| louisville 0,-1 | Full | 200 | 82 / 1 / 0.3 | 0 / 0.0 | 57 / 242,773 / 8.1 | 12.2 | 187.3 | 37 / 1.1 |
| louisville 1,1 | Full | 108 | 67 / 1 / 0.2 | 0 / 0.0 | 49 / 187,911 / 7.1 | 10.2 | 97.9 | 35 / 0.4 |
| louisville -1,1 | Full | 112 | 117 / 1 / 6.7 | 0 / 0.0 | 51 / 155,080 / 6.4 | 14.5 | 90.7 | 37 / 0.4 |
| louisville 1,-1 | Full | 74 | 89 / 1 / 0.1 | 0 / 0.0 | 41 / 102,221 / 5.6 | 9.6 | 64.1 | 32 / 0.4 |
| louisville -1,-1 | Full | 152 | 110 / 1 / 0.2 | 0 / 0.0 | 42 / 208,443 / 6.3 | 8.8 | 142.9 | 30 / 0.3 |

The Louisville block's NavMesh: 327 sources collected in 2 ms; baked at once (as one 384 m square) in 196 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 50 | 17 | 34% | cutaway 16.3 | streets 8.1 | 95,372 / 11,363 | 99 / 37 | 68 / 32 | 69 / 1 | 3 / 0 |
| rural-5km | 38 | 10 | 25% | cutaway 13.4 | streets 1.8 | 132,668 / 8,003 | 95 / 38 | 64 / 33 | 115 / 1 | 5 / 0 |
| louisville-downtown | 98 | 33 | 34% | cutaway 36.3 | buildings 13.2 | 193,209 / 15,139 | 99 / 36 | 54 / 31 | 65 / 1 | 0 / 0 |
| lexington-downtown | 101 | 18 | 18% | cutaway 22.9 | streets 7.0 | 130,569 / 11,694 | 94 / 32 | 53 / 27 | 42 / 1 | 0 / 0 |
| hazard | 37 | 10 | 28% | outdoor 6.5 | streets 3.2 | 66,921 / 8,355 | 72 / 33 | 39 / 28 | 2 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 73,873, normals 69,223, UVs 78,946; largest difference 3.93E-06 m, 1.2E-05 (normals), 0.000614 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 117.28 | 58.64 | make 100.58 + switch on 16.70 ms; 1602 objects; destroy 8.95 ms; 3,000 rays 4.28 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 34.31 | 17.15 | make 23.49 + switch on 10.81 ms; 10 objects; destroy 6.98 ms; 3,000 rays 4.83 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 20.26 | 10.13 | make 5.46 + switch on 14.79 ms; 2 objects; destroy 0.15 ms; 3,000 rays 3.09 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 3.87 | 1.94 | make 3.76 + switch on 0.11 ms (+ 11.33 ms baking on the worker); 2 objects; destroy 0.10 ms; 3,000 rays 3.12 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 13.44 | 336.05 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 18.96 | 473.97 | objects 1.99, allocate 0.65, pack 14.85, apply 1.47 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 2.60 | 65.05 | objects 2.12, allocate 0.02, pack 15.26 (worker), apply 0.46 ms |
| primitives | Box, turned, flat colour | 20,000 | 40.72 | 2.04 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 120.71 | 6.04 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 44.14 | 2.21 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 94.89 | 4.74 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 21.02 | 1.05 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 60.22 | 3.01 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.38 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.21 | 0.21 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 12.42 | 2.48 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 36.09 | 7.22 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 31.28 | 10.43 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 60.63 | 20.21 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 21.39 | 10.69 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 26.21 | 26.21 |  |
| objects | BoxCollider added to one object | 2,000 | 66.36 | 33.18 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 36.27 | 181.33 |  |
| objects | RoomLight.Spawn | 100 | 18.93 | 189.29 |  |
| objects | switching all of the above on | 1 | 46.40 | 46402.00 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 40.78 | 40782.30 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 1.00 | 16.40 | worst 0.16 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 10.33 | 1033.20 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 872.03 | 872030.60 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 568.88 | 568878.40 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 38.88 | 38876.30 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,500 ms, 3,870,339 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (33) | 0.64 | 0.04 | 5.42 | 13.23 | 44.28 | 32 / 9 / 1 / 1 | cutaway 43.3 | 5 (5 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 / 2,214 / 4,347,784 | 2 | 289 / 904 |
| drive-east | 2400 (150) | 0.49 | 0.04 | 5.24 | 8.90 | 24.24 | 143 / 32 / 6 / 0 | trees 19.6 | 35 (35 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,283 / 6,476,902 | 2 | 249 / 991 |
| teleport-louisville | 720 (167) | 2.95 | 0.04 | 15.94 | 35.73 | 68.76 | 167 / 82 / 36 / 11 | cutaway 67.1 | 25 (25 / 0) | 0 | 30 | 0.2 | 1.1 | 10 / 66 | 50 / 3,284 / 7,975,619 | 4 | 289 / 1,047 |
| drive-louisville | 1800 (113) | 0.74 | 0.04 | 5.30 | 16.81 | 72.44 | 110 / 49 / 19 / 3 | buildings 66.4 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,546 / 8,569,884 | 6 | 274 / 1,074 |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (4x, worst 43.3 ms), outdoor (1x, worst 13.7 ms), parking lot cars (1x, worst 9.7 ms), fence rows (1x, worst 8.3 ms), streets (1x, worst 5.2 ms), yards (1x, worst 3.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: trees (6x, worst 19.6 ms), interior plans (1x, worst 16.7 ms), buildings (5x, worst 15.8 ms), cutaway (15x, worst 15.6 ms), snow tone (1x, worst 9.6 ms), street lights (1x, worst 8.7 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: cutaway (23x, worst 67.1 ms), yards (1x, worst 67.0 ms), fields (11x, worst 50.5 ms), interior plans (3x, worst 43.1 ms), fences and walls (1x, worst 31.2 ms), buildings (21x, worst 30.8 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: buildings (12x, worst 66.4 ms), outdoor (2x, worst 56.3 ms), cutaway (14x, worst 29.2 ms), wall drifts (2x, worst 25.2 ms), fields (7x, worst 15.2 ms), road network (1x, worst 11.2 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 3,795 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 744 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.481 | 0% | 191 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.102 | 0% | 175 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 115 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.098 | 0% | 170 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.476 | 0% | 120 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 90 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 162 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 119 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 71 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 105 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 85 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 89 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.471 | 0% | 177 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 166 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 153 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 81 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 109 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.213 | 18% | 174 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.413 | 0% | 126 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 21% | 185 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 481 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 104 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 121 |

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
| setup | yes | 96 |  |
| square: KentuckyMap.Build | yes | 6,358 |  |
| square: count | yes | 265 |  |
| square: pick spots | yes | 326 |  |
| square: pictures | yes | 12,394 |  |
| square: discard | yes | 276 |  |
| square: passes (warm rebuild) | yes | 5,187 |  |
| chunks: stress test | yes | 2,564 |  |
| chunks: Louisville 3x3 | yes | 2,986 |  |
| chunks: determinism | yes | 177 |  |
| perf: Lite against Full | yes | 603 |  |
| perf: determinism across build orders | yes | 459 |  |
| perf: what things cost | yes | 2,786 |  |
| perf: walking and driving through the state | yes | 160,396 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
