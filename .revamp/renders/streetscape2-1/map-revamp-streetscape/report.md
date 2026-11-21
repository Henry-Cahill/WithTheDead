# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 193.5 s, started 2026-10-24 09:53:00.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,304 ms |
| the same square again, warm, pass by pass | 2,174 ms |
| logged while building | 4 errors, 116 warnings, 232 edit-mode artefacts |
| spawn | -84.2, 13.8, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 66 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 516 containers, 22 lights, 330 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 15,127 | 5,531 | 5,531 | 4,625 | 3,319,223 | 1,968,873 | 120 | 12,026 | 248 (0) | 5,237 | 4,162 | 1,843 |
| RaidOverhead | 467 | 349 | 349 | 349 | 327,668 | 169,845 | 128 | 1,374 | 0 (0) | 0 | 349 | 0 |
| RaidScenery | 119 | 118 | 118 | 118 | 69,817 | 71,830 | 36 | 327 | 0 (0) | 0 | 73 | 0 |
| **all** | 15,713 | 5,998 | 5,998 | 5,092 | 3,716,708 | 2,210,548 | 217 | 13,727 | 248 (0) | 5,237 | 4,584 | 1,843 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 75 | 3% | 1 | 75 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 37 | 2% | 1 | 37 |
| cemeteries | 5 | 0% | 4 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 15 | 1% | 10 | 3 |
| snow tone | 12 | 1% | 1 | 12 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 2 | 0% | 1 | 2 |
| streets | 32 | 1% | 1 | 32 |
| sidewalks | 31 | 1% | 1 | 31 |
| berms and driveways | 23 | 1% | 1 | 23 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 119 | 5% | 21 | 15 |
| interior plans | 162 | 7% | 7 | 33 |
| interior shell | 656 | 30% | 116 | 34 |
| interior walls | 134 | 6% | 116 | 8 |
| interior doors | 522 | 24% | 116 | 17 |
| cutaway | 55 | 3% | 15 | 8 |
| utility lines | 5 | 0% | 1 | 5 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 6 | 0% | 1 | 6 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 4 | 0% | 1 | 4 |
| street furniture | 14 | 1% | 1 | 14 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 10 | 0% | 1 | 10 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 5 | 0% | 1 | 5 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 4 | 0% | 1 | 4 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 10 | 0% | 15 | 1 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 1 | 0% | 1 | 1 |
| kerbside cars | 97 | 4% | 21 | 46 |
| parking lot cars | 2 | 0% | 3 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 45 | 2% | 10 | 6 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 2 | 0% | 1 | 2 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 20 | 1% | 1 | 20 |
| (emit the outdoor meshes) | 51 | 2% | 1 | 51 |
| **total** | **2,174** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **65** | data 17 | 0 | 90 | 62 | 96,008 | 50,076 | 0 | 66 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **43** | cutaway 11 | 0 | 96 | 64 | 133,999 | 76,961 | 5 | 115 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 150 + 9 | **152** | cutaway 60 | 0 | 95 | 50 | 178,946 | 93,179 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 451 + 36 | **74** | buildings 26 | 0 | 110 | 61 | 96,167 | 52,289 | 3 | 43 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 2 | **21** | streets 3 | 0 | 68 | 40 | 60,699 | 44,753 | 0 | 2 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 16.8 | 1 | 16.8 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 2.0 | 1 | 2.0 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 8.0 | 1 | 8.0 |
| sidewalks | 1.6 | 1 | 1.6 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.9 | 2 | 4.4 |
| cutaway | 14.9 | 2 | 14.8 |
| utility lines | 1.3 | 1 | 1.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.7 | 1 | 0.7 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 2.5 | 1 | 2.5 |
| street furniture | 1.0 | 1 | 1.0 |
| landmarks | 0.5 | 1 | 0.5 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.7 | 1 | 0.7 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.1 | 1 | 0.1 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 2.2 | 1 | 2.2 |

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
| yards | 3.8 | 2 | 2.2 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 1.7 | 1 | 1.7 |
| sidewalks | 0.3 | 1 | 0.3 |
| berms and driveways | 0.5 | 1 | 0.5 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 11.4 | 2 | 11.0 |
| cutaway | 12.6 | 2 | 11.2 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.0 | 1 | 0.0 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.9 | 1 | 0.9 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.4 | 4 | 0.4 |
| driveway cars | 0.7 | 1 | 0.7 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.2 | 1 | 0.2 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.4 | 1 | 0.4 |
| outdoor | 2.6 | 1 | 2.6 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 13.3 | 1 | 13.3 |
| water | 13.9 | 1 | 13.9 |
| fields | 20.1 | 1 | 20.1 |
| parking | 1.1 | 1 | 1.1 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.3 | 2 | 1.3 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.2 | 1 | 1.2 |
| sidewalks | 1.1 | 1 | 1.1 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.8 | 1 | 0.8 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 17.4 | 2 | 10.9 |
| cutaway | 60.3 | 1 | 60.3 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 3.1 | 1 | 3.1 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 2.4 | 3 | 1.2 |
| parking lot cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.3 | 1 | 3.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.4 | 1 | 0.4 |
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
| yards | 8.6 | 2 | 8.6 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 6.7 | 1 | 6.7 |
| sidewalks | 2.0 | 1 | 2.0 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 25.7 | 1 | 25.7 |
| cutaway | 12.7 | 1 | 12.7 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.1 | 1 | 1.1 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 0.3 | 1 | 0.3 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 2.2 | 3 | 0.8 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.0 | 1 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.2 | 1 | 1.2 |
| overhead | 0.5 | 1 | 0.5 |
| outdoor | 1.9 | 1 | 1.9 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 1.7 | 1 | 1.7 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
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
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.9 | 1 | 0.9 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 4.6 | 4 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 1.8 | 3 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 2.2 | 1 | 2.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 78 | 16 |
| 186_141 | yes | 13 | 3 |
| 185_141 | yes | 8 | 2 |
| 185_142 | yes | 18 | 11 |
| 183_148 | yes | 150 | 9 |
| 196_145 | yes | 451 | 36 |
| 210_134 | yes | 6 | 2 |
| 182_148 | yes | 167 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 739 ms (slowest 112 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **69** | cutaway 26 | 0 | 95 | 50 | 178,946 | 93,179 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **102** | interior plans 27 | 1 | 100 | 75 | 242,650 | 126,747 | 0 | 129 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 167 + 8 | **67** | cutaway 18 | 0 | 93 | 49 | 110,060 | 61,465 | 0 | 66 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **92** | buildings 33 | 0 | 105 | 56 | 268,558 | 139,661 | 1 | 84 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **106** | cutaway 24 | 0 | 102 | 57 | 244,011 | 127,745 | 1 | 47 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **80** | cutaway 28 | 0 | 80 | 46 | 189,632 | 101,351 | 0 | 55 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **67** | buildings 15 | 0 | 97 | 51 | 157,772 | 83,847 | 0 | 49 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **43** | cutaway 8 | 0 | 92 | 49 | 107,037 | 62,390 | 2 | 16 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **112** | cutaway 57 | 0 | 77 | 43 | 211,086 | 114,038 | 1 | 45 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 843 | 476 | 476 | 473 | 1,709,752 | 910,423 | 126 | 1,976 | 5 (0) | 556 | 318 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (96,008 vertices, 50,076 triangles, 90 objects, 62 meshes; hash 6302bf078d63697a vs 6302bf078d63697a; 70 colliders, lights and loot containers, hash 58044ecab8d8142b vs 58044ecab8d8142b). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 65 | 48 / 1 / 0.1 | 0 / 0.0 | 60 / 93,321 / 2.8 | 5.1 | 60.0 | 41 / 3.6 |
| rural-5km | Full | 43 | 84 / 1 / 0.1 | 0 / 0.0 | 63 / 131,399 / 3.8 | 6.2 | 36.7 | 43 / 0.3 |
| louisville-downtown | Full | 152 | 102 / 1 / 0.1 | 0 / 0.0 | 49 / 176,345 / 15.9 | 17.8 | 133.7 | 37 / 0.3 |
| lexington-downtown | Full | 74 | 66 / 1 / 0.1 | 0 / 0.0 | 56 / 93,358 / 2.0 | 3.5 | 70.7 | 41 / 0.2 |
| hazard | Full | 21 | 165 / 1 / 0.2 | 0 / 0.0 | 35 / 57,234 / 1.2 | 2.1 | 18.7 | 28 / 0.2 |
| louisville 0,0 | Full | 69 | 102 / 1 / 0.1 | 0 / 0.0 | 49 / 176,345 / 4.4 | 6.2 | 62.8 | 37 / 0.2 |
| louisville 1,0 | Full | 102 | 71 / 1 / 0.1 | 0 / 0.0 | 73 / 239,980 / 5.8 | 8.4 | 93.5 | 46 / 0.4 |
| louisville -1,0 | Full | 67 | 147 / 1 / 0.1 | 0 / 0.0 | 46 / 107,397 / 2.4 | 3.8 | 63.6 | 37 / 0.2 |
| louisville 0,1 | Full | 92 | 87 / 1 / 0.1 | 0 / 0.0 | 52 / 265,829 / 6.0 | 7.7 | 84.0 | 37 / 0.2 |
| louisville 0,-1 | Full | 106 | 82 / 1 / 0.3 | 0 / 0.0 | 53 / 241,239 / 5.6 | 8.7 | 97.3 | 37 / 0.9 |
| louisville 1,1 | Full | 80 | 65 / 1 / 0.1 | 0 / 0.0 | 45 / 187,031 / 4.1 | 5.6 | 74.2 | 35 / 0.2 |
| louisville -1,1 | Full | 67 | 123 / 1 / 0.2 | 0 / 0.0 | 49 / 155,106 / 3.7 | 5.7 | 60.9 | 37 / 0.3 |
| louisville 1,-1 | Full | 43 | 93 / 1 / 0.1 | 0 / 0.0 | 44 / 103,736 / 2.8 | 4.2 | 39.2 | 34 / 0.2 |
| louisville -1,-1 | Full | 112 | 112 / 1 / 0.1 | 0 / 0.0 | 40 / 208,378 / 5.8 | 7.3 | 104.8 | 31 / 0.2 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 170 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 43 | 15 | 35% | cutaway 13.1 | streets 6.9 | 96,008 / 11,363 | 90 / 37 | 62 / 32 | 66 / 1 | 0 / 0 |
| rural-5km | 29 | 6 | 21% | cutaway 9.9 | streets 1.4 | 132,668 / 8,003 | 95 / 38 | 64 / 33 | 115 / 1 | 5 / 0 |
| louisville-downtown | 81 | 13 | 17% | cutaway 29.0 | buildings 3.7 | 192,675 / 15,139 | 95 / 36 | 50 / 31 | 65 / 1 | 0 / 0 |
| lexington-downtown | 59 | 13 | 22% | cutaway 18.6 | streets 6.1 | 131,543 / 11,694 | 97 / 32 | 56 / 27 | 42 / 1 | 0 / 0 |
| hazard | 22 | 8 | 38% | streets 3.5 | streets 3.0 | 66,973 / 8,355 | 73 / 33 | 40 / 28 | 2 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 73,873, normals 69,223, UVs 78,946; largest difference 3.93E-06 m, 1.2E-05 (normals), 0.000614 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 61.67 | 30.83 | make 52.73 + switch on 8.94 ms; 1602 objects; destroy 4.66 ms; 3,000 rays 2.89 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 15.36 | 7.68 | make 10.42 + switch on 4.94 ms; 10 objects; destroy 1.77 ms; 3,000 rays 2.03 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 14.05 | 7.03 | make 6.03 + switch on 8.02 ms; 2 objects; destroy 0.11 ms; 3,000 rays 2.19 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 5.34 | 2.67 | make 5.30 + switch on 0.04 ms (+ 9.42 ms baking on the worker); 2 objects; destroy 0.13 ms; 3,000 rays 2.56 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 7.66 | 191.57 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 12.56 | 313.92 | objects 1.02, allocate 0.43, pack 10.03, apply 1.07 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.15 | 28.65 | objects 1.00, allocate 0.01, pack 6.86 (worker), apply 0.13 ms |
| primitives | Box, turned, flat colour | 20,000 | 19.39 | 0.97 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 85.48 | 4.27 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 41.97 | 2.10 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 107.60 | 5.38 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 14.09 | 0.70 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 17.10 | 0.86 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 5.39 | 0.27 | 80,000 vertices |
| primitives | Tri | 20,000 | 3.42 | 0.17 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 19.89 | 3.98 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 45.59 | 9.12 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 26.28 | 8.76 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 41.43 | 13.81 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.67 | 7.33 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.05 | 12.05 |  |
| objects | BoxCollider added to one object | 2,000 | 40.84 | 20.42 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 21.61 | 108.06 |  |
| objects | RoomLight.Spawn | 100 | 11.77 | 117.66 |  |
| objects | switching all of the above on | 1 | 30.33 | 30328.30 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 16.10 | 16098.00 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.65 | 10.60 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.20 | 220.36 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 405.18 | 405181.00 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 398.54 | 398536.20 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 23.97 | 23965.40 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,277 ms, 3,877,357 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (28) | 0.49 | 0.04 | 5.14 | 8.03 | 40.35 | 27 / 5 / 1 / 1 | cutaway 39.7 | 5 (5 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 / 2,193 / 4,353,633 | 2 | 291 / 903 |
| drive-east | 2400 (150) | 0.51 | 0.04 | 5.24 | 9.48 | 37.01 | 143 / 35 / 6 / 1 | trees 37.0 | 35 (35 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,242 / 6,481,432 | 2 | 234 / 991 |
| teleport-louisville | 720 (177) | 3.08 | 0.04 | 15.94 | 35.21 | 67.51 | 179 / 82 / 36 / 12 | yards 66.1 | 25 (25 / 0) | 0 | 30 | 0.2 | 1.1 | 10 / 68 | 50 / 3,226 / 7,983,930 | 4 | 221 / 1,046 |
| drive-louisville | 1800 (108) | 0.65 | 0.04 | 5.28 | 15.78 | 38.79 | 106 / 43 / 17 / 3 | cutaway 37.5 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,444 / 8,562,569 | 6 | 272 / 1,072 |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (3x, worst 39.7 ms), buses and cruisers (1x, worst 7.0 ms), yards (1x, worst 3.4 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: trees (10x, worst 37.0 ms), cutaway (18x, worst 28.5 ms), interior plans (1x, worst 11.1 ms), buildings (2x, worst 10.7 ms), yards (2x, worst 10.5 ms), power lines (1x, worst 6.7 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: yards (1x, worst 66.1 ms), cutaway (24x, worst 54.6 ms), fields (13x, worst 52.1 ms), interior plans (3x, worst 47.2 ms), buildings (22x, worst 32.3 ms), parking (1x, worst 30.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (14x, worst 37.5 ms), buildings (14x, worst 34.6 ms), utility lines (1x, worst 32.3 ms), fields (7x, worst 14.1 ms), outdoor (1x, worst 9.8 ms), yards (1x, worst 9.6 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 6,343 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 333 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.484 | 0% | 3,612 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.115 | 0% | 5,483 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.475 | 0% | 67 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 65 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.474 | 0% | 81 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.088 | 0% | 95 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.444 | 0% | 93 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 116 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 231 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 118 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 123 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 122 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.472 | 0% | 87 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.077 | 0% | 196 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 73 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 80 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.448 | 0% | 168 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.190 | 19% | 154 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.405 | 0% | 82 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.169 | 20% | 226 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 197 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 183 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 74 |

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
| setup | yes | 71 |  |
| square: KentuckyMap.Build | yes | 5,306 |  |
| square: count | yes | 133 |  |
| square: pick spots | yes | 93 |  |
| square: pictures | yes | 20,577 |  |
| square: discard | yes | 140 |  |
| square: passes (warm rebuild) | yes | 2,312 |  |
| chunks: stress test | yes | 1,177 |  |
| chunks: Louisville 3x3 | yes | 1,979 |  |
| chunks: determinism | yes | 178 |  |
| perf: Lite against Full | yes | 395 |  |
| perf: determinism across build orders | yes | 358 |  |
| perf: what things cost | yes | 1,703 |  |
| perf: walking and driving through the state | yes | 159,025 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
