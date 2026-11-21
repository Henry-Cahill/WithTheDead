# CLONE-Z interiors harness

Build ky-local-f2. Unity 6000.7.0a2, Direct3D11. Places: Bardstown, Princeton, Pikeville, Middlesboro. Edit mode: nothing wakes, so the live kitchen sink is drawn plain and the ceiling fittings show their bare bulbs, as the isometric view has them. Map data Â© OpenStreetMap contributors (ODbL).

## Bardstown

KentuckyMap.Build 6,944 ms. Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 935 containers, 1083 lights, 332 trees, 4 exits. Map data Â© OpenStreetMap contributors (ODbL)

Interiors: 116 buildings furnished (713 rooms), 0 unplannable, 0 failed, 815 containers, 1062 lights, 5078 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 31,105 | 29,741 |
| loot containers | 935 | 815 (2,462 more stripped of their storage) |
| lights | 1,099 | 1,062 (38 rooms left lit) |
| vertices | 8,864,547 | |
| furnishing time | | 4,353 ms in all, slowest step 97.7 ms |
| buildings furnished | | 116 (0 failed) |


## Princeton

KentuckyMap.Build 4,117 ms. Kentucky map Princeton (build ky-local-f2): 102 buildings, 42 beyond the edge, 161 street pieces, 861 containers, 996 lights, 458 trees, 4 exits. Map data Â© OpenStreetMap contributors (ODbL)

Interiors: 102 buildings furnished (705 rooms), 0 unplannable, 0 failed, 741 containers, 977 lights, 3450 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 30,399 | 29,134 |
| loot containers | 861 | 741 (2,842 more stripped of their storage) |
| lights | 1,008 | 977 (47 rooms left lit) |
| vertices | 8,725,741 | |
| furnishing time | | 3,261 ms in all, slowest step 33.2 ms |
| buildings furnished | | 102 (0 failed) |


## Pikeville

KentuckyMap.Build 3,484 ms. Kentucky map Pikeville (build ky-local-f2): 57 buildings, 27 beyond the edge, 164 street pieces, 588 containers, 625 lights, 691 trees, 4 exits. Map data Â© OpenStreetMap contributors (ODbL)

Interiors: 54 buildings furnished (555 rooms), 1 unplannable, 0 failed, 476 containers, 596 lights, 2903 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 23,228 | 22,182 |
| loot containers | 588 | 476 (2,501 more stripped of their storage) |
| lights | 626 | 596 (32 rooms left lit) |
| vertices | 7,133,664 | |
| furnishing time | | 2,752 ms in all, slowest step 39.5 ms |
| buildings furnished | | 54 (0 failed) |


## Middlesboro

KentuckyMap.Build 1,709 ms. Kentucky map Middlesboro (build ky-local-f2): 31 buildings, 15 beyond the edge, 58 street pieces, 409 containers, 296 lights, 2408 trees, 4 exits. Map data Â© OpenStreetMap contributors (ODbL)

Interiors: 31 buildings furnished (260 rooms), 0 unplannable, 0 failed, 291 containers, 279 lights, 1263 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 11,142 | 10,218 |
| loot containers | 409 | 291 (1,050 more stripped of their storage) |
| lights | 296 | 279 (5 rooms left lit) |
| vertices | 3,769,603 | |
| furnishing time | | 1,190 ms in all, slowest step 39.5 ms |
| buildings furnished | | 31 (0 failed) |


## By use (every square)

