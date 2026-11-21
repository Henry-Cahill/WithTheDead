# CLONE-Z isometric cutaway harness

Build ky-local-f2, Bardstown (fixed square, as the map harness builds it). Unity 6000.7.0a2, Direct3D11. Camera: isometric, yaw 45, pitch 38. Map data © OpenStreetMap contributors (ODbL).

KentuckyMap.Build: 1,318 ms. Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 10 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

## What the square is made of

| root | objects | renderers | vertices | building walls (renderers) | upper parts (renderers) |
|---|--:|--:|--:|--:|--:|
| World | 3,228 | 1,152 | 622,673 | 103 |  |
| RaidOverhead | 266 | 264 | 76,428 |  | 104 |
| RaidScenery | 72 | 71 | 32,245 |  | 0 |
| **all** | 3,566 | 1,487 | 731,346 | | |

Phase A for comparison (everything batched, nothing cut): World 3,126 objects / 1,051 renderers, RaidOverhead 221 / 220, RaidScenery 72 / 71, all 3,419 / 1,342, 731,346 vertices.

1 site(s), 104 buildings with shells of their own registered (103 with walls, 104 with an upper part, 0 whose roof reaches well past their walls).

## Pictures

Each picture is the game's isometric camera at its widest zoom (12.5 m half-height) unless noted, in plain overcast daylight; the red capsule stands in for the survivor. "roofs on" = what the view showed before the cutaway; "cut" = after one scan.

| file | shows | cut away |
|---|---|---|
| `01_downtown_no_survivor.png` | Downtown with no survivor: every roof and upper storey shows |  |
| `02_house_roofs_on.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 5.6 m), roofs on (before) |  |
| `03_house_cut.png` | Inside house 2363563976 (219 m², walls 3.0 m, top 5.6 m): its roof lifted, its walls turned to the camera cut to stubs | house 2363563976: upper + walls SW (stand-in 20 parts) |
| `04_house_cut_close.png` | The same, closest zoom | house 2363563976: upper + walls SW (stand-in 20 parts) |
| `05_house_cut_turned.png` | The same with the camera turned 90 degrees (the other walls go) | house 2363563976: upper + walls NW (stand-in 20 parts) |
| `06_house_cut_night.png` | The same by night (the map's lights on round the survivor) | house 2363563976: upper + walls SW (stand-in 20 parts) |
| `07_behind_roofs_on.png` | Behind commercial 2363564002 (843 m², walls 3.0 m, top 3.4 m) (as the camera sees it), roofs on (before) |  |
| `08_behind_cut.png` | Behind commercial 2363564002 (843 m², walls 3.0 m, top 3.4 m): what stands in the way is cut | commercial 2363564002: upper + walls E (stand-in 20 parts) |
| `09_downtown_inside_cut.png` | Inside commercial 2363564002 (843 m², walls 3.0 m, top 3.4 m) | commercial 2363564002: upper + walls SW (stand-in 20 parts) |

## Walking in and out

The stand-in walks at 1.4 m/s from 5 m outside house 2363563976 (219 m², walls 3.0 m, top 5.6 m) (toward the camera) to its middle, waits 1 s and walks back out; one cutaway frame every 0.1 s. Changes of the house's cut:

| t (s) | stand-in | house cut |
|--:|---|---|
| 0.1 | 4.3 m outside | nothing |
| 3.4 | 0.4 m outside | upper + walls S |
| 3.8 | inside | upper + walls SW |
| 17.7 | 0.8 m outside | upper + walls S |
| 17.8 | 1.0 m outside | nothing |

Entered at 3.8 s, cut at 3.4 s; left at 17.0 s, all back at 17.8 s (restore delay 0.4 s).

## Scan timing

3000 scans (one a frame) while the stand-in walks a 70 m circle round the middle of the square, the camera turning a quarter every 750: **2.0 µs** a scan (cutting and restoring included), 0 bytes allocated over the run (after a first lap that made the cut walls), at most 2 buildings cut at once.

## Streamed pieces

Built pieces 11712_9478 to 11713_9479 (Full detail): 36 buildings registered (36 in the cutaway).

| `10_louisville_inside_cut.png` | Inside warehouse 662565452 (3,026 m², walls 3.0 m, top 6.5 m) in streamed piece 11712_9478 | warehouse 662565452: upper + walls SW (stand-in 20 parts) |
Cut inside it: 1 buildings. Its piece let go of while cut: 30 still registered, nothing of it left cut or registered; a scan on the empty spot afterwards cut 0 buildings.

Every piece let go of: nothing left registered.


## Log

0 errors, 0 warnings logged during the run.

## Stages

- setup: ok, 59 ms
- build the square: ok, 1,322 ms
- count the square: ok, 5 ms
- register the shells: ok, 4 ms
- pictures: ok, 1,795 ms
- walk in and out: ok, 2 ms
- scan timing: ok, 25 ms
- discard the square: ok, 14 ms
- streamed pieces: ok, 941 ms

**Exit 0** after 4.2 s.
