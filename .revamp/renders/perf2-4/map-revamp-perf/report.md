# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 183.7 s, started 2026-10-24 10:08:30.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 4,382 ms |
| the same square again, warm, pass by pass | 3,202 ms |
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

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 13,542 | 4,428 | 4,428 | 4,219 | 2,072,226 | 1,316,584 | 41 | 9,066 | 210 (0) | 3,293 | 3,223 | 1,327 |
| World/Building shells | 118 | 117 | 117 | 117 | 728,200 | 366,092 | 48 | 1,131 | 0 (0) | 0 | 117 | 0 |
| RaidOverhead/Building shells | 118 | 117 | 117 | 117 | 269,865 | 132,262 | 117 | 810 | 0 (0) | 0 | 117 | 0 |
| World/Loot | 680 | 679 | 679 | 2 | 34,408 | 27,392 | 9 | 679 | 0 (0) | 679 | 679 | 396 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 336,081 | 191,228 | 21 | 590 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 141 | 141 | 141 | 141 | 138,347 | 59,099 | 11 | 506 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 131 | 131 | 131 | 131 | 48,848 | 27,400 | 43 | 449 | 0 (0) | 0 | 131 | 0 |
| RaidScenery/SceneryProps | 45 | 45 | 45 | 45 | 58,302 | 35,180 | 31 | 232 | 0 (0) | 0 | 45 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 75 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 28 | 28 | 28 | 28 | 4,320 | 7,614 | 1 | 28 | 0 (0) | 0 | 28 | 0 |
| RaidOverhead/Wires | 25 | 25 | 25 | 25 | 3,627 | 6,300 | 1 | 25 | 0 (0) | 0 | 25 | 0 |
| World/StreetLamp | 42 | 21 | 21 | 3 | 1,573 | 2,440 | 3 | 21 | 21 (0) | 21 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 20,987 | 40,958 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 10,340 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 67 | 2% | 1 | 67 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 37 | 1% | 1 | 37 |
| cemeteries | 5 | 0% | 4 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 11 | 0% | 10 | 2 |
| snow tone | 11 | 0% | 1 | 11 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 2 | 0% | 1 | 2 |
| streets | 32 | 1% | 1 | 32 |
| sidewalks | 51 | 2% | 1 | 51 |
| berms and driveways | 11 | 0% | 1 | 11 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 129 | 4% | 21 | 19 |
| interior plans | 174 | 5% | 7 | 30 |
| interior shell | 1,178 | 37% | 116 | 76 |
| interior walls | 215 | 7% | 116 | 29 |
| interior doors | 845 | 26% | 116 | 42 |
| cutaway | 84 | 3% | 15 | 13 |
| utility lines | 21 | 1% | 1 | 21 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 23 | 1% | 1 | 23 |
| street lights | 6 | 0% | 1 | 6 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 9 | 0% | 1 | 9 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 9 | 0% | 1 | 9 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 5 | 0% | 1 | 5 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 13 | 0% | 15 | 3 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 36 | 1% | 22 | 3 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 106 | 3% | 10 | 36 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 7 | 0% | 1 | 7 |
| (emit the overhead meshes) | 17 | 1% | 1 | 17 |
| (emit the outdoor meshes) | 75 | 2% | 1 | 75 |
| **total** | **3,202** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **82** | cutaway 23 | 1 | 116 | 68 | 95,372 | 50,284 | 3 | 105 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **43** | cutaway 12 | 1 | 118 | 64 | 133,759 | 76,841 | 5 | 197 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 200 + 12 | **136** | cutaway 34 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 523 + 61 | **72** | buildings 24 | 0 | 153 | 54 | 94,666 | 50,719 | 0 | 105 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **21** | streets 4 | 0 | 101 | 39 | 60,103 | 44,413 | 0 | 165 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.2 | 1 | 2.2 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.5 | 1 | 1.5 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 8.4 | 1 | 8.4 |
| sidewalks | 2.3 | 1 | 2.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.8 | 2 | 6.0 |
| cutaway | 22.6 | 2 | 22.5 |
| utility lines | 1.7 | 1 | 1.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.8 | 1 | 0.8 |
| signs | 9.2 | 1 | 9.2 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 0.4 | 1 | 0.4 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 10.6 | 1 | 10.6 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.2 | 1 | 0.2 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.8 | 1 | 0.8 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.1 | 1 | 0.1 |
| colliders | 0.4 | 1 | 0.4 |
| objects | 2.8 | 1 | 2.8 |
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
| yards | 3.8 | 2 | 2.2 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 0.3 | 1 | 0.3 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.2 | 2 | 11.8 |
| cutaway | 13.4 | 2 | 12.1 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.8 | 1 | 0.8 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.4 | 4 | 0.4 |
| driveway cars | 0.6 | 1 | 0.6 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 2 | 1.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.3 | 1 | 0.3 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 1.7 | 1 | 1.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 10.0 | 1 | 10.0 |
| water | 14.5 | 1 | 14.5 |
| fields | 22.5 | 1 | 22.5 |
| parking | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.2 | 2 | 1.1 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 0.8 | 1 | 0.8 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.1 | 1 | 1.1 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.6 | 1 | 4.6 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 15.3 | 2 | 10.0 |
| cutaway | 34.0 | 1 | 34.0 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 4.3 | 1 | 4.3 |
| nature plan | 0.7 | 1 | 0.7 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.7 | 1 | 0.7 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.1 | 1 | 0.1 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 3.1 | 3 | 1.6 |
| parking lot cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 12.2 | 1 | 12.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.5 | 1 | 1.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 8.7 | 2 | 8.7 |
| snow tone | 1.9 | 1 | 1.9 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 7.3 | 1 | 7.3 |
| sidewalks | 2.2 | 1 | 2.2 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.7 | 1 | 0.7 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 23.5 | 1 | 23.5 |
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
| trees | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.9 | 3 | 0.7 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.1 | 1 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

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
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 3.5 | 1 | 3.5 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.7 | 1 | 0.7 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 5.6 | 4 | 2.7 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 2.1 | 3 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 54 | 34 |
| 186_141 | yes | 34 | 4 |
| 185_141 | yes | 9 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 200 | 12 |
| 196_145 | yes | 523 | 61 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 140 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 924 ms (slowest 135 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **70** | cutaway 26 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **133** | interior plans 36 | 1 | 140 | 75 | 242,361 | 126,463 | 0 | 198 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 140 + 8 | **75** | cutaway 19 | 1 | 226 | 56 | 110,356 | 61,805 | 0 | 207 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **132** | cutaway 56 | 1 | 178 | 53 | 267,161 | 138,369 | 0 | 165 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **134** | fences and walls 35 | 1 | 170 | 61 | 245,587 | 128,383 | 1 | 128 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **79** | cutaway 34 | 1 | 126 | 52 | 190,554 | 101,967 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **117** | buildings 42 | 1 | 208 | 54 | 157,791 | 83,737 | 0 | 165 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **48** | cutaway 8 | 1 | 153 | 44 | 104,889 | 60,042 | 0 | 102 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **135** | cutaway 58 | 1 | 162 | 46 | 211,115 | 113,812 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,551 | 495 | 495 | 495 | 1,709,583 | 908,289 | 125 | 1,974 | 1 (0) | 1,405 | 341 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (95,372 vertices, 50,284 triangles, 116 objects, 68 meshes; hash e8ef7e9829059ac0 vs e8ef7e9829059ac0; 112 colliders, lights and loot containers, hash 6074aea0d42f2c35 vs 6074aea0d42f2c35). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 82 | 37 / 18 / 0.3 | 0 / 0.0 | 61 / 92,441 / 2.8 | 3.8 | 78.1 | 44 / 5.5 |
| rural-5km | Full | 43 | 83 / 24 / 0.8 | 0 / 0.0 | 63 / 131,159 / 2.7 | 5.1 | 36.7 | 43 / 0.4 |
| louisville-downtown | Full | 136 | 102 / 88 / 1.5 | 0 / 0.0 | 52 / 177,153 / 5.2 | 10.4 | 124.4 | 37 / 0.4 |
| lexington-downtown | Full | 72 | 66 / 54 / 0.6 | 0 / 0.0 | 51 / 92,012 / 1.4 | 2.4 | 68.9 | 38 / 0.4 |
| hazard | Full | 21 | 164 / 35 / 0.7 | 0 / 0.0 | 34 / 56,674 / 0.2 | 0.6 | 20.2 | 28 / 0.3 |
| louisville 0,0 | Full | 70 | 102 / 88 / 1.1 | 0 / 0.0 | 52 / 177,153 / 3.4 | 5.1 | 64.3 | 37 / 0.3 |
| louisville 1,0 | Full | 133 | 70 / 41 / 0.5 | 0 / 0.0 | 73 / 239,691 / 5.4 | 7.1 | 125.5 | 46 / 0.4 |
| louisville -1,0 | Full | 75 | 142 / 127 / 2.7 | 0 / 0.0 | 50 / 107,279 / 1.5 | 3.0 | 69.1 | 37 / 0.3 |
| louisville 0,1 | Full | 132 | 83 / 78 / 0.8 | 0 / 0.0 | 50 / 264,498 / 27.8 | 29.1 | 101.9 | 36 / 0.5 |
| louisville 0,-1 | Full | 134 | 82 / 65 / 0.7 | 0 / 0.0 | 57 / 242,773 / 6.4 | 9.0 | 124.7 | 37 / 0.5 |
| louisville 1,1 | Full | 79 | 67 / 41 / 0.9 | 0 / 0.0 | 49 / 187,911 / 4.7 | 6.4 | 71.6 | 35 / 0.5 |
| louisville -1,1 | Full | 117 | 117 / 109 / 1.6 | 0 / 0.0 | 51 / 155,080 / 4.2 | 5.7 | 110.0 | 37 / 0.6 |
| louisville 1,-1 | Full | 48 | 89 / 69 / 1.0 | 0 / 0.0 | 41 / 102,221 / 1.3 | 1.9 | 45.4 | 32 / 0.5 |
| louisville -1,-1 | Full | 135 | 110 / 84 / 1.5 | 0 / 0.0 | 42 / 208,443 / 4.5 | 5.8 | 127.6 | 30 / 0.4 |

The Louisville block's NavMesh: 327 sources collected in 2 ms; baked at once (as one 384 m square) in 211 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 67 | 29 | 43% | cutaway 20.0 | streets 7.4 | 95,372 / 11,363 | 116 / 37 | 68 / 32 | 105 / 1 | 3 / 0 |
| rural-5km | 71 | 11 | 16% | cutaway 30.5 | snow tone 2.0 | 132,668 / 8,003 | 114 / 38 | 64 / 33 | 192 / 1 | 5 / 0 |
| louisville-downtown | 100 | 15 | 15% | cutaway 40.9 | buildings 4.9 | 193,209 / 15,139 | 180 / 36 | 54 / 31 | 160 / 1 | 0 / 0 |
| lexington-downtown | 63 | 14 | 23% | cutaway 19.1 | streets 6.3 | 130,569 / 11,694 | 142 / 32 | 53 / 27 | 103 / 1 | 0 / 0 |
| hazard | 22 | 8 | 35% | streets 3.6 | streets 3.0 | 66,921 / 8,355 | 104 / 33 | 39 / 28 | 163 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 84.53 | 42.27 | make 71.69 + switch on 12.84 ms; 1602 objects; destroy 8.38 ms; 3,000 rays 4.09 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 27.88 | 13.94 | make 21.30 + switch on 6.58 ms; 10 objects; destroy 2.08 ms; 3,000 rays 1.94 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 14.44 | 7.22 | make 4.05 + switch on 10.39 ms; 2 objects; destroy 0.15 ms; 3,000 rays 2.13 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.69 | 1.34 | make 2.64 + switch on 0.05 ms (+ 11.25 ms baking on the worker); 2 objects; destroy 0.10 ms; 3,000 rays 3.04 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 12.62 | 315.51 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 10.44 | 260.87 | objects 1.01, allocate 0.03, pack 9.19, apply 0.21 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.53 | 38.14 | objects 1.38, allocate 0.02, pack 8.71 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 38.61 | 1.93 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 94.96 | 4.75 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 54.43 | 2.72 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 105.52 | 5.28 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 22.96 | 1.15 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 66.29 | 3.32 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.25 | 0.41 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.35 | 0.27 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 21.73 | 4.35 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 30.46 | 6.09 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 36.83 | 12.28 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 48.75 | 16.25 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 15.46 | 7.73 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 13.93 | 13.93 |  |
| objects | BoxCollider added to one object | 2,000 | 49.08 | 24.54 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 28.22 | 141.11 |  |
| objects | RoomLight.Spawn | 100 | 15.79 | 157.90 |  |
| objects | switching all of the above on | 1 | 34.64 | 34636.90 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 19.22 | 19221.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.69 | 11.27 | worst 0.13 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.67 | 266.92 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 434.52 | 434519.40 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 427.50 | 427494.60 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 31.48 | 31483.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,497 ms, 3,851,843 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (12) | 0.16 | 0.05 | 0.07 | 5.20 | 12.08 | 5 / 1 / 0 / 0 | streets 3.5 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,052 / 3,891,453 | 2 | 224 / 889 (pool 22) |
| drive-east | 2400 (201) | 0.36 | 0.05 | 2.98 | 5.60 | 14.92 | 37 / 14 / 0 / 0 | trees 14.9 | 35 (21 / 14) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,108 / 5,670,014 | 2 | 226 / 969 (pool 23) |
| teleport-louisville | 720 (167) | 1.37 | 0.05 | 5.26 | 29.29 | 40.12 | 39 / 25 / 15 / 4 | cutaway 33.0 | 25 (9 / 16) | 0 | 30 | 0.2 | 2.5 | 12 / 149 | 50 (16 Lite) / 2,800 / 5,960,786 | 4 | 230 / 976 (pool 22) |
| drive-louisville | 1800 (170) | 0.55 | 0.05 | 3.45 | 10.21 | 55.48 | 46 / 26 / 14 / 1 | cutaway 55.4 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 36 | 55 (12 Lite) / 3,194 / 7,162,250 | 6 | 308 / 1,025 (pool 26) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.5 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: trees (1x, worst 14.9 ms), cutaway (9x, worst 14.0 ms), interior plans (1x, worst 12.7 ms), buildings (2x, worst 11.8 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: cutaway (9x, worst 33.0 ms), interior plans (1x, worst 28.0 ms), water (2x, worst 24.5 ms), buildings (7x, worst 21.2 ms), streets (1x, worst 15.3 ms), fields (2x, worst 10.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (11x, worst 55.4 ms), parking (1x, worst 29.3 ms), buildings (9x, worst 27.4 ms), fields (3x, worst 12.8 ms), snow tone (1x, worst 7.3 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 34 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 1,854 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 492 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.481 | 0% | 182 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.102 | 0% | 87 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 70 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.098 | 0% | 79 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.476 | 0% | 65 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 86 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 76 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 56 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 80 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 69 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 72 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 82 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.471 | 0% | 64 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 83 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 92 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 64 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 91 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.213 | 18% | 161 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.413 | 0% | 146 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 21% | 79 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 210 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 61 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 66 |

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

### Everything logged during the run: 8 errors, 232 warnings, 498 edit-mode artefacts

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
| setup | yes | 61 |  |
| square: KentuckyMap.Build | yes | 4,384 |  |
| square: count | yes | 394 |  |
| square: pick spots | yes | 95 |  |
| square: pictures | yes | 6,398 |  |
| square: discard | yes | 117 |  |
| square: passes (warm rebuild) | yes | 3,360 |  |
| chunks: stress test | yes | 1,383 |  |
| chunks: Louisville 3x3 | yes | 2,232 |  |
| chunks: determinism | yes | 208 |  |
| perf: Lite against Full | yes | 1,925 |  |
| perf: determinism across build orders | yes | 420 |  |
| perf: what things cost | yes | 2,014 |  |
| perf: walking and driving through the state | yes | 160,689 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