| use | buildings | failed | rooms (mean) | containers mean / max | allotted mean | lights mean / max | lit rooms | objects mean / max | ms mean / max | slowest step ms |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Apartments | 2 | 0 | 49.0 | 24.0 / 24 | 24.0 | 46.5 / 54 | 1 | 1,863 / 2255 | 265.8 / 348.9 | 39.5 |
| AutoShop | 4 | 0 | 2.5 | 4.3 / 5 | 5.0 | 4.3 / 5 | 1 | 114 / 146 | 15.5 / 22.5 | 20.4 |
| Bank | 6 | 0 | 16.5 | 11.3 / 16 | 12.7 | 15.2 / 17 | 2 | 612 / 962 | 72.7 / 103.3 | 15.1 |
| Bar | 2 | 0 | 3.5 | 5.5 / 6 | 8.5 | 8.5 / 12 | 2 | 194 / 266 | 30.9 / 49.9 | 19.4 |
| Barn | 6 | 0 | 3.8 | 5.0 / 6 | 5.3 | 5.0 / 5 | 1 | 108 / 121 | 11.0 / 17.8 | 8.1 |
| BigStore | 5 | 0 | 7.0 | 11.8 / 14 | 13.8 | 8.2 / 9 | 3 | 349 / 436 | 45.0 / 75.9 | 31.6 |
| Church | 7 | 0 | 5.4 | 4.7 / 6 | 4.7 | 6.1 / 7 | 3 | 268 / 453 | 19.4 / 26.9 | 8.3 |
| Civic | 12 | 0 | 20.0 | 13.1 / 16 | 13.5 | 16.3 / 17 | 8 | 688 / 1228 | 80.7 / 166.2 | 19.1 |
| Clinic | 2 | 0 | 14.5 | 13.0 / 14 | 13.5 | 13.0 / 13 | 1 | 604 / 631 | 65.2 / 66.7 | 9.2 |
| Convenience | 2 | 0 | 5.5 | 8.5 / 10 | 9.5 | 6.5 / 7 | 0 | 235 / 242 | 34.8 / 48.3 | 14.5 |
| Cottage | 2 | 0 | 3.0 | 2.0 / 2 | 3.0 | 4.5 / 5 | 0 | 107 / 119 | 11.4 / 12.5 | 4.7 |
| FireStation | 2 | 0 | 5.0 | 8.5 / 10 | 9.0 | 9.0 / 10 | 0 | 254 / 302 | 36.9 / 53.8 | 17.1 |
| Funeral | 1 | 0 | 28.0 | 3.0 / 3 | 4.0 | 12.0 / 12 | 0 | 619 / 619 | 71.6 / 71.6 | 14.0 |
| Garage | 3 | 0 | 1.0 | 1.3 / 2 | 1.3 | 1.0 / 1 | 0 | 34 / 40 | 2.8 / 3.4 | 2.3 |
| Hardware | 4 | 0 | 4.0 | 7.8 / 10 | 8.8 | 5.3 / 6 | 4 | 192 / 221 | 20.2 / 32.4 | 7.3 |
| House | 103 | 0 | 6.4 | 6.3 / 9 | 6.5 | 11.4 / 14 | 41 | 250 / 343 | 32.9 / 88.0 | 33.7 |
| Library | 3 | 0 | 15.0 | 12.0 / 16 | 12.3 | 12.0 / 14 | 1 | 649 / 831 | 74.1 / 95.1 | 12.3 |
| Motel | 2 | 0 | 16.5 | 15.0 / 17 | 15.5 | 13.5 / 17 | 0 | 549 / 606 | 99.9 / 135.8 | 36.5 |
| Office | 19 | 0 | 12.6 | 8.3 / 16 | 9.3 | 12.9 / 17 | 8 | 447 / 729 | 59.0 / 101.4 | 20.2 |
| Pharmacy | 5 | 0 | 5.2 | 12.4 / 14 | 14.0 | 7.2 / 8 | 2 | 343 / 445 | 47.0 / 66.8 | 39.5 |
| Police | 3 | 0 | 12.7 | 10.0 / 12 | 10.7 | 11.3 / 14 | 0 | 441 / 619 | 55.4 / 76.4 | 11.2 |
| PostOffice | 1 | 0 | 16.0 | 15.0 / 15 | 15.0 | 12.0 / 12 | 1 | 508 / 508 | 54.2 / 54.2 | 7.5 |
| Restaurant | 11 | 0 | 4.4 | 6.9 / 9 | 8.5 | 12.4 / 15 | 3 | 503 / 848 | 53.6 / 104.7 | 49.0 |
| Salon | 10 | 0 | 4.3 | 5.7 / 8 | 8.9 | 5.1 / 7 | 8 | 168 / 246 | 19.1 / 36.4 | 18.4 |
| School | 5 | 0 | 13.2 | 9.8 / 13 | 10.8 | 13.2 / 15 | 1 | 623 / 858 | 72.4 / 105.8 | 22.4 |
| Shed | 2 | 0 | 1.0 | 1.0 / 1 | 1.0 | 1.0 / 1 | 0 | 29 / 29 | 2.3 / 2.6 | 1.8 |
| Store | 47 | 0 | 4.1 | 7.7 / 14 | 8.7 | 5.7 / 9 | 24 | 189 / 343 | 22.1 / 98.2 | 27.6 |
| Warehouse | 18 | 0 | 5.8 | 8.8 / 15 | 10.0 | 6.5 / 8 | 4 | 218 / 310 | 40.5 / 255.0 | 97.7 |
| Workshop | 14 | 0 | 5.0 | 7.7 / 14 | 8.7 | 5.9 / 7 | 3 | 184 / 231 | 22.4 / 40.7 | 17.0 |

