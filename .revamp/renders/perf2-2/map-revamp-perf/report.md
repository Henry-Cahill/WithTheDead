# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 191.4 s, started 2026-10-24 09:51:50.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 10,782 ms |
| the same square again, warm, pass by pass | 2,728 ms |
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
| (read the area) | 92 | 3% | 1 | 92 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 41 | 1% | 1 | 41 |
| cemeteries | 10 | 0% | 4 | 6 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 11 | 0% | 10 | 2 |
| snow tone | 12 | 0% | 1 | 12 |
| road network | 6 | 0% | 1 | 6 |
| junctions | 2 | 0% | 1 | 2 |
| streets | 36 | 1% | 1 | 36 |
| sidewalks | 26 | 1% | 1 | 26 |
| berms and driveways | 34 | 1% | 1 | 34 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 2 | 0% | 1 | 2 |
| buildings | 167 | 6% | 21 | 26 |
| interior plans | 262 | 10% | 7 | 91 |
| interior shell | 879 | 32% | 116 | 46 |
| interior walls | 173 | 6% | 116 | 11 |
| interior doors | 679 | 25% | 116 | 31 |
| cutaway | 63 | 2% | 15 | 9 |
| utility lines | 17 | 1% | 1 | 17 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 6 | 0% | 1 | 6 |
| street lights | 5 | 0% | 1 | 5 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 9 | 0% | 1 | 9 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 7 | 0% | 1 | 7 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 7 | 0% | 1 | 7 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 16 | 1% | 15 | 9 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 27 | 1% | 22 | 2 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 37 | 1% | 10 | 5 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 12 | 0% | 1 | 12 |
| (emit the outdoor meshes) | 73 | 3% | 1 | 73 |
| **total** | **2,728** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **102** | buildings 21 | 1 | 116 | 68 | 95,372 | 50,284 | 3 | 105 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **111** | buildings 49 | 1 | 118 | 64 | 133,759 | 76,841 | 5 | 197 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 242 + 82 | **217** | cutaway 75 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 860 + 66 | **117** | buildings 33 | 1 | 153 | 54 | 94,666 | 50,719 | 0 | 105 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 11 + 3 | **32** | streets 4 | 1 | 101 | 39 | 60,103 | 44,413 | 0 | 165 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.9 | 1 | 2.9 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 1.8 | 1 | 1.8 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 8.6 | 1 | 8.6 |
| sidewalks | 2.2 | 1 | 2.2 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 21.8 | 2 | 20.7 |
| cutaway | 18.3 | 2 | 18.1 |
| utility lines | 2.7 | 1 | 2.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.3 | 1 | 1.3 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 9.0 | 1 | 9.0 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 8.0 | 1 | 8.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.0 | 1 | 1.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.4 | 1 | 0.4 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.7 | 4 | 0.6 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.3 | 1 | 1.3 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.0 | 1 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.6 | 1 | 0.6 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 8.7 | 1 | 8.7 |
| meshes | 2.7 | 1 | 2.7 |

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
| yards | 5.0 | 2 | 3.2 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 2.0 | 1 | 2.0 |
| sidewalks | 0.4 | 1 | 0.4 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 49.9 | 2 | 49.5 |
| cutaway | 21.0 | 2 | 16.7 |
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
| street trees | 1.3 | 1 | 1.3 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 11.4 | 4 | 9.9 |
| driveway cars | 1.1 | 1 | 1.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.7 | 2 | 1.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.3 | 1 | 0.3 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 10.1 | 1 | 10.1 |
| meshes | 1.9 | 1 | 1.9 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.7 | 1 | 0.7 |
| ground | 27.5 | 1 | 27.5 |
| water | 19.6 | 1 | 19.6 |
| fields | 18.6 | 1 | 18.6 |
| parking | 1.1 | 1 | 1.1 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 3.5 | 2 | 3.4 |
| snow tone | 2.2 | 1 | 2.2 |
| road network | 0.8 | 1 | 0.8 |
| junctions | 1.3 | 1 | 1.3 |
| streets | 2.0 | 1 | 2.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 1.0 | 1 | 1.0 |
| paths | 6.3 | 1 | 6.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 33.2 | 2 | 19.8 |
| cutaway | 74.9 | 1 | 74.9 |
| utility lines | 1.2 | 1 | 1.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 4.5 | 1 | 4.5 |
| nature plan | 0.6 | 1 | 0.6 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.8 | 1 | 0.8 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 3.2 | 3 | 1.6 |
| parking lot cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.6 | 1 | 3.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.5 | 2 | 2.1 |
| objects | 2.2 | 1 | 2.2 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.8 | 1 | 0.8 |
| ground | 2.9 | 1 | 2.9 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 9.7 | 2 | 9.7 |
| snow tone | 2.3 | 1 | 2.3 |
| road network | 1.2 | 1 | 1.2 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 8.8 | 1 | 8.8 |
| sidewalks | 4.3 | 1 | 4.3 |
| berms and driveways | 2.1 | 1 | 2.1 |
| road markings | 0.8 | 1 | 0.8 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 33.1 | 1 | 33.1 |
| cutaway | 22.2 | 1 | 22.2 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 2.8 | 1 | 2.8 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.3 | 1 | 1.3 |
| street furniture | 1.7 | 1 | 1.7 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.9 | 1 | 0.9 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.4 | 4 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 4.6 | 3 | 1.7 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 4.4 | 1 | 4.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.7 | 1 | 0.7 |
| points | 2.3 | 1 | 2.3 |
| colliders | 0.9 | 1 | 0.9 |
| objects | 2.0 | 1 | 2.0 |
| meshes | 4.8 | 1 | 4.8 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 2.3 | 1 | 2.3 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 2.1 | 1 | 2.1 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 4.1 | 1 | 4.1 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 3.7 | 1 | 3.7 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.5 | 1 | 1.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.9 | 1 | 0.9 |
| trees | 6.5 | 4 | 2.4 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 3.0 | 3 | 1.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.5 | 1 | 0.5 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 95 | 13 |
| 186_141 | yes | 25 | 13 |
| 185_141 | yes | 20 | 22 |
| 185_142 | yes | 48 | 4 |
| 183_148 | yes | 242 | 82 |
| 196_145 | yes | 860 | 66 |
| 210_134 | yes | 11 | 3 |
| 182_148 | yes | 200 | 9 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 974 ms (slowest 162 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **98** | cutaway 37 | 1 | 186 | 54 | 179,769 | 93,711 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **162** | interior plans 53 | 1 | 140 | 75 | 242,361 | 126,463 | 0 | 198 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 200 + 9 | **84** | cutaway 22 | 2 | 226 | 56 | 110,356 | 61,805 | 0 | 207 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **124** | cutaway 49 | 1 | 178 | 53 | 267,161 | 138,369 | 0 | 165 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **153** | fences and walls 42 | 1 | 170 | 61 | 245,587 | 128,383 | 1 | 128 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **94** | cutaway 35 | 1 | 126 | 52 | 190,554 | 101,967 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **78** | cutaway 19 | 1 | 208 | 54 | 157,791 | 83,737 | 0 | 165 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **54** | cutaway 10 | 1 | 153 | 44 | 104,889 | 60,042 | 0 | 102 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **127** | cutaway 64 | 1 | 162 | 46 | 211,115 | 113,812 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,551 | 495 | 495 | 495 | 1,709,583 | 908,289 | 125 | 1,974 | 1 (0) | 1,405 | 341 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (95,372 vertices, 50,284 triangles, 116 objects, 68 meshes; hash e8ef7e9829059ac0 vs e8ef7e9829059ac0; 112 colliders, lights and loot containers, hash 6074aea0d42f2c35 vs 6074aea0d42f2c35). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 102 | 37 / 18 / 0.4 | 0 / 0.0 | 61 / 92,441 / 3.8 | 7.0 | 94.4 | 44 / 8.0 |
| rural-5km | Full | 111 | 83 / 24 / 0.8 | 0 / 0.0 | 63 / 131,159 / 6.6 | 15.1 | 95.0 | 43 / 0.6 |
| louisville-downtown | Full | 217 | 102 / 88 / 2.5 | 0 / 0.0 | 52 / 177,153 / 26.9 | 30.4 | 183.7 | 37 / 0.5 |
| lexington-downtown | Full | 117 | 66 / 54 / 0.9 | 0 / 0.0 | 51 / 92,012 / 6.5 | 4.5 | 111.7 | 38 / 0.6 |
| hazard | Full | 32 | 164 / 35 / 1.5 | 0 / 0.0 | 34 / 56,674 / 0.3 | 1.1 | 29.5 | 28 / 0.3 |
| louisville 0,0 | Full | 98 | 102 / 88 / 1.5 | 0 / 0.0 | 52 / 177,153 / 5.9 | 8.6 | 88.2 | 37 / 0.6 |
| louisville 1,0 | Full | 162 | 70 / 41 / 1.4 | 0 / 0.0 | 73 / 239,691 / 8.8 | 12.8 | 147.7 | 46 / 0.7 |
| louisville -1,0 | Full | 84 | 142 / 127 / 1.6 | 0 / 0.0 | 50 / 107,279 / 1.7 | 2.6 | 80.3 | 37 / 0.5 |
| louisville 0,1 | Full | 124 | 83 / 78 / 1.7 | 0 / 0.0 | 50 / 264,498 / 8.0 | 10.8 | 111.7 | 36 / 0.5 |
| louisville 0,-1 | Full | 153 | 82 / 65 / 1.3 | 0 / 0.0 | 57 / 242,773 / 6.6 | 9.0 | 142.6 | 37 / 0.6 |
| louisville 1,1 | Full | 94 | 67 / 41 / 1.2 | 0 / 0.0 | 49 / 187,911 / 5.2 | 7.5 | 85.4 | 35 / 0.6 |
| louisville -1,1 | Full | 78 | 117 / 109 / 1.2 | 0 / 0.0 | 51 / 155,080 / 4.4 | 5.7 | 70.7 | 37 / 0.5 |
| louisville 1,-1 | Full | 54 | 89 / 69 / 0.9 | 0 / 0.0 | 41 / 102,221 / 1.7 | 2.6 | 50.3 | 32 / 0.5 |
| louisville -1,-1 | Full | 127 | 110 / 84 / 3.3 | 0 / 0.0 | 42 / 208,443 / 4.5 | 10.2 | 113.3 | 30 / 1.5 |

The Louisville block's NavMesh: 327 sources collected in 4 ms; baked at once (as one 384 m square) in 193 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 46 | 16 | 35% | cutaway 13.1 | streets 7.4 | 95,372 / 11,363 | 116 / 37 | 68 / 32 | 105 / 1 | 3 / 0 |
| rural-5km | 38 | 8 | 20% | cutaway 12.7 | streets 1.7 | 132,668 / 8,003 | 114 / 38 | 64 / 33 | 192 / 1 | 5 / 0 |
| louisville-downtown | 80 | 15 | 18% | cutaway 28.8 | buildings 4.2 | 193,209 / 15,139 | 180 / 36 | 54 / 31 | 160 / 1 | 0 / 0 |
| lexington-downtown | 64 | 14 | 22% | cutaway 17.9 | streets 6.6 | 130,569 / 11,694 | 142 / 32 | 53 / 27 | 103 / 1 | 0 / 0 |
| hazard | 23 | 9 | 38% | streets 3.7 | streets 3.2 | 66,921 / 8,355 | 104 / 33 | 39 / 28 | 163 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 100.01 | 50.01 | make 86.07 + switch on 13.94 ms; 1602 objects; destroy 6.04 ms; 3,000 rays 3.67 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 22.76 | 11.38 | make 14.28 + switch on 8.48 ms; 10 objects; destroy 2.57 ms; 3,000 rays 2.51 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 14.29 | 7.15 | make 5.70 + switch on 8.59 ms; 2 objects; destroy 0.17 ms; 3,000 rays 3.05 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.46 | 1.23 | make 2.42 + switch on 0.04 ms (+ 9.83 ms baking on the worker); 2 objects; destroy 0.09 ms; 3,000 rays 3.18 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.40 | 209.97 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.98 | 224.60 | objects 0.97, allocate 0.01, pack 7.83, apply 0.16 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 4.77 | 119.36 | objects 1.40, allocate 3.22, pack 8.85 (worker), apply 0.16 ms |
| primitives | Box, turned, flat colour | 20,000 | 41.87 | 2.09 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 87.15 | 4.36 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 46.62 | 2.33 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 84.15 | 4.21 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 25.46 | 1.27 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 62.62 | 3.13 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.67 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.91 | 0.25 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 13.03 | 2.61 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 31.72 | 6.34 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 32.58 | 10.86 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 33.74 | 11.25 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.36 | 6.68 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 18.77 | 18.77 |  |
| objects | BoxCollider added to one object | 2,000 | 44.87 | 22.44 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 24.90 | 124.51 |  |
| objects | RoomLight.Spawn | 100 | 11.36 | 113.63 |  |
| objects | switching all of the above on | 1 | 30.84 | 30843.00 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 17.53 | 17526.20 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.97 | 15.95 | worst 0.21 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 8.98 | 897.96 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 559.39 | 559385.10 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 604.26 | 604255.30 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 35.08 | 35083.70 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,456 ms, 3,851,843 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (11) | 0.16 | 0.05 | 0.09 | 5.08 | 12.24 | 5 / 1 / 0 / 0 | streets 3.2 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,052 / 3,891,453 | 2 | 223 / 888 (pool 22) |
| drive-east | 2400 (214) | 0.41 | 0.05 | 3.32 | 6.04 | 19.17 | 40 / 13 / 3 / 0 | cutaway 19.1 | 35 (21 / 14) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,108 / 5,670,014 | 2 | 225 / 968 (pool 23) |
| teleport-louisville | 720 (162) | 1.38 | 0.05 | 5.28 | 32.58 | 42.53 | 38 / 25 / 15 / 7 | cutaway 42.5 | 25 (9 / 16) | 0 | 30 | 0.2 | 2.4 | 11 / 144 | 50 (16 Lite) / 2,800 / 5,960,786 | 4 | 229 / 975 (pool 22) |
| drive-louisville | 1800 (204) | 0.70 | 0.05 | 3.60 | 14.34 | 57.32 | 58 / 33 / 16 / 4 | cutaway 57.3 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 60 | 55 (12 Lite) / 3,194 / 7,162,250 | 6 | 270 / 1,025 (pool 26) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.8 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: cutaway (9x, worst 19.1 ms), buildings (2x, worst 16.0 ms), interior plans (1x, worst 12.6 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: cutaway (9x, worst 42.5 ms), buildings (7x, worst 34.5 ms), interior plans (1x, worst 32.5 ms), water (2x, worst 11.8 ms), fields (3x, worst 10.6 ms), streets (1x, worst 6.4 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (12x, worst 57.3 ms), wall drifts (1x, worst 34.2 ms), parking (1x, worst 29.4 ms), buildings (9x, worst 26.8 ms), park litter (1x, worst 25.8 ms), fields (3x, worst 13.4 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 2,358 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 301 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.481 | 0% | 185 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.102 | 0% | 104 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 83 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.098 | 0% | 80 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.476 | 0% | 78 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 100 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 68 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 106 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 112 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 97 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 84 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 93 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.471 | 0% | 93 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 93 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 84 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 82 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 91 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.213 | 18% | 185 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.413 | 0% | 108 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 21% | 109 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 196 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.473 | 0% | 76 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 73 |

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
| setup | yes | 124 |  |
| square: KentuckyMap.Build | yes | 10,784 |  |
| square: count | yes | 599 |  |
| square: pick spots | yes | 186 |  |
| square: pictures | yes | 7,866 |  |
| square: discard | yes | 205 |  |
| square: passes (warm rebuild) | yes | 3,003 |  |
| chunks: stress test | yes | 2,225 |  |
| chunks: Louisville 3x3 | yes | 2,507 |  |
| chunks: determinism | yes | 153 |  |
| perf: Lite against Full | yes | 1,586 |  |
| perf: determinism across build orders | yes | 403 |  |
| perf: what things cost | yes | 2,262 |  |
| perf: walking and driving through the state | yes | 159,476 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
