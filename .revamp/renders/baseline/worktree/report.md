# CLONE-Z map harness: Bardstown (build ky-local-f1)

**Exit 0**: ok. Full run, 8.2 s, started 2026-10-23 22:29:00.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,227 ms |
| the same square again, warm, pass by pass | 443 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -6.2, 12.0, facing -117° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 170 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f1): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 77 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,398 | 1,115 | 1,115 | 279 | 673,004 | 415,138 | 44 | 2,409 | 143 (66) | 3,165 | 905 | 433 |
| RaidOverhead | 171 | 170 | 170 | 170 | 76,683 | 40,533 | 89 | 870 | 0 (0) | 0 | 170 | 0 |
| RaidScenery | 72 | 71 | 71 | 71 | 34,243 | 16,483 | 17 | 342 | 0 (0) | 0 | 39 | 0 |
| **all** | 3,641 | 1,356 | 1,356 | 520 | 783,930 | 472,154 | 117 | 3,621 | 143 (66) | 3,165 | 1,114 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 50 | 11% | 1 | 50 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 1 | 0% | 1 | 1 |
| streets | 32 | 7% | 1 | 32 |
| sidewalks | 12 | 3% | 1 | 12 |
| lines | 0 | 0% | 1 | 0 |
| buildings | 211 | 48% | 41 | 21 |
| roof eaves | 11 | 3% | 1 | 11 |
| kerbs and poles | 16 | 4% | 1 | 16 |
| road markings | 0 | 0% | 1 | 0 |
| facades | 3 | 1% | 1 | 3 |
| street lights | 4 | 1% | 1 | 4 |
| street lamps | 19 | 4% | 1 | 19 |
| signposts | 3 | 1% | 1 | 3 |
| marked street lamps | 10 | 2% | 1 | 10 |
| hydrants | 0 | 0% | 1 | 0 |
| snow piles | 2 | 1% | 1 | 2 |
| parked cars | 4 | 1% | 1 | 4 |
| trees | 4 | 1% | 1 | 4 |
| points | 1 | 0% | 1 | 1 |
| (emit the overhead meshes) | 11 | 2% | 1 | 11 |
| (emit the outdoor meshes) | 49 | 11% | 1 | 49 |
| **total** | **443** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **38** | buildings 16 | 1 | 265 | 120 | 87,629 | 55,041 | 6 | 235 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **19** | buildings 7 | 1 | 389 | 106 | 50,467 | 28,911 | 6 | 339 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 108 + 7 | **92** | buildings 52 | 1 | 240 | 97 | 271,071 | 144,687 | 2 | 202 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 350 + 18 | **30** | buildings 13 | 1 | 194 | 102 | 85,577 | 52,657 | 2 | 178 | 6 | 43/6/5 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 4 + 1 | **8** | outdoor 2 | 0 | 62 | 30 | 32,578 | 21,904 | 2 | 52 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.8 | 1 | 1.8 |
| ground | 0.4 | 1 | 0.4 |
| water and lots | 0.0 | 1 | 0.0 |
| streets | 4.5 | 1 | 4.5 |
| sidewalks | 3.3 | 1 | 3.3 |
| lines | 0.0 | 1 | 0.0 |
| buildings | 16.4 | 1 | 16.4 |
| roof eaves | 3.5 | 1 | 3.5 |
| kerbs and poles | 0.5 | 1 | 0.5 |
| road markings | 0.0 | 1 | 0.0 |
| facades | 0.1 | 1 | 0.1 |
| street lights | 0.8 | 1 | 0.8 |
| street lamps | 0.9 | 1 | 0.9 |
| signposts | 0.1 | 1 | 0.1 |
| marked street lamps | 0.4 | 1 | 0.4 |
| hydrants | 0.0 | 1 | 0.0 |
| snow piles | 0.6 | 1 | 0.6 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.7 | 1 | 0.7 |
| outdoor | 3.2 | 1 | 3.2 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water and lots | 0.0 | 1 | 0.0 |
| streets | 0.7 | 1 | 0.7 |
| sidewalks | 0.2 | 1 | 0.2 |
| lines | 0.0 | 1 | 0.0 |
| buildings | 13.1 | 2 | 6.6 |
| roof eaves | 0.7 | 1 | 0.7 |
| kerbs and poles | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| facades | 0.1 | 1 | 0.1 |
| street lights | 0.4 | 1 | 0.4 |
| street lamps | 0.5 | 1 | 0.5 |
| signposts | 0.1 | 1 | 0.1 |
| marked street lamps | 0.0 | 1 | 0.0 |
| hydrants | 0.0 | 1 | 0.0 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.7 | 1 | 0.7 |
| outdoor | 2.1 | 1 | 2.1 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 1.3 | 1 | 1.3 |
| streets | 7.1 | 1 | 7.1 |
| sidewalks | 14.3 | 1 | 14.3 |
| lines | 0.0 | 1 | 0.0 |
| buildings | 52.3 | 1 | 52.3 |
| roof eaves | 0.0 | 1 | 0.0 |
| kerbs and poles | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| facades | 0.0 | 1 | 0.0 |
| street lights | 0.2 | 1 | 0.2 |
| street lamps | 0.2 | 1 | 0.2 |
| signposts | 0.0 | 1 | 0.0 |
| marked street lamps | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.3 | 1 | 0.3 |
| points | 7.1 | 1 | 7.1 |
| overhead | 4.9 | 1 | 4.9 |
| outdoor | 3.6 | 1 | 3.6 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| streets | 2.9 | 1 | 2.9 |
| sidewalks | 1.8 | 1 | 1.8 |
| lines | 0.0 | 1 | 0.0 |
| buildings | 12.7 | 1 | 12.7 |
| roof eaves | 0.0 | 1 | 0.0 |
| kerbs and poles | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| facades | 0.0 | 1 | 0.0 |
| street lights | 0.3 | 1 | 0.3 |
| street lamps | 0.2 | 1 | 0.2 |
| signposts | 0.0 | 1 | 0.0 |
| marked street lamps | 0.0 | 1 | 0.0 |
| hydrants | 0.0 | 1 | 0.0 |
| snow piles | 0.4 | 1 | 0.4 |
| parked cars | 0.3 | 1 | 0.3 |
| trees | 0.4 | 1 | 0.4 |
| points | 0.2 | 1 | 0.2 |
| overhead | 1.0 | 1 | 1.0 |
| outdoor | 9.4 | 1 | 9.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water and lots | 1.3 | 1 | 1.3 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 0.4 | 1 | 0.4 |
| lines | 0.0 | 1 | 0.0 |
| roof eaves | 0.0 | 1 | 0.0 |
| kerbs and poles | 0.1 | 1 | 0.1 |
| road markings | 0.0 | 1 | 0.0 |
| facades | 0.0 | 1 | 0.0 |
| street lights | 0.4 | 1 | 0.4 |
| street lamps | 0.2 | 1 | 0.2 |
| signposts | 0.1 | 1 | 0.1 |
| marked street lamps | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.4 | 1 | 0.4 |
| trees | 0.8 | 1 | 0.8 |
| points | 0.1 | 1 | 0.1 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 2.0 | 1 | 2.0 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 35 | 7 |
| 186_141 | yes | 9 | 8 |
| 185_141 | yes | 6 | 1 |
| 185_142 | yes | 7 | 2 |
| 183_148 | yes | 108 | 7 |
| 196_145 | yes | 350 | 18 |
| 210_134 | yes | 4 | 1 |
| 182_148 | yes | 95 | 6 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 732 ms (slowest 134 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **83** | buildings 58 | 1 | 240 | 97 | 271,071 | 144,687 | 2 | 202 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **132** | buildings 76 | 1 | 433 | 178 | 521,463 | 267,133 | 3 | 402 | 17 | 11/17/12 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 95 + 6 | **30** | roof eaves 10 | 1 | 202 | 69 | 99,007 | 60,027 | 3 | 174 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **134** | buildings 71 | 1 | 275 | 111 | 479,156 | 248,620 | 6 | 221 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **131** | roof eaves 102 | 1 | 171 | 81 | 550,853 | 287,312 | 3 | 144 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **94** | roof eaves 74 | 1 | 178 | 87 | 339,913 | 174,833 | 1 | 140 | 5 | 11/5/2 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **45** | roof eaves 29 | 1 | 215 | 87 | 199,658 | 110,072 | 5 | 182 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **22** | sidewalks 7 | 0 | 94 | 49 | 61,097 | 43,703 | 5 | 61 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **60** | roof eaves 46 | 1 | 194 | 77 | 167,017 | 94,180 | 6 | 163 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 2,004 | 836 | 836 | 389 | 2,689,235 | 1,430,567 | 51 | 2,258 | 34 (3) | 1,689 | 666 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **MISMATCH**: objects 267 vs 267, vertices 87706 vs 87706, triangles 55161 vs 55161; 6 of 121 meshes differ (positions to the millimetre) (87,706 vertices, 55,161 triangles, 267 objects, 121 meshes; hash f83df0f8f8f87fc9 vs d1f9532eefaacaca).

Meshes that came out different (first 6):

- `root/RoomLight/Bulb (77 vertices at -8.6, -35.9) vs root/RoomLight/Bulb (77 vertices at -8.7, -35.9)`
- `root/RoomLight/Bulb (77 vertices at -0.7, -39.2) vs root/RoomLight/Bulb (77 vertices at -0.8, -39.1)`
- `root/RoomLight/Bulb (77 vertices at 8.5, -39.6)`
- `root/OutdoorProps (1656 vertices at -13.5, -48.5)`
- `root/OutdoorProps (2570 vertices at 21.5, -48.5)`
- `overhead/Overhead/Overhead (444 vertices at -20.0, 60.0)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.615 | 0% | 799 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 137 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.571 | 0% | 129 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.100 | 0% | 73 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.560 | 0% | 57 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.088 | 0% | 77 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.524 | 0% | 54 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.087 | 0% | 67 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.549 | 0% | 59 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.094 | 0% | 64 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.524 | 0% | 49 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 93 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.282 | 0% | 69 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 61 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.558 | 0% | 58 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.084 | 0% | 54 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.590 | 0% | 50 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 47 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.484 | 0% | 62 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.156 | 36% | 55 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.490 | 0% | 67 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.145 | 37% | 97 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.594 | 0% | 223 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 53 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 50 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -6.2, 12.0 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 58 |  |
| square: KentuckyMap.Build | yes | 1,228 |  |
| square: count | yes | 11 |  |
| square: pick spots | yes | 64 |  |
| square: pictures | yes | 3,958 |  |
| square: discard | yes | 13 |  |
| square: passes (warm rebuild) | yes | 458 |  |
| chunks: stress test | yes | 765 |  |
| chunks: Louisville 3x3 | yes | 1,504 |  |
| chunks: determinism | yes | 106 |  |

## Notes

- Determinism: chunk 11913_9092 came out different the second time (objects 267 vs 267, vertices 87706 vs 87706, triangles 55161 vs 55161; 6 of 121 meshes differ (positions to the millimetre)), first: root/RoomLight/Bulb (77 vertices at -8.6, -35.9) vs root/RoomLight/Bulb (77 vertices at -8.7, -35.9); root/RoomLight/Bulb (77 vertices at -0.7, -39.2) vs root/RoomLight/Bulb (77 vertices at -0.8, -39.1); root/RoomLight/Bulb (77 vertices at 8.5, -39.6); root/OutdoorProps (1656 vertices at -13.5, -48.5); root/OutdoorProps (2570 vertices at 21.5, -48.5); overhead/Overhead/Overhead (444 vertices at -20.0, 60.0)

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
