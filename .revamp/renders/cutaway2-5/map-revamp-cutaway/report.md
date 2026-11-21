# CLONE-Z isometric cutaway harness

Build ky-local-f2, Bardstown (fixed square, as the map harness builds it). Unity 6000.7.0a2, Direct3D11. Camera: isometric, yaw 45, pitch 38. Map data © OpenStreetMap contributors (ODbL).

KentuckyMap.Build: 4,567 ms (130 shells emitted in 12.4 ms). Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 950 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

## What the square is made of

| root | objects | renderers | vertices | building walls (renderers) | upper parts (renderers) |
|---|--:|--:|--:|--:|--:|
| World | 28,745 | 15,731 | 7,521,993 | 130 |  |
| RaidOverhead | 584 | 466 | 464,268 |  | 130 |
| RaidScenery | 116 | 115 | 80,822 |  | 0 |
| **all** | 29,445 | 16,312 | 8,067,083 | 130 | 130 |
| *every building batched (before)* | 29,215 | 16,084 | 8,067,083 | 0 | 0 |

Shells of their own cost +230 objects and +228 renderers over the same square batched (228 / 260 shell renderers: the shared batches they'd have joined mostly exist anyway).

1 site(s), 130 buildings with shells of their own registered (130 with walls, 130 with an upper part, 119 reaching 2.5 m or more above their ground storey's walls; 726 working doors in the square).

Reading their meshes (the game does it a slice of at most 0.5 ms a frame for the buildings round the survivor; here all at once): 130 roofs read, 2.3 ms each on average, 8.3 ms the longest, 259,612 bytes of heights in all. 5 buildings' walls reach above their ground storey's top (tall windows, linings): shown clipped while their upper part is lifted.

## Pictures

Each picture is the game's isometric camera at its widest zoom (12.5 m half-height) unless noted, in plain overcast daylight; the red capsule stands in for the survivor. "roofs on" = what the view showed before the cutaway; "cut" = after one scan. The heading is the camera's (the game's starts at 45° and turns a quarter at a time). The last column says what the old roof test (each building's footprint up to its highest point) would have cut instead, where that differs.

| file | shows | cut away | old test |
|---|---|---|---|
| `01_downtown_no_survivor.png` | Downtown with no survivor: every roof and upper storey shows |  |  |
| `02_house_roofs_on.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.4 m), roofs on (before) |  |  |
| `03_house_cut_045.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.4 m), heading 045° | house 2363563976: upper + walls SW |  |
| `04_house_cut_135.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.4 m), heading 135° | house 2363563976: upper + walls NW |  |
| `05_house_cut_225.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.4 m), heading 225° | house 2363563976: upper + walls NE |  |
| `06_house_cut_315.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.4 m), heading 315° | house 2363563976: upper + walls ES |  |
| `07_house_cut_close.png` | The same, closest zoom | house 2363563976: upper + walls SW |  |
| `08_house_cut_night.png` | The same by night (the map's lights on round the survivor) | house 2363563976: upper + walls SW |  |
| `09_house_both_cutaways.png` | The same with the game's other cutaway too (IsoCutaway: the interiors' partitions and props in the way); the ray cutaway hid 2 pieces | house 2363563976: upper + walls SW |  |
| `10_block_roofs_on.png` | Behind commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m) as the camera sees it, roofs on (before) |  |  |
| `11_block_behind_045.png` | Behind commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m), heading 045° | commercial 2622504470: upper + walls N |  |
| `12_block_behind_135.png` | Behind commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m), heading 135° | commercial 2622504470: upper + walls E |  |
| `13_block_behind_225.png` | Behind commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m), heading 225° | commercial 2622504470: upper + walls S |  |
| `14_block_behind_315.png` | Behind commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m), heading 315° | commercial 2622504470: upper + walls W |  |
| `15_block_inside.png` | Inside commercial 2622504470 (554 m², walls 3.0 m, top 19.5 m) | commercial 2622504470: upper + walls SW |  |
| `16_church_behind.png` | Behind church 2239973256 (1,231 m², walls 3.0 m, top 38.7 m) | church 2239973256: upper + walls E |  |
| `17_church_inside.png` | Inside church 2239973256 (1,231 m², walls 3.0 m, top 38.7 m) | church 2239973256: upper + walls SW |  |

Meshes of the square: 261 shell meshes and stand-ins checked for NaN or infinite vertices, normals and bounds: all sound.

## Walking in and out

The stand-in walks at 1.4 m/s through the front door of house 2363563976 (219 m², walls 3.0 m, top 9.4 m) from 4 m out, the camera at heading 225° facing that wall, to the middle, waits 1 s and walks back out, as the game runs it: a cutaway scan every 0.1 s, its work every frame (six a scan), from nothing read or made (a first visit). Pictures `18`-`23` are taken on the way. Changes of the house's cut:

| t (s) | stand-in | house cut |
|--:|---|---|
| 0.1 | 3.9 m outside | nothing |
| 2.6 | 0.4 m outside | upper + walls N |
| 3.0 | inside | upper + walls NE |
| 15.4 | 1.0 m outside | upper |
| 15.5 | 1.1 m outside | nothing |

Entered at 3.0 s, cut at 2.6 s; left at 14.7 s, all back at 15.5 s (restore delay 0.4 s). The worst frame on the way (scan and work): 1.02 ms; stand-ins made on the way: 4; cuts held back for one: 0.

| file | shows | house cut | |
|---|---|---|---|
| `18_walk_a_outside.png` | 4 m outside the door, walking in (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | nothing | |
| `19_walk_b_entering.png` | Just through the door (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | upper + walls NE | |
| `20_walk_c_inside.png` | Inside, after a second (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | upper + walls NE | |
| `21_walk_d_leaving.png` | Just out of the door again (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | upper + walls NE | |
| `22_walk_e_stepped_out.png` | 0.3 s later (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | upper + walls NE | |
| `23_walk_f_restored.png` | 0.8 s later (house 2363563976 (219 m², walls 3.0 m, top 9.4 m)) | nothing | |

## Scan timing

3000 scans while the stand-in walks a 70 m circle round the middle of the square, the camera turning a quarter every 750, 6 frames of the cutaway's work after each, after a first lap that read the roofs and made the stand-ins round the path as it went (its worst frame, scan and work: 1.10 ms):

| roofs told by | µs a scan | bytes the scans allocated | µs a frame (scan and work) | worst frame ms | buildings cut on average | at most | upper parts cut on average |
|---|--:|--:|--:|--:|--:|--:|--:|
| their heights (now) | 30.8 | 0 | 5.5 | 0.15 | 0.86 | 2 | 0.86 |
| footprint up to the top (before) | 4.2 | 0 | 1.0 | 0.07 | 0.87 | 3 | 0.87 |

Stand-ins made so far: 26 (2.9 ms each on average in all, the most one took 4.4 ms, made in slices of at most 1.10 ms: from sorting the triangles to the end, walls of 6,458 vertices); 2 let go of. Roofs read in slices of at most 0.75 ms. Cuts held back for a stand-in: 0, the longest 0 frame(s). The longest frame's work: 1.10 ms. Door renderers hidden with their walls: 310.

## Streamed pieces: build steps

The piece holding each place's middle, built the way the streamer builds it (`ChunkSteps`, Full detail, every step timed back to back on the main thread; the waits while its meshes are packed on a worker don't count), after a warm-up build of it, three times each (the middle total is shown, the range in brackets): with every building in the shared batches as before the cutaway, with the shells baked as `CutawayPass` emits them (on the main thread), and queued on the piece's bake (`Site.Bake`, packed on a worker with the piece's other meshes: what the game does). "interior plans" is the interiors' tail, which `CutawayPass` closes with a step of its own (it used to be charged to "cutaway"); "meshes" is the piece's own objects and meshes steps. Register = switching the piece on (its buildings into the cutaway's hash); read = every building's meshes read at once (the game reads them a slice a frame round the survivor).

| piece | shells | total ms | slowest step | cutaway steps: Σ / max ms | interior plans ms | meshes ms | register ms | read ms | objects | renderers | vertices |
|---|---|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|
| start-centre 11913_9092 | batched (before) | 42.3 (39-44) | interior plans 2.7 |  | 9.5 | 1.9 |  |  | 116 | 50 | 131,567 |
| start-centre 11913_9092 | baked at once (8) | 42.1 (42-48) | interior plans 2.8 | 3.3 / 2.3 | 10.8 | 1.0 | 0.0 | 28.8 | 129 | 61 | 131,567 |
| start-centre 11913_9092 | queued on the bake (8) | 44.0 (41-51) | interior plans 3.1 | 1.4 / 1.4 | 11.3 | 1.8 | 0.0 | 28.6 | 129 | 61 | 131,567 |
| lexington-downtown 12577_9303 | batched (before) | 42.6 (38-54) | streets 5.4 |  | 15.2 | 1.1 |  |  | 127 | 49 | 119,975 |
| lexington-downtown 12577_9303 | baked at once (6) | 42.0 (42-42) | streets 5.4 | 2.8 / 2.1 | 11.9 | 1.2 | 0.0 | 23.1 | 137 | 57 | 119,975 |
| lexington-downtown 12577_9303 | queued on the bake (6) | 39.2 (38-39) | streets 5.3 | 0.6 / 0.6 | 12.0 | 1.1 | 0.0 | 23.2 | 137 | 57 | 119,975 |
| louisville-downtown 11712_9478 | batched (before) | 47.2 (46-59) | interior plans 6.2 |  | 18.8 | 1.3 |  |  | 207 | 48 | 129,514 |
| louisville-downtown 11712_9478 | baked at once (6) | 50.0 (48-54) | interior plans 6.3 | 3.1 / 2.3 | 18.4 | 1.0 | 0.0 | 26.7 | 214 | 53 | 129,514 |
| louisville-downtown 11712_9478 | queued on the bake (6) | 46.1 (46-48) | interior plans 6.2 | 0.7 / 0.7 | 18.2 | 1.0 | 0.0 | 26.6 | 214 | 53 | 129,514 |

## Streamed pieces: load, cut, unload, reload

Built pieces 11712_9478 to 11713_9479 (Full detail): 36 buildings registered (36 in the cutaway, 269 hash cells).

Meshes of the Louisville pieces: 72 shell meshes and stand-ins checked for NaN or infinite vertices, normals and bounds: all sound.

| file | shows | cut away | old test |
|---|---|---|---|
| `25_louisville_behind_roofs_on.png` | Behind commercial 662565060 (1,728 m², walls 3.0 m, top 159.8 m) in streamed Louisville, roofs on (before) |  |  |
| `25_louisville_behind.png` | Behind commercial 662565060 (1,728 m², walls 3.0 m, top 159.8 m) in streamed Louisville | commercial 662565060: upper + walls E; warehouse 662565798: upper + walls NW |  |
| `24_louisville_inside_roofs_on.png` | Inside warehouse 662565452 (3,026 m², walls 3.0 m, top 15.9 m) in streamed piece 11712_9478, roofs on (before) |  |  |
| `24_louisville_inside.png` | Inside warehouse 662565452 (3,026 m², walls 3.0 m, top 15.9 m) in streamed piece 11712_9478 | warehouse 662565452: upper + walls SW |  |

Cut inside it: 1 building(s), 1 cut-wall stand-in(s) of its piece made. Its piece let go of while cut: 30 still registered, nothing of it left cut or registered, stand-ins 1 → 0; a scan on the empty spot afterwards cut 0 buildings.

Built again: 36 registered; standing in the same spot cuts upper + walls SW of it again.

Every piece let go of: 0 buildings and 0 sites registered, 0 hash cells, 0 cut, 0 bytes of heights and 0 stand-ins left.


## Log

0 errors from the cutaway or its checks, 0 warnings logged during the run.

## Stages

- setup: ok, 65 ms
- build the square batched (before): ok, 8,178 ms
- build the square: ok, 4,586 ms
- count the square: ok, 152 ms
- register the shells: ok, 308 ms
- pictures: ok, 4,202 ms
- check the meshes: ok, 89 ms
- walk in and out: ok, 1,158 ms
- scan timing: ok, 337 ms
- discard the square: ok, 221 ms
- streamed pieces: build steps: ok, 2,365 ms
- streamed pieces: load, cut, unload, reload: ok, 1,108 ms

**Exit 0** after 22.8 s.