303 buildings furnished, 2323 loot containers in them.

## Pictures

Left: from straight above in the building's own axes (its front door's wall at the bottom), roofs, upper storeys and ceilings off, plain overcast daylight without shadows. Right: the game's isometric camera (widened to fit the building), the red capsule standing in for the survivor in its biggest room, the cutaway run (its roof and the walls turned to the camera lifted), ceilings hidden as in the game; interior partitions aren't cut here (the game's IsoCutaway does that in play).

| file | place | building | footprint | rooms | containers (allotted) | lights | objects |
|---|---|---|---|---|--:|--:|--:|
| `01_house_2622504514.png` | Bardstown | House (home; house) 2622504514 | 219 mÂ², turned 15Â°, 6 corners, fill 0.86 | Bathroom, BedroomÃ—3, Dining, Hall, Kitchen, LaundryÃ—2, Living | 8 (8) | 12 | 337 |
| `02_cottage_2622506312.png` | Bardstown | Cottage (outbuilding; house) 2622506312 | 57 mÂ², turned 13Â°, 4 corners, fill 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 94 |
| `03_motel_1915619802.png` | Bardstown | Motel (hotel; commercial) 1915619802 | 823 mÂ², turned 15Â°, 9 corners, fill 0.89 | GuestÃ—12, Hall, Laundry, Lobby | 17 (18) | 17 | 491 |
| `04_store_2622504474.png` | Bardstown | Store (clothing; commercial) 2622504474 | 727 mÂ², turned 14Â°, 12 corners, fill 0.88 | BackOffice, Restroom, Sales, StockÃ—4 | 14 (15) | 6 | 343 |
| `05_bigstore_2363563962.png` | Bardstown | BigStore (grocery; commercial / shop:pastry) 2363563962 | 432 mÂ², turned 13Â°, 10 corners, fill 0.92 | BackOffice, OfficeÃ—2, RestroomÃ—2, Sales, Stock | 8 (10) | 9 | 329 |
| `06_convenience_2622504504.png` | Bardstown | Convenience (convenience; commercial) 2622504504 | 304 mÂ², turned 14Â°, 10 corners, fill 0.67 | BackOffice, Office, Restroom, Sales, StockÃ—2 | 7 (8) | 7 | 228 |
| `07_pharmacy_281586026.png` | Bardstown | Pharmacy (pharmacy; commercial / pharmacy) 281586026 | 994 mÂ², turned 15Â°, 6 corners, fill 0.71 | BackOffice, Dispensary, RestroomÃ—2, Sales, Stock | 14 (16) | 8 | 364 |
| `08_hardware_2622505338.png` | Bardstown | Hardware (commercial; commercial) 2622505338 | 336 mÂ², turned 14Â°, 8 corners, fill 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 6 | 221 |
| `09_autoshop_2363563990.png` | Bardstown | AutoShop (commercial; commercial) 2363563990 | 254 mÂ², turned 14Â°, 5 corners, fill 0.95 | Bays | 3 (3) | 3 | 58 |
| `10_salon_2363563974.png` (+ `10_salon_2363563974_night.png`) | Bardstown | Salon (salon; commercial) 2363563974 | 511 mÂ², turned 12Â°, 6 corners, fill 0.91 | BackOffice, Restroom, Sales, StockÃ—2 | 7 (12) | 5, 2 lit | 204 |
| `11_restaurant_2622504478.png` (+ `11_restaurant_2622504478_night.png`) | Bardstown | Restaurant (restaurant; commercial) 2622504478 | 379 mÂ², turned 14Â°, 10 corners, fill 0.91 | CommercialKitchen, EateryÃ—3, RestroomÃ—2 | 8 (10) | 15, 1 lit | 609 |
| `12_bar_2363563996.png` (+ `12_bar_2363563996_night.png`) | Bardstown | Bar (bar; house) 2363563996 | 209 mÂ², turned 13Â°, 5 corners, fill 0.99 | Bar, Restroom, Stock | 5 (6) | 5, 1 lit | 122 |
| `13_office_2622504446.png` | Bardstown | Office (commercial; commercial) 2622504446 | 388 mÂ², turned 14Â°, 6 corners, fill 0.88 | BreakRoom, Hall, Lobby, OfficeÃ—7, OpenOffice, Records, Stock | 10 (10) | 13 | 446 |
| `14_bank_2622504490.png` | Bardstown | Bank (bank; commercial) 2622504490 | 584 mÂ², turned 14Â°, 8 corners, fill 0.93 | BreakRoom, Hall, Lobby, OfficeÃ—8, RecordsÃ—2 | 12 (13) | 15 | 523 |
| `15_civic_2622504470.png` | Bardstown | Civic (courthouse; commercial) 2622504470 | 554 mÂ², turned 14Â°, 5 corners, fill 0.99 | BreakRoom, Hall, Lobby, Meeting, OfficeÃ—7, Records | 11 (12) | 15 | 458 |
| `16_clinic_2363564004.png` | Bardstown | Clinic (clinic; commercial) 2363564004 | 543 mÂ², turned 14Â°, 4 corners, fill 0.98 | Dispensary, ExamÃ—9, Hall, OfficeÃ—2, Waiting | 12 (12) | 13 | 576 |
| `17_house_2238555434.png` (+ `17_house_2238555434_night.png`) | Princeton | House (home; house) 2238555434 | 248 mÂ², turned 30Â°, 6 corners, fill 0.96 | Bathroom, BedroomÃ—4, Dining, Hall, Kitchen, Living | 8 (8) | 13, 1 lit | 326 |
| `18_apartments_2229833810.png` | Princeton | Apartments (apartments; apartments) 2229833810 | 1,310 mÂ², turned 27Â°, 6 corners, fill 0.72 | BathroomÃ—11, BedroomÃ—11, Hall, Laundry, LivingÃ—11, Lobby | 24 (24) | 39 | 1470 |
| `19_library_1474017444.png` | Princeton | Library (library; commercial / library) 1474017444 | 613 mÂ², turned 27Â°, 8 corners, fill 0.74 | Hall, OfficeÃ—2, Records, Restroom, StacksÃ—12, Stock | 13 (13) | 12, 1 lit | 702 |
| `20_police_2229427062.png` | Princeton | Police (police; civic / police) 2229427062 | 578 mÂ², turned 29Â°, 4 corners, fill 0.98 | Armory, Cells, Hall, Lobby, OfficeÃ—11, Records, Restroom, Squad | 12 (12) | 14 | 619 |
| `21_firestation_2229427060.png` | Princeton | FireStation (fire_station; civic / fire_station) 2229427060 | 401 mÂ², turned 27Â°, 9 corners, fill 0.96 | Bays, Bunks, DayRoom, Office | 7 (7) | 8 | 205 |
| `22_church_2229567554.png` | Princeton | Church (church; church) 2229567554 | 663 mÂ², turned 28Â°, 14 corners, fill 0.77 | MeetingÃ—3, Narthex, Nave | 4 (4) | 5 | 220 |
| `23_barn_2229549568.png` | Princeton | Barn (commercial; commercial) 2229549568 | 272 mÂ², turned 29Â°, 6 corners, fill 0.86 | Barn, StockÃ—3 | 5 (5) | 5 | 113 |
| `24_garage_2238550564.png` | Princeton | Garage (outbuilding; house) 2238550564 | 32 mÂ², turned 25Â°, 4 corners, fill 1.00 | HomeGarage | 1 (1) | 1 | 31 |
| `25_school_1729270046.png` | Pikeville | School (school; school) 1729270046 | 801 mÂ², turned 18Â°, 10 corners, fill 0.69 | BreakRoom, ClassroomÃ—5, Hall, Lobby, OfficeÃ—2 | 8 (9) | 11 | 489 |
| `26_warehouse_2638698918.png` | Pikeville | Warehouse (commercial; commercial) 2638698918 | 662 mÂ², turned 29Â°, 15 corners, fill 0.82 | Office, Restroom, Warehouse | 6 (7) | 6 | 150 |
| `27_workshop_2638698932.png` | Pikeville | Workshop (commercial; commercial) 2638698932 | 450 mÂ², turned 30Â°, 6 corners, fill 0.84 | Office, Restroom, StockÃ—2, WorkshopÃ—2 | 8 (9) | 7 | 193 |
| `28_postoffice_2589948132.png` | Middlesboro | PostOffice (post_office; commercial / post_office) 2589948132 | 744 mÂ², turned 7Â°, 6 corners, fill 0.91 | Hall, Lobby, OfficeÃ—2, SortingÃ—10, StockÃ—2 | 15 (15) | 12, 1 lit | 508 |
| `29_funeral_1807972230.png` | Middlesboro | Funeral (funeral; commercial / shop:funeral_directors) 1807972230 | 550 mÂ², turned 6Â°, 6 corners, fill 0.77 | Chapel, Hall, Narthex, OfficeÃ—22, Restroom, StockÃ—2 | 3 (4) | 12 | 619 |

## Streamed: the statewide map's lazy interiors

Piece 11913_9092 of Bardstown (Full) built in 71 ms: 0 interiors in it as built (none, as it should be), 8 buildings' plans left with the streamer; the plans took 18.6 ms over 4 steps (slowest 11.1 ms).

Furnished round a survivor at the piece's middle: 6 buildings in 106 frames (1.8 s at 60 fps), 6 standing, 0 failed; the slowest frame's furnishing took 24.5 ms (budget 3 ms, plus the step that crosses it). 6 interiors furnished (0 failed, 0 taken down), 6 standing, 8 known.

Loot memory: building 1915619802 (Motel, 18 containers): the first carried off, the second emptied; walked 2 km away (taken down) and back (furnished again): 18 containers, the carried-off one gone, the emptied one still empty, 16 of 16 others as they were. **OK**.

Determinism: the piece built again and building 1915619802 furnished again: identical (objects, names, places, meshes). **OK**.


## Log

900 errors, 0 warnings logged during the run:

- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`
- `Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead. @ UnityEngine.Renderer:get_material ()`

## Stages

- setup: ok, 36 ms
- square: Bardstown: ok, 26,291 ms
- discard: Bardstown: ok, 233 ms
- square: Princeton: ok, 6,898 ms
- discard: Princeton: ok, 356 ms
- square: Pikeville: ok, 4,576 ms
- discard: Pikeville: ok, 186 ms
- square: Middlesboro: ok, 2,335 ms
- discard: Middlesboro: ok, 91 ms
- summary by use: ok, 6 ms
- streamed pieces: ok, 989 ms

**Exit 2** after 42.0 s.
