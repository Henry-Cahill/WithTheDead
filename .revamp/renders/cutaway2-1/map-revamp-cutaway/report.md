# CLONE-Z isometric cutaway harness

Build ky-local-f2, Bardstown (fixed square, as the map harness builds it). Unity 6000.7.0a2, Direct3D11. Camera: isometric, yaw 45, pitch 38. Map data © OpenStreetMap contributors (ODbL).

KentuckyMap.Build: 2,189 ms (117 shells emitted in 33.4 ms). Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 516 containers, 21 lights, 332 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

## What the square is made of

| root | objects | renderers | vertices | building walls (renderers) | upper parts (renderers) |
|---|--:|--:|--:|--:|--:|
| World | 15,125 | 5,530 | 3,310,859 | 117 |  |
| RaidOverhead | 457 | 339 | 323,687 |  | 117 |
| RaidScenery | 119 | 118 | 84,853 |  | 0 |
| **all** | 15,701 | 5,987 | 3,719,399 | 117 | 117 |
| *every building batched (before)* | 15,498 | 5,786 | 3,719,399 | 0 | 0 |

Shells of their own cost +203 objects and +201 renderers over the same square batched (201 / 234 shell renderers: the shared batches they'd have joined mostly exist anyway).

1 site(s), 117 buildings with shells of their own registered (117 with walls, 117 with an upper part, 107 reaching 2.5 m or more above their ground storey's walls; 726 working doors in the square).

Reading their meshes (the heights of each upper part, done by the scans the first time they meet a building, at most 0.6 ms a scan): 117 buildings, 2.0 ms each on average, 7.7 ms the longest, 238,991 bytes of heights in all.

## Pictures

Each picture is the game's isometric camera at its widest zoom (12.5 m half-height) unless noted, in plain overcast daylight; the red capsule stands in for the survivor. "roofs on" = what the view showed before the cutaway; "cut" = after one scan. The heading is the camera's (the game's starts at 45° and turns a quarter at a time). The last column says what the old roof test (each building's footprint up to its highest point) would have cut instead, where that differs.

| file | shows | cut away | old test |
|---|---|---|---|
| `01_downtown_no_survivor.png` | Downtown with no survivor: every roof and upper storey shows |  |  |
| `02_house_roofs_on.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.3 m), roofs on (before) |  |  |
| `03_house_cut_045.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.3 m), heading 045° | house 2363563976: upper + walls SW |  |
| `04_house_cut_135.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.3 m), heading 135° | house 2363563976: upper + walls NW |  |
| `05_house_cut_225.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.3 m), heading 225° | house 2363563976: upper + walls NE |  |
| `06_house_cut_315.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 9.3 m), heading 315° | house 2363563976: upper + walls ES |  |
| `07_house_cut_close.png` | The same, closest zoom | house 2363563976: upper + walls SW |  |
| `08_house_cut_night.png` | The same by night (the map's lights on round the survivor) | house 2363563976: upper + walls SW |  |
| `09_house_both_cutaways.png` | The same with the game's other cutaway too (IsoCutaway: the interiors' partitions and props in the way); the ray cutaway hid 2 pieces | house 2363563976: upper + walls SW |  |
| `10_block_roofs_on.png` | Behind commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m) as the camera sees it, roofs on (before) |  |  |
| `11_block_behind_045.png` | Behind commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m), heading 045° | commercial 2622504470: upper + walls N |  |
| `12_block_behind_135.png` | Behind commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m), heading 135° | commercial 2622504470: upper + walls E |  |
| `13_block_behind_225.png` | Behind commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m), heading 225° | commercial 2622504470: upper + walls S |  |
| `14_block_behind_315.png` | Behind commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m), heading 315° | commercial 2622504470: upper + walls W |  |
| `15_block_inside.png` | Inside commercial 2622504470 (554 m², walls 3.5 m, top 19.5 m) | commercial 2622504470: upper + walls SW |  |
| `16_church_behind.png` | Behind commercial 2622504474 (727 m², walls 3.0 m, top 12.9 m) | commercial 2622504474: upper + walls N |  |
| `17_church_inside.png` | Inside commercial 2622504474 (727 m², walls 3.0 m, top 12.9 m) | commercial 2622504472: upper; commercial 2622504474: upper + walls SW |  |

## Meshes

237 shell meshes and stand-ins checked for NaN or infinite vertices, normals and bounds: all sound.

## Walking in and out

The stand-in walks at 1.4 m/s through the front door of house 2363563976 (219 m², walls 3.0 m, top 9.3 m) from 4 m out, the camera at heading 225° facing that wall, to the middle, waits 1 s and walks back out; one cutaway frame every 0.1 s, as the game runs it. Pictures `18`-`23` are taken on the way. Changes of the house's cut:

| t (s) | stand-in | house cut |
|--:|---|---|
| 0.1 | 3.9 m outside | nothing |
| 2.6 | 0.4 m outside | upper + walls N |
| 3.0 | inside | upper + walls NE |
| 15.2 | 1.0 m outside | upper |
| 15.3 | 1.1 m outside | nothing |

Entered at 3.0 s, cut at 2.6 s; left at 14.5 s, all back at 15.3 s (restore delay 0.4 s).

| file | shows | house cut | |
|---|---|---|---|
| `18_walk_a_outside.png` | 4 m outside the door, walking in (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | nothing | |
| `19_walk_b_entering.png` | Just through the door (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | upper + walls NE | |
| `20_walk_c_inside.png` | Inside, after a second (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | upper + walls NE | |
| `21_walk_d_leaving.png` | Just out of the door again (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | upper + walls NE | |
| `22_walk_e_stepped_out.png` | 0.3 s later (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | upper + walls NE | |
| `23_walk_f_restored.png` | 0.8 s later (house 2363563976 (219 m², walls 3.0 m, top 9.3 m)) | nothing | |

## Scan timing

3000 scans (one a frame) while the stand-in walks a 70 m circle round the middle of the square, the camera turning a quarter every 750 (after a first lap that read the buildings and made the cut walls):

| roofs told by | µs a scan | bytes allocated | buildings cut on average | at most | upper parts cut on average |
|---|--:|--:|--:|--:|--:|
| their heights (now) | 34.9 | 0 | 0.86 | 2 | 0.86 |
| footprint up to the top (before) | 4.0 | 0 | 0.85 | 2 | 0.85 |

Cut walls' stand-ins made so far (each the first time its building's walls are cut): 10, 3.4 ms each on average, 8.3 ms the longest. Doors hidden with their walls: 392.

## Streamed pieces: build steps

The piece holding each place's middle, built the way the streamer builds it (`ChunkSteps`, Full detail, every step timed back to back), after a warm-up build of it: with every building in the shared batches as before the cutaway, with the shells baked as `CutawayPass` emits them, and queued on the piece's bake (`Site.Bake`, packed in the piece's `outdoor` step today, off the main thread once the streamer does that). "interior plans" is the interiors' tail, which `CutawayPass` now closes with a step of its own (it used to be charged to "cutaway"). Register = switching the piece on (its buildings into the cutaway's hash); read = every building's meshes read at once (the game reads them one scan at a time).

| piece | shells | total ms | slowest step | cutaway steps: Σ / max ms | interior plans ms | outdoor ms | register ms | read ms | objects | renderers | vertices |
|---|---|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|
| start-centre 11913_9092 | batched (before) | 37.4 | utility lines 9.1 |  | 0.0 | 1.9 |  |  | 86 | 57 | 95,372 |
| start-centre 11913_9092 | baked at once (8) | 47.1 | interior plans 13.0 | 1.8 / 1.8 | 13.0 | 2.4 | 0.1 | 29.5 | 99 | 68 | 95,372 |
| start-centre 11913_9092 | queued on the bake (8) | 44.7 | interior plans 8.3 | 1.6 / 1.6 | 8.3 | 7.3 | 0.0 | 26.8 | 99 | 68 | 95,372 |
| lexington-downtown 12577_9303 | batched (before) | 49.4 | utility lines 9.4 |  | 0.0 | 2.2 |  |  | 85 | 46 | 130,550 |
| lexington-downtown 12577_9303 | baked at once (6) | 63.1 | buildings 16.6 | 2.3 / 2.2 | 9.2 | 1.6 | 0.0 | 29.8 | 95 | 54 | 130,550 |
| lexington-downtown 12577_9303 | queued on the bake (6) | 49.4 | interior plans 9.2 | 0.5 / 0.5 | 9.2 | 2.9 | 0.0 | 30.4 | 95 | 54 | 130,550 |
| louisville-downtown 11712_9478 | batched (before) | 59.9 | utility lines 16.6 |  | 0.0 | 2.8 |  |  | 91 | 48 | 193,221 |
| louisville-downtown 11712_9478 | baked at once (6) | 60.3 | interior plans 15.9 | 3.4 / 2.5 | 15.9 | 1.6 | 0.0 | 46.4 | 99 | 54 | 193,221 |
| louisville-downtown 11712_9478 | queued on the bake (6) | 94.6 | interior plans 22.5 | 0.6 / 0.6 | 22.5 | 3.8 | 0.0 | 45.2 | 99 | 54 | 193,221 |

## Streamed pieces: load, cut, unload, reload

Built pieces 11712_9478 to 11713_9479 (Full detail): 36 buildings registered (36 in the cutaway, 287 hash cells).

| file | shows | cut away | old test |
|---|---|---|---|
| `25_louisville_behind.png` | Behind warehouse 662565608 (2,854 m², walls 3.0 m, top 91.3 m) in streamed Louisville | warehouse 662565608: upper + walls N |  |
| `24_louisville_inside.png` | Inside warehouse 662565452 (3,026 m², walls 3.0 m, top 15.9 m) in streamed piece 11712_9478 | warehouse 662565452: upper + walls SW |  |

Cut inside it: 1 building(s), 1 cut-wall stand-in(s) of its piece made. Its piece let go of while cut: 30 still registered, nothing of it left cut or registered, stand-ins 2 → 1; a scan on the empty spot afterwards cut 0 buildings.

Built again: 36 registered; standing in the same spot cuts upper + walls SW of it again.

Every piece let go of: 0 buildings and 0 sites registered, 0 hash cells, 0 cut, 0 bytes of heights and 0 stand-ins left.


## Log

8 errors, 232 warnings logged during the run:

- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { 183.984650, NaN, -198.997833 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { -149.351318, NaN, -192.004913 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { -100.808449, NaN, -166.508759 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { 14.642389, NaN, -156.245056 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { 183.984650, NaN, -198.997833 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { -149.351318, NaN, -192.004913 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { -100.808449, NaN, -166.508759 }.`
- `transform.position assign attempt for 'BasinWater' is not valid. Input position is { 14.642389, NaN, -156.245056 }.`

## Stages

- setup: ok, 46 ms
- build the square batched (before): ok, 4,374 ms
- build the square: ok, 2,204 ms
- count the square: ok, 73 ms
- register the shells: ok, 246 ms
- pictures: ok, 11,337 ms
- check the meshes: ok, 80 ms
- walk in and out: ok, 1,059 ms
- scan timing: ok, 252 ms
- discard the square: ok, 113 ms
- streamed pieces: build steps: ok, 1,636 ms
- streamed pieces: load, cut, unload, reload: ok, 813 ms

**Exit 2** after 22.2 s.
