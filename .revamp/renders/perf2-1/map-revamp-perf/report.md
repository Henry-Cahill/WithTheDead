# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 205.5 s, started 2026-10-24 09:38:15.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,106 ms |
| the same square again, warm, pass by pass | 2,415 ms |
| logged while building | 4 errors, 116 warnings, 232 edit-mode artefacts |
| spawn | -84.2, 13.8, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 66 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 516 containers, 21 lights, 332 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 15,666 | 5,530 | 5,530 | 4,626 | 3,310,859 | 1,962,847 | 120 | 11,994 | 247 (0) | 6,163 | 4,162 | 1,843 |
| RaidOverhead | 457 | 339 | 339 | 339 | 323,687 | 166,411 | 128 | 1,350 | 0 (0) | 0 | 339 | 0 |
| RaidScenery | 119 | 118 | 118 | 118 | 84,853 | 84,184 | 38 | 337 | 0 (0) | 0 | 73 | 0 |
| **all** | 16,242 | 5,987 | 5,987 | 5,083 | 3,719,399 | 2,213,442 | 217 | 13,681 | 247 (0) | 6,163 | 4,574 | 1,843 |

### Meshes that aren't finite numbers

None: every one of the 9,484 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 65 | 3% | 1 | 65 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 37 | 2% | 1 | 37 |
| cemeteries | 5 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 12 | 0% | 10 | 2 |
| snow tone | 11 | 0% | 1 | 11 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 2 | 0% | 1 | 2 |
| streets | 33 | 1% | 1 | 33 |
| sidewalks | 44 | 2% | 1 | 44 |
| berms and driveways | 12 | 1% | 1 | 12 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 138 | 6% | 21 | 22 |
| interior plans | 168 | 7% | 7 | 32 |
| interior shell | 785 | 33% | 116 | 43 |
| interior walls | 179 | 7% | 116 | 14 |
| interior doors | 642 | 27% | 116 | 39 |
| cutaway | 65 | 3% | 15 | 11 |
| utility lines | 13 | 1% | 1 | 13 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 19 | 1% | 1 | 19 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 9 | 0% | 1 | 9 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 7 | 0% | 1 | 7 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 3 | 0% | 1 | 3 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 8 | 0% | 15 | 1 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 23 | 1% | 22 | 2 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 44 | 2% | 10 | 14 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 10 | 0% | 1 | 10 |
| (emit the outdoor meshes) | 54 | 2% | 1 | 54 |
| **total** | **2,415** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **84** | cutaway 29 | 1 | 116 | 68 | 95,372 | 50,284 | 3 | 105 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **47** | cutaway 13 | 1 | 118 | 64 | 133,759 | 76,841 | 5 | 197 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 197 + 13 | **125** | cutaway 35 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 540 + 64 | **88** | buildings 31 | 1 | 153 | 54 | 94,666 | 50,719 | 0 | 105 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 7 + 2 | **24** | streets 4 | 1 | 101 | 39 | 60,103 | 44,413 | 0 | 165 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.7 | 1 | 2.7 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.5 | 1 | 1.5 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 8.4 | 1 | 8.4 |
| sidewalks | 2.3 | 1 | 2.3 |
| berms and driveways | 1.6 | 1 | 1.6 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.8 | 2 | 6.0 |
| cutaway | 28.7 | 2 | 28.6 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.4 | 1 | 0.4 |
| signs | 5.9 | 1 | 5.9 |
| street furniture | 1.3 | 1 | 1.3 |
| landmarks | 0.6 | 1 | 0.6 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.8 | 1 | 0.8 |
| nature plan | 0.5 | 1 | 0.5 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.1 | 1 | 0.1 |
| street trees | 0.3 | 1 | 0.3 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 4 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 7.9 | 1 | 7.9 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.8 | 1 | 1.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.2 | 1 | 0.2 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 3.8 | 1 | 3.8 |
| meshes | 1.7 | 1 | 1.7 |

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
| yards | 4.4 | 2 | 2.8 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 0.3 | 1 | 0.3 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.8 | 2 | 12.4 |
| cutaway | 15.1 | 2 | 13.4 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.8 | 1 | 0.8 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.5 | 4 | 0.5 |
| driveway cars | 1.1 | 1 | 1.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.3 | 1 | 0.3 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 2 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.4 | 1 | 0.4 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.9 | 1 | 0.9 |
| objects | 1.9 | 1 | 1.9 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 8.2 | 1 | 8.2 |
| water | 14.3 | 1 | 14.3 |
| fields | 11.8 | 1 | 11.8 |
| parking | 0.9 | 1 | 0.9 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.7 | 2 | 1.6 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 1.1 | 1 | 1.1 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.5 | 1 | 4.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 17.5 | 2 | 11.1 |
| cutaway | 35.4 | 1 | 35.4 |
| utility lines | 0.9 | 1 | 0.9 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.3 | 1 | 1.3 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.3 | 1 | 0.3 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 5.4 | 1 | 5.4 |
| nature plan | 0.7 | 1 | 0.7 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.8 | 1 | 0.8 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 3.1 | 3 | 1.7 |
| parking lot cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.7 | 1 | 3.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.4 | 2 | 2.1 |
| objects | 2.0 | 1 | 2.0 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.7 | 1 | 1.7 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 8.9 | 2 | 8.9 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 7.0 | 1 | 7.0 |
| sidewalks | 2.5 | 1 | 2.5 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.8 | 1 | 0.8 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 30.9 | 1 | 30.9 |
| cutaway | 15.8 | 1 | 15.8 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.3 | 1 | 1.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 1.0 | 1 | 1.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.8 | 1 | 0.8 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.4 | 4 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 3.3 | 3 | 1.1 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.1 | 1 | 0.1 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.4 | 1 | 2.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 1.3 | 1 | 1.3 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 1.3 | 1 | 1.3 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 2.0 | 1 | 2.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 4.3 | 1 | 4.3 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.3 | 1 | 1.3 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.9 | 1 | 0.9 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 4.1 | 4 | 1.7 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 2.1 | 3 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.9 | 1 | 0.9 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 47 | 25 |
| 186_141 | yes | 26 | 2 |
| 185_141 | yes | 8 | 2 |
| 185_142 | yes | 10 | 3 |
| 183_148 | yes | 197 | 13 |
| 196_145 | yes | 540 | 64 |
| 210_134 | yes | 7 | 2 |
| 182_148 | yes | 146 | 12 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 813 ms (slowest 132 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **78** | cutaway 31 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **128** | interior plans 36 | 1 | 140 | 75 | 242,361 | 126,463 | 0 | 198 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 146 + 12 | **77** | cutaway 19 | 1 | 226 | 56 | 110,356 | 61,805 | 0 | 207 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **132** | cutaway 58 | 1 | 178 | 53 | 267,161 | 138,369 | 0 | 165 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **118** | fences and walls 33 | 1 | 170 | 61 | 245,587 | 128,383 | 1 | 128 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **63** | cutaway 29 | 0 | 126 | 52 | 190,554 | 101,967 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **73** | cutaway 15 | 1 | 208 | 54 | 157,791 | 83,737 | 0 | 165 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **42** | cutaway 8 | 0 | 153 | 44 | 104,889 | 60,042 | 0 | 102 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **102** | cutaway 57 | 1 | 162 | 46 | 211,115 | 113,812 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,551 | 495 | 495 | 495 | 1,709,583 | 908,289 | 125 | 1,974 | 1 (0) | 1,405 | 341 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (95,372 vertices, 50,284 triangles, 116 objects, 68 meshes; hash e8ef7e9829059ac0 vs e8ef7e9829059ac0; 112 colliders, lights and loot containers, hash 6074aea0d42f2c35 vs 6074aea0d42f2c35). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 84 | 37 / 18 / 0.4 | 0 / 0.0 | 61 / 92,441 / 13.6 | 15.0 | 68.9 | 44 / 4.3 |
| rural-5km | Full | 47 | 83 / 24 / 0.9 | 0 / 0.0 | 63 / 131,159 / 3.1 | 5.7 | 40.2 | 43 / 0.5 |
| louisville-downtown | Full | 125 | 102 / 88 / 2.3 | 0 / 0.0 | 52 / 177,153 / 5.5 | 8.4 | 113.9 | 37 / 0.3 |
| lexington-downtown | Full | 88 | 66 / 54 / 0.7 | 0 / 0.0 | 51 / 92,012 / 2.2 | 3.7 | 83.2 | 38 / 0.5 |
| hazard | Full | 24 | 164 / 35 / 0.8 | 0 / 0.0 | 34 / 56,674 / 0.3 | 1.1 | 21.9 | 28 / 0.3 |
| louisville 0,0 | Full | 78 | 102 / 88 / 1.6 | 0 / 0.0 | 52 / 177,153 / 4.6 | 6.4 | 70.2 | 37 / 0.3 |
| louisville 1,0 | Full | 128 | 70 / 41 / 0.6 | 0 / 0.0 | 73 / 239,691 / 6.5 | 8.8 | 118.5 | 46 / 0.6 |
| louisville -1,0 | Full | 77 | 142 / 127 / 2.0 | 0 / 0.0 | 50 / 107,279 / 1.4 | 2.5 | 72.5 | 37 / 0.3 |
| louisville 0,1 | Full | 132 | 83 / 78 / 1.2 | 0 / 0.0 | 50 / 264,498 / 26.5 | 28.5 | 102.2 | 36 / 0.3 |
| louisville 0,-1 | Full | 118 | 82 / 65 / 1.0 | 0 / 0.0 | 57 / 242,773 / 5.8 | 7.5 | 109.9 | 37 / 0.3 |
| louisville 1,1 | Full | 63 | 67 / 41 / 0.7 | 0 / 0.0 | 49 / 187,911 / 3.5 | 4.6 | 57.4 | 35 / 0.5 |
| louisville -1,1 | Full | 73 | 117 / 109 / 1.6 | 0 / 0.0 | 51 / 155,080 / 2.9 | 4.0 | 67.2 | 37 / 0.5 |
| louisville 1,-1 | Full | 42 | 89 / 69 / 0.8 | 0 / 0.0 | 41 / 102,221 / 1.2 | 1.8 | 39.2 | 32 / 0.3 |
| louisville -1,-1 | Full | 102 | 110 / 84 / 1.0 | 0 / 0.0 | 42 / 208,443 / 4.9 | 6.0 | 95.2 | 30 / 0.3 |

The Louisville block's NavMesh: 327 sources collected in 2 ms; baked at once (as one 384 m square) in 177 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 47 | 19 | 41% | cutaway 13.3 | streets 7.0 | 95,372 / 11,363 | 116 / 37 | 68 / 32 | 105 / 1 | 3 / 0 |
| rural-5km | 36 | 12 | 32% | cutaway 13.4 | buildings 4.3 | 132,668 / 8,003 | 114 / 38 | 64 / 33 | 192 / 1 | 5 / 0 |
| louisville-downtown | 72 | 12 | 17% | cutaway 27.5 | buildings 3.7 | 193,209 / 15,139 | 180 / 36 | 54 / 31 | 160 / 1 | 0 / 0 |
| lexington-downtown | 59 | 13 | 23% | cutaway 17.0 | streets 6.4 | 130,569 / 11,694 | 142 / 32 | 53 / 27 | 103 / 1 | 0 / 0 |
| hazard | 21 | 8 | 37% | streets 3.7 | streets 3.0 | 66,921 / 8,355 | 104 / 33 | 39 / 28 | 163 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 64.67 | 32.34 | make 56.36 + switch on 8.31 ms; 1602 objects; destroy 4.17 ms; 3,000 rays 2.84 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 15.13 | 7.57 | make 10.53 + switch on 4.61 ms; 10 objects; destroy 1.58 ms; 3,000 rays 2.14 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.55 | 5.77 | make 3.80 + switch on 7.74 ms; 2 objects; destroy 0.12 ms; 3,000 rays 2.10 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.18 | 1.09 | make 2.14 + switch on 0.04 ms (+ 9.39 ms baking on the worker); 2 objects; destroy 0.06 ms; 3,000 rays 2.04 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.01 | 200.16 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.99 | 199.83 | objects 0.84, allocate 0.02, pack 7.02, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.19 | 29.80 | objects 1.01, allocate 0.02, pack 6.76 (worker), apply 0.17 ms |
| primitives | Box, turned, flat colour | 20,000 | 48.00 | 2.40 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 61.92 | 3.10 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 38.93 | 1.95 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 107.83 | 5.39 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 18.20 | 0.91 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 47.79 | 2.39 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 7.28 | 0.36 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.91 | 0.25 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 11.02 | 2.20 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 32.77 | 6.56 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 25.10 | 8.37 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 31.71 | 10.57 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.43 | 7.21 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 16.16 | 16.16 |  |
| objects | BoxCollider added to one object | 2,000 | 41.65 | 20.82 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 24.23 | 121.13 |  |
| objects | RoomLight.Spawn | 100 | 10.75 | 107.45 |  |
| objects | switching all of the above on | 1 | 30.81 | 30804.90 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 12.02 | 12021.20 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.70 | 11.39 | worst 0.12 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 5.54 | 554.20 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 419.35 | 419347.20 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 415.21 | 415211.00 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 26.88 | 26877.70 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (0 pieces in 1,583 ms, 0 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (140) | 1.44 | 0.05 | 5.07 | 20.23 | 47.23 | 27 / 12 / 7 / 2 | cutaway 23.2 | 25 (9 / 16) | 0 | 0 | 0.0 | 1.8 | 2 / 107 | 25 (16 Lite) / 1,234 / 1,514,875 | 2 | 253 / 951 (pool 21) |
| drive-east | 2400 (269) | 0.54 | 0.05 | 3.90 | 8.35 | 33.16 | 59 / 26 / 5 / 1 | interior plans 33.1 | 43 (27 / 16) | 3 | 35 | 0.0 | 0.0 | 0 / 0 | 30 (12 Lite) / 1,268 / 1,994,284 | 2 | 252 / 976 (pool 22) |
| teleport-louisville | 720 (161) | 1.77 | 0.05 | 5.61 | 32.32 | 224.73 | 40 / 24 / 15 / 7 | buildings 88.0 | 25 (9 / 16) | 0 | 30 | 0.2 | 2.4 | 10 / 143 | 25 (16 Lite) / 883 / 2,108,943 | 4 | 298 / 977 (pool 23) |
| drive-louisville | 1800 (177) | 0.59 | 0.05 | 3.52 | 13.06 | 60.04 | 47 / 29 / 14 / 2 | cutaway 60.0 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 41 | 30 (12 Lite) / 1,277 / 3,310,407 | 6 | 461 / 1,027 (pool 27) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (8x, worst 23.2 ms), interior plans (2x, worst 18.9 ms), junctions (1x, worst 7.2 ms), wall drifts (1x, worst 1.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (1x, worst 33.1 ms), cutaway (16x, worst 24.3 ms), buildings (6x, worst 13.9 ms), signs (1x, worst 10.5 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: buildings (6x, worst 88.0 ms), cutaway (9x, worst 45.6 ms), interior plans (1x, worst 42.2 ms), water (2x, worst 12.5 ms), fields (3x, worst 11.1 ms), snow tone (1x, worst 8.9 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (11x, worst 60.0 ms), parking (1x, worst 29.3 ms), buildings (10x, worst 23.9 ms), objects (2x, worst 23.7 ms), fields (3x, worst 15.1 ms), street lights (1x, worst 8.6 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 38 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_6TH & MAIN GARAGE050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_6TH & MAIN GARAGE050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DRY GOODS08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DRY GOODS08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DERBY CITY GAMING07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DERBY CITY GAMING07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 9,513 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 562 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.481 | 0% | 7,470 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.102 | 0% | 9,206 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 78 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.098 | 0% | 85 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.476 | 0% | 85 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 71 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 87 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 60 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 135 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 58 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 66 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 75 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.471 | 0% | 63 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 87 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 89 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 65 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 80 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.213 | 18% | 150 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.413 | 0% | 85 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 21% | 75 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 226 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 73 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 67 |

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

### Everything logged during the run: 8 errors, 232 warnings, 502 edit-mode artefacts

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
| setup | yes | 93 |  |
| square: KentuckyMap.Build | yes | 5,110 |  |
| square: count | yes | 398 |  |
| square: pick spots | yes | 89 |  |
| square: pictures | yes | 30,493 |  |
| square: discard | yes | 133 |  |
| square: passes (warm rebuild) | yes | 2,538 |  |
| chunks: stress test | yes | 1,401 |  |
| chunks: Louisville 3x3 | yes | 2,104 |  |
| chunks: determinism | yes | 134 |  |
| perf: Lite against Full | yes | 481 |  |
| perf: determinism across build orders | yes | 359 |  |
| perf: what things cost | yes | 1,728 |  |
| perf: walking and driving through the state | yes | 160,423 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
