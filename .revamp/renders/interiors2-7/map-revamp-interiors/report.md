# CLONE-Z interiors harness

Build ky-local-f2. Unity 6000.7.0a2, Direct3D11. Places: Bardstown, Princeton, Pikeville, Middlesboro. Edit mode: nothing wakes, so the live kitchen sink is drawn plain and the ceiling fittings show their bare bulbs, as the isometric view has them. Map data © OpenStreetMap contributors (ODbL).

## Bardstown

KentuckyMap.Build 8,242 ms. Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 950 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 116 buildings furnished (697 rooms), 0 unplannable, 0 failed, 809 containers, 928 lights, 6049 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 29,906 | 27,705 |
| loot containers | 901 | 809 (2,185 more stripped of their storage) |
| lights | 966 | 928 (38 rooms left lit) |
| vertices | 8,067,083 | 6,364,261 |
| renderers (material slots) | | 13,997 (31,685; 1,873 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 4,935 ms in all, slowest step 144.5 ms |
| buildings furnished | | 116 (0 failed) |

<details><summary>Every building (116)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2363563990 | AutoShop | commercial | commercial | 254 | 14 | 0.95 | Bays | 3 (3) | 2 | 58 | 21.3 (19.2 interior rooms) |
| 2622504490 | Bank | bank | commercial | 584 | 14 | 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 353 | 81.9 (14.2 interior merge) |
| 2239973254 | Bank | bank | commercial / bank | 202 | 13 | 0.99 | Lobby, Office, Vault | 4 (4) | 4 | 145 | 28.2 (8.5 interior rooms) |
| 2363563980 | Bar | bar | commercial | 460 | 14 | 1.00 | Bar, CommercialKitchen, Restroom×2 | 6 (11) | 12 lit 1 | 228 | 66.2 (22.8 interior floors) |
| 2363563996 | Bar | bar | house | 209 | 13 | 0.99 | Bar, Restroom, Stock | 5 (6) | 3 lit 1 | 99 | 20.2 (9.3 interior rooms) |
| 2622506322 | Barn | commercial | commercial | 369 | 14 | 0.58 | Barn×3, Stock×2 | 6 (7) | 4 | 121 | 23.0 (10.7 interior rooms) |
| 2622506284 | Barn | commercial | commercial | 261 | 14 | 0.68 | Barn×2, Stock×2 | 5 (5) | 4 | 111 | 13.2 (3.8 interior rooms) |
| 2622506324 | Barn | commercial | commercial | 254 | 14 | 0.98 | Barn×2, Stock×2 | 5 (6) | 4 | 113 | 9.7 (2.3 interior rooms) |
| 2239973246 | BigStore | grocery | commercial | 460 | 14 | 0.96 | BackOffice, Restroom×2, Sales, Stock | 9 (11) | 5 lit 1 | 220 | 41.8 (10.8 interior rooms) |
| 2363563962 | BigStore | grocery | commercial / shop:pastry | 432 | 13 | 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 | 40.0 (7.5 interior rooms) |
| 2239973256 | Church | church | church / place_of_worship | 1,231 | 14 | 0.84 | Meeting×4, Nave, Sacristy | 5 (5) | 5 lit 1 | 443 | 58.2 (16.4 interior rooms) |
| 2239973252 | Church | church | church / place_of_worship | 362 | 14 | 0.76 | Meeting×2, Nave, Sacristy×2 | 4 (4) | 6 | 287 | 40.4 (12.1 interior rooms) |
| 2622504470 | Civic | courthouse | commercial | 554 | 14 | 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 | 68.1 (7.9 interior rooms) |
| 2363564004 | Clinic | clinic | commercial | 543 | 14 | 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 | 87.5 (9.8 interior walls) |
| 124092542 | Convenience | convenience | commercial / shop:convenience | 464 | 14 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 10 (11) | 5 | 229 | 62.2 (20.5 interior rooms) |
| 2622504504 | Convenience | convenience | commercial | 304 | 14 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 | 36.7 (10.3 interior doors) |
| 2648437042 | Cottage | home | house | 66 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 5 | 115 | 13.2 (5.1 interior rooms) |
| 2622506312 | Cottage | outbuilding | house | 57 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 | 16.9 (3.6 interior wall meshes) |
| 2622504458 | Garage | outbuilding | house | 38 | 14 | 0.99 | HomeGarage | 1 (1) | 1 | 34 | 3.2 (1.8 interior rooms) |
| 2648437046 | Garage | outbuilding | house | 33 | 15 | 0.97 | HomeGarage | 2 (2) | 1 | 43 | 3.5 (2.4 interior rooms) |
| 2622504448 | Hardware | hardware | commercial | 587 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 12 (13) | 4 lit 1 | 205 | 39.5 (13.9 interior rooms) |
| 2622505338 | Hardware | commercial | commercial | 336 | 14 | 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 206 | 25.0 (5.0 interior rooms) |
| 2622504452 | House | home | house | 246 | 14 | 0.95 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (8) | 11 | 248 | 35.0 (10.4 interior rooms) |
| 2363563982 | House | home | house | 245 | 15 | 0.99 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 324 | 71.5 (21.1 interior rooms) |
| 2622504486 | House | home | house | 243 | 14 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 283 | 35.4 (5.3 interior rooms) |
| 2622505348 | House | home | house | 237 | 14 | 0.87 | Bathroom, Bedroom×3, Dining, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 lit 2 | 311 | 50.4 (14.4 interior rooms) |
| 2622506314 | House | home | house | 237 | 13 | 0.95 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 328 | 47.7 (8.3 interior walls) |
| 2622504494 | House | home | house | 227 | 15 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 7 (8) | 11 | 227 | 33.9 (10.6 interior rooms) |
| 2648411562 | House | home | house | 222 | 13 | 0.95 | Bathroom, Bedroom×2, Closet, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 255 | 33.1 (6.5 interior rooms) |
| 2622506286 | House | home | house | 221 | 14 | 0.82 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 | 295 | 34.4 (6.9 interior rooms) |
| 2622505344 | House | home | house | 221 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 280 | 38.1 (5.9 interior rooms) |
| 2622504514 | House | home | house | 219 | 15 | 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 11 | 346 | 52.0 (6.8 interior rooms) |
| 2363563976 | House | home | house | 219 | 14 | 0.99 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 8 (8) | 11 | 260 | 67.8 (29.1 interior rooms) |
| 2648411526 | House | home | house | 218 | 13 | 0.90 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 7 (7) | 11 lit 1 | 294 | 65.5 (20.6 interior rooms) |
| 2363564000 | House | home | house | 214 | 13 | 0.84 | Bathroom, Bedroom×2, Kitchen, Living | 7 (7) | 11 | 244 | 44.5 (12.0 interior rooms) |
| 2622505346 | House | home | house | 214 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 8 (8) | 11 | 261 | 27.8 (5.5 interior rooms) |
| 2622506276 | House | home | house | 208 | 13 | 0.78 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 6 (7) | 12 | 317 | 37.7 (5.6 interior rooms) |
| 2622506288 | House | home | house | 206 | 14 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 262 | 31.2 (5.4 interior rooms) |
| 2239973242 | House | home | house | 206 | 14 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 245 | 53.6 (17.8 interior rooms) |
| 2622506282 | House | home | house | 204 | 14 | 0.68 | Bathroom, Bedroom×2, Den, Hall, Kitchen, Living | 7 (7) | 11 | 267 | 48.5 (9.4 interior doors) |
| 2622504468 | House | home | house | 203 | 12 | 0.78 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 7 (7) | 12 lit 1 | 250 | 29.7 (6.7 interior rooms) |
| 2622504456 | House | home | house | 202 | 14 | 0.68 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Laundry, Living | 7 (7) | 11 | 298 | 62.2 (21.8 interior rooms) |
| 2622504454 | House | home | house | 199 | 15 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 7 (7) | 12 lit 1 | 267 | 43.7 (9.3 interior rooms) |
| 2622504484 | House | home | house | 196 | 14 | 1.00 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Living | 6 (7) | 12 | 314 | 50.9 (8.8 interior rooms) |
| 2648437058 | House | home | house | 194 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 288 | 35.7 (7.1 interior rooms) |
| 2622506316 | House | home | house | 194 | 13 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 224 | 26.6 (8.5 interior rooms) |
| 2622504496 | House | home | house | 186 | 15 | 0.82 | Bathroom, Bedroom, Kitchen, Laundry, Living | 7 (7) | 9 | 236 | 32.1 (10.1 interior rooms) |
| 2622506266 | House | home | house | 185 | 14 | 0.78 | Bathroom, Bedroom×3, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 289 | 33.6 (7.9 interior rooms) |
| 2648437044 | House | home | house | 182 | 14 | 1.00 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 7 (7) | 12 | 235 | 28.5 (7.0 interior rooms) |
| 2622504444 | House | home | house | 174 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 284 | 42.9 (5.3 interior doors) |
| 2648411528 | House | home | house | 174 | 13 | 0.92 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Living | 7 (7) | 11 | 264 | 48.4 (10.9 interior rooms) |
| 2647449226 | House | home | house | 171 | 15 | 0.87 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 7 (7) | 12 lit 1 | 263 | 42.3 (6.9 interior rooms) |
| 2622504462 | House | home | house | 169 | 14 | 0.89 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 7 (7) | 11 | 275 | 41.0 (10.7 interior rooms) |
| 2648411530 | House | home | house | 164 | 13 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 242 | 35.2 (8.4 interior rooms) |
| 2622506326 | House | home | house | 161 | 13 | 0.78 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 6 (6) | 11 lit 1 | 292 | 38.2 (7.6 interior rooms) |
| 2622505340 | House | home | house | 158 | 14 | 0.89 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 6 (6) | 12 | 232 | 41.5 (11.7 interior rooms) |
| 2622504464 | House | home | house | 154 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 6 (6) | 11 lit 2 | 254 | 42.6 (11.1 interior rooms) |
| 2648437050 | House | home | house | 152 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 228 | 31.6 (8.2 interior rooms) |
| 2622506246 | House | home | house | 148 | 14 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 6 (6) | 11 | 260 | 31.5 (7.9 interior rooms) |
| 2622505358 | House | home | house | 141 | 13 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 224 | 26.1 (5.3 interior rooms) |
| 2648437056 | House | home | house | 137 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 229 | 28.7 (9.6 interior rooms) |
| 2648411540 | House | home | house | 131 | 14 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 230 | 25.9 (6.2 interior rooms) |
| 2622504488 | House | home | house | 127 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 243 | 29.7 (6.9 interior rooms) |
| 2622505342 | House | home | house | 126 | 14 | 0.86 | Bathroom, Bedroom×2, Kitchen, Living | 6 (6) | 11 lit 1 | 235 | 27.8 (7.8 interior rooms) |
| 2622504450 | House | home | house | 125 | 14 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 209 | 40.4 (11.7 interior rooms) |
| 2648411532 | House | home | house | 124 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 250 | 35.8 (9.2 interior rooms) |
| 2622506250 | House | home | house | 120 | 13 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 6 (6) | 11 lit 1 | 233 | 41.4 (10.1 interior merge) |
| 2622504480 | House | home | house | 96 | 14 | 0.88 | Bathroom, Bedroom, Living | 5 (5) | 5 | 133 | 14.8 (3.5 interior rooms) |
| 2648411536 | House | home | house | 96 | 14 | 0.96 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (5) | 11 | 213 | 51.0 (23.0 interior rooms) |
| 2648411534 | House | home | house | 89 | 13 | 0.98 | Bathroom, Hall, Kitchen, Living | 4 (4) | 9 lit 2 | 188 | 44.8 (12.0 interior finish) |
| 2622506320 | House | home | house | 88 | 13 | 1.00 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 186 | 20.9 (5.7 interior rooms) |
| 2622506318 | House | home | house | 85 | 12 | 0.87 | Bathroom, Bedroom, Den, Kitchen, Living | 3 (5) | 9 | 188 | 19.8 (2.8 interior rooms) |
| 2648411564 | House | home | house | 77 | 14 | 0.92 | Bathroom, Bedroom, Living | 3 (3) | 4 lit 1 | 116 | 27.8 (13.7 interior rooms) |
| 2648411538 | House | home | house | 77 | 14 | 0.98 | Bathroom, Bedroom, Living | 4 (4) | 5 | 124 | 15.8 (3.7 interior rooms) |
| 2648437054 | House | home | house | 76 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 10 | 175 | 74.4 (18.0 interior rooms) |
| 2648437040 | House | home | house | 74 | 13 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 175 | 19.7 (5.3 interior rooms) |
| 1915619802 | Motel | hotel | commercial | 823 | 15 | 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 582 | 149.4 (39.4 interior rooms) |
| 1843815690 | Motel | hotel | commercial | 515 | 13 | 0.98 | Bathroom×8, Guest×8, Hall, Lobby | 13 (13) | 10 | 525 | 254.9 (45.6 interior floors) |
| 2363563966 | Office | office | commercial | 669 | 13 | 0.93 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock×2 | 14 (14) | 11 | 650 | 141.8 (19.5 interior rooms) |
| 2622504446 | Office | commercial | commercial | 388 | 14 | 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 463 | 59.5 (8.0 interior rooms) |
| 2622504502 | Office | commercial | commercial | 380 | 15 | 0.98 | Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (9) | 6 lit 1 | 389 | 53.0 (23.6 interior rooms) |
| 2363563984 | Office | commercial | commercial | 286 | 15 | 0.99 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 7 (8) | 9 | 396 | 71.0 (12.7 interior rooms) |
| 281586026 | Pharmacy | pharmacy | commercial / pharmacy | 994 | 15 | 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 | 72.5 (20.1 interior rooms) |
| 2363563978 | Pharmacy | pharmacy | commercial | 607 | 12 | 1.00 | BackOffice, Dispensary, Restroom, Sales | 13 (14) | 5 | 245 | 36.7 (10.8 interior rooms) |
| 2363563964 | Restaurant | restaurant | commercial / restaurant | 629 | 13 | 0.99 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 10 | 389 | 87.9 (38.9 interior rooms) |
| 2622504500 | Restaurant | restaurant | commercial | 513 | 14 | 0.98 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 11 | 453 | 66.6 (33.5 interior rooms) |
| 2622504478 | Restaurant | restaurant | commercial | 379 | 14 | 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12 lit 1 | 513 | 61.4 (19.7 interior rooms) |
| 2363563998 | Restaurant | restaurant | house | 125 | 14 | 0.99 | CommercialKitchen, Eatery, Restroom | 3 (6) | 10 | 220 | 22.7 (12.8 interior rooms) |
| 2363564002 | Salon | salon | commercial | 843 | 14 | 0.97 | BackOffice, Restroom×2, Sales, Stock | 6 (11) | 4 | 185 | 22.5 (4.1 interior rooms) |
| 2363563974 | Salon | salon | commercial | 511 | 12 | 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4 lit 2 | 188 | 34.5 (7.7 interior rooms) |
| 2363563986 | Salon | salon | house | 216 | 15 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (6) | 5 | 149 | 23.6 (5.7 interior rooms) |
| 2648437048 | Shed | shed | house | 25 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 2.8 (1.7 interior rooms) |
| 2648437052 | Shed | shed | house | 18 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 4.0 (2.1 interior rooms) |
| 2622504474 | Store | clothing | commercial | 727 | 14 | 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 | 29.5 (7.4 interior rooms) |
| 2363563992 | Store | clothing | commercial | 521 | 14 | 0.98 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 lit 2 | 196 | 28.3 (8.4 interior rooms) |
| 2622504442 | Store | laundry | commercial | 457 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 213 | 22.1 (7.1 interior rooms) |
| 2363563968 | Store | shop | commercial | 392 | 13 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 191 | 50.3 (22.3 interior rooms) |
| 2363563988 | Store | shop | commercial / shop:art | 328 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 8 (9) | 5 lit 1 | 176 | 17.9 (4.6 interior rooms) |
| 2363563994 | Store | shop | commercial | 267 | 13 | 0.97 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 127 | 13.9 (5.3 interior rooms) |
| 2648411568 | Store | commercial | commercial | 253 | 13 | 0.85 | BackOffice, Restroom, Sales, Stock×2 | 6 (7) | 5 | 194 | 22.4 (4.6 interior rooms) |
| 2622504498 | Store | commercial | commercial | 251 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 176 | 19.4 (5.4 interior rooms) |
| 2239973244 | Store | grocery | house | 134 | 13 | 0.97 | BackOffice, Restroom, Sales, Stock | 5 (6) | 6 | 135 | 19.7 (3.9 interior rooms) |
| 2239973248 | Warehouse | commercial | commercial | 871 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 175 | 25.0 (8.3 interior rooms) |
| 2622504482 | Warehouse | commercial | commercial | 793 | 14 | 0.86 | Office, Restroom, Warehouse, Workshop | 10 (11) | 5 | 161 | 15.6 (3.6 interior rooms) |
| 2622504460 | Warehouse | commercial | commercial | 777 | 14 | 0.96 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 178 | 22.9 (6.1 interior rooms) |
| 124090468 | Warehouse | commercial | commercial | 742 | 15 | 0.88 | Office, Restroom, Stock×2, Warehouse×2, Workshop | 11 (12) | 5 | 208 | 370.5 (144.5 interior rooms) |
| 2622504472 | Warehouse | commercial | commercial | 308 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 4 | 125 | 12.5 (2.8 interior rooms) |
| 2622504466 | Warehouse | commercial | commercial | 266 | 13 | 0.69 | Office, Restroom, Stock×2, Warehouse, Workshop | 7 (8) | 5 lit 1 | 162 | 16.0 (4.2 interior rooms) |
| 2622504492 | Workshop | commercial | commercial | 822 | 14 | 0.46 | Office, Restroom, Stock, Workshop×4 | 14 (15) | 4 lit 1 | 211 | 33.6 (10.3 interior walls) |
| 2622504476 | Workshop | commercial | commercial | 798 | 14 | 1.00 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 lit 1 | 167 | 17.6 (3.7 interior rooms) |
| 2239973240 | Workshop | commercial | commercial | 379 | 14 | 0.84 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 lit 1 | 166 | 28.8 (7.0 interior rooms) |
| 2647445510 | Workshop | commercial | commercial | 343 | 16 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 118 | 16.1 (4.3 interior rooms) |
| 2622506278 | Workshop | commercial | commercial | 329 | 14 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 | 151 | 19.7 (5.1 interior rooms) |
| 2622506280 | Workshop | commercial | commercial | 306 | 14 | 0.78 | Office, Restroom, Stock×2, Workshop×3 | 7 (8) | 5 | 192 | 50.6 (11.6 interior rooms) |
| 2622506264 | Workshop | commercial | commercial | 295 | 14 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 122 | 12.0 (3.1 interior rooms) |
| 2648411560 | Workshop | commercial | commercial | 283 | 13 | 0.75 | Office, Restroom, Workshop×2 | 7 (8) | 5 | 148 | 15.3 (3.6 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Dresser in Motel 1843815690): 716 of its triangles hidden in its room's furniture mesh, **OK**.

## Princeton

KentuckyMap.Build 5,807 ms. Kentucky map Princeton (build ky-local-f2): 102 buildings, 42 beyond the edge, 161 street pieces, 858 containers, 803 lights, 459 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 102 buildings furnished (631 rooms), 0 unplannable, 0 failed, 718 containers, 779 lights, 4971 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 28,366 | 26,076 |
| loot containers | 858 | 718 (2,098 more stripped of their storage) |
| lights | 815 | 779 (47 rooms left lit) |
| vertices | 7,657,963 | 6,048,783 |
| renderers (material slots) | | 12,764 (29,545; 1,746 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 4,689 ms in all, slowest step 82.2 ms |
| buildings furnished | | 102 (0 failed) |

<details><summary>Every building (102)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2229833810 | Apartments | apartments | apartments | 1,310 | 27 | 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1450 | 277.7 (30.9 interior rooms) |
| 2229822680 | AutoShop | commercial | commercial | 437 | 28 | 0.96 | Bays, Restroom, Sales | 5 (6) | 4 lit 1 | 115 | 17.4 (8.3 interior rooms) |
| 2229822692 | AutoShop | auto | commercial / shop:car_repair | 126 | 29 | 1.00 | Bays, Restroom, Sales | 4 (5) | 3 | 98 | 13.3 (4.7 interior rooms) |
| 2229447462 | Bank | bank | commercial | 1,169 | 29 | 0.80 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 11 (12) | 6 lit 2 | 344 | 49.5 (6.0 interior rooms) |
| 2229447460 | Bank | bank | commercial | 948 | 29 | 0.77 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 12 (13) | 6 | 333 | 58.7 (9.1 interior merge) |
| 2229549564 | Bank | bank | commercial / bank | 368 | 29 | 0.70 | BreakRoom, Lobby, Office×4, Records, Restroom, Stock, Vault | 8 (8) | 7 | 350 | 51.1 (5.3 interior rooms) |
| 2229549568 | Barn | commercial | commercial | 272 | 29 | 0.86 | Barn, Stock×3 | 5 (5) | 4 | 110 | 17.8 (6.8 interior rooms) |
| 2230229656 | Barn | commercial | commercial | 250 | 28 | 1.00 | Barn, Stock×2 | 5 (5) | 4 lit 1 | 97 | 12.6 (3.5 interior doors) |
| 2229533466 | Barn | commercial | commercial | 175 | 30 | 0.95 | Barn, Stock×2 | 4 (4) | 3 | 82 | 11.2 (3.5 interior doors) |
| 2229566908 | Church | church | church / place_of_worship | 999 | 29 | 0.75 | Meeting×4, Narthex, Nave, Sacristy | 6 (6) | 5 | 415 | 71.1 (11.8 interior rooms) |
| 2229567554 | Church | church | church | 663 | 28 | 0.77 | Meeting×4, Nave, Sacristy | 5 (5) | 5 | 394 | 36.5 (11.4 interior rooms) |
| 2229361630 | Civic | courthouse | commercial / courthouse | 1,016 | 30 | 0.81 | BreakRoom, Hall, Lobby, Meeting×3, Office×7, Records×2, Stock×3 | 15 (16) | 12 | 708 | 118.7 (14.1 interior rooms) |
| 2229432452 | Civic | civic | commercial | 785 | 31 | 0.75 | BreakRoom, Hall, Lobby, Meeting×2, Office×6, Records×2, Stock×3 | 15 (16) | 11 | 635 | 104.5 (18.0 interior rooms) |
| 1474017440 | Civic | community | commercial | 547 | 30 | 0.99 | BreakRoom, Meeting, Office, Restroom, Stock | 4 (5) | 7 | 422 | 156.0 (82.2 interior merge) |
| 2229519182 | Civic | courthouse | commercial / courthouse | 450 | 28 | 0.96 | BreakRoom, Hall, Lobby, Meeting, Office×4, Records | 9 (10) | 7 | 346 | 49.7 (5.5 interior rooms) |
| 2229822690 | Civic | community | commercial / social_centre | 381 | 29 | 0.94 | BreakRoom, Meeting, Office×2, Restroom×2, Stock | 5 (7) | 7 | 387 | 75.4 (17.4 interior doors) |
| 2229553344 | Clinic | clinic | commercial | 705 | 29 | 0.82 | Dispensary, Exam×8, Hall, Office×3, Records, Waiting | 14 (15) | 10 lit 1 | 627 | 111.0 (15.7 interior rooms) |
| 2229427060 | FireStation | fire_station | civic / fire_station | 401 | 27 | 0.96 | Bays, Bunks, DayRoom, Office | 8 (8) | 6 | 213 | 35.8 (7.7 interior rooms) |
| 2238550564 | Garage | outbuilding | house | 32 | 25 | 1.00 | HomeGarage | 1 (1) | 1 | 34 | 3.5 (1.7 interior rooms) |
| 2229546206 | Hardware | hardware | commercial / shop:hardware | 141 | 30 | 1.00 | Restroom, Sales, Stock | 5 (6) | 3 lit 2 | 110 | 12.9 (3.5 interior rooms) |
| 2230673820 | House | home | house | 368 | 26 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 9 (9) | 12 | 254 | 61.9 (12.5 interior rooms) |
| 2229833824 | House | home | house | 293 | 28 | 0.91 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 9 (9) | 12 | 288 | 57.5 (12.4 interior walls) |
| 2238546558 | House | home | house | 261 | 28 | 0.75 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 8 (8) | 12 | 264 | 45.9 (10.3 interior rooms) |
| 2229833820 | House | home | house | 255 | 27 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 9 (9) | 12 lit 2 | 271 | 42.1 (9.7 interior rooms) |
| 2238546246 | House | home | house | 250 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 304 | 48.1 (6.6 interior rooms) |
| 2238555434 | House | home | house | 248 | 30 | 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 332 | 56.7 (8.8 interior rooms) |
| 2238546252 | House | home | house | 234 | 29 | 0.90 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 337 | 84.3 (16.1 interior merge) |
| 2238555432 | House | home | house | 233 | 31 | 0.72 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 lit 1 | 300 | 69.9 (12.2 interior rooms) |
| 2238546556 | House | home | house | 231 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 307 | 55.9 (11.8 interior rooms) |
| 2229549576 | House | home | house | 225 | 32 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (8) | 11 | 255 | 37.5 (9.3 interior rooms) |
| 2229565810 | House | home | house | 214 | 28 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 262 | 39.2 (8.5 interior rooms) |
| 2238555430 | House | home | house | 214 | 27 | 0.85 | Bathroom, Bedroom×4, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 296 | 49.4 (10.5 interior rooms) |
| 2229833814 | House | home | house | 207 | 26 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 259 | 101.8 (37.5 interior rooms) |
| 2229549570 | House | home | house | 206 | 29 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 262 | 84.0 (27.5 interior rooms) |
| 2238554394 | House | home | house | 202 | 26 | 0.89 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 236 | 44.2 (8.7 interior rooms) |
| 2229567550 | House | home | house | 193 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (7) | 11 | 218 | 45.6 (12.0 interior doors) |
| 2229833812 | House | home | house | 180 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 lit 2 | 240 | 35.2 (7.7 interior rooms) |
| 2229565802 | House | home | house | 171 | 26 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (7) | 11 | 233 | 33.0 (8.0 interior rooms) |
| 2238554392 | House | home | house | 168 | 28 | 0.79 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Laundry, Living | 5 (6) | 12 | 253 | 46.4 (6.4 interior rooms) |
| 2238546248 | House | home | house | 165 | 28 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 7 (7) | 12 | 235 | 40.7 (8.1 interior rooms) |
| 2238554396 | House | home | house | 161 | 32 | 0.73 | Bathroom, Bedroom, Hall, HomeGarage, Kitchen, Laundry, Living | 6 (6) | 11 | 250 | 41.2 (10.4 interior rooms) |
| 2238554390 | House | home | house | 158 | 29 | 0.90 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 6 (6) | 12 lit 1 | 236 | 35.2 (5.9 interior rooms) |
| 2238550556 | House | home | house | 141 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 2 | 250 | 39.0 (11.2 interior rooms) |
| 2238550566 | House | home | house | 132 | 26 | 0.82 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Laundry, Living | 6 (6) | 11 lit 1 | 265 | 42.2 (9.1 interior rooms) |
| 2229822682 | House | home | house | 131 | 27 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 6 (6) | 11 | 249 | 51.2 (13.8 interior walls) |
| 2229549574 | House | home | house | 125 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 221 | 36.2 (9.4 interior rooms) |
| 2229549566 | House | home | house | 125 | 29 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 238 | 36.8 (8.9 interior rooms) |
| 2229833816 | House | home | house | 122 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 | 220 | 36.9 (10.9 interior rooms) |
| 2229549580 | House | home | house | 114 | 32 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 216 | 45.0 (13.7 interior doors) |
| 2229833822 | House | home | house | 102 | 28 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 10 | 200 | 38.2 (9.1 interior rooms) |
| 2238550562 | House | home | house | 101 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 189 | 28.5 (8.6 interior rooms) |
| 2229833818 | House | home | house | 91 | 26 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 | 212 | 30.3 (8.3 interior rooms) |
| 2238550560 | House | home | house | 89 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 5 (5) | 11 | 223 | 73.2 (28.3 interior rooms) |
| 2238550558 | House | home | house | 69 | 28 | 0.99 | Bathroom, Bedroom, Living | 4 (4) | 8 | 147 | 52.2 (15.6 interior rooms) |
| 1474017444 | Library | library | commercial / library | 613 | 27 | 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5 lit 1 | 304 | 56.0 (14.3 interior rooms) |
| 2229546212 | Library | library | commercial / library | 288 | 29 | 0.97 | Office×2, Records, Restroom, Stacks, Stock | 7 (8) | 5 | 234 | 48.3 (24.3 interior rooms) |
| 2229553356 | Office | commercial | commercial | 488 | 29 | 0.95 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom | 10 (11) | 10 | 630 | 122.6 (41.3 interior rooms) |
| 2229822684 | Office | office | commercial | 460 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records | 10 (11) | 9 lit 1 | 473 | 154.5 (38.8 interior rooms) |
| 2229533470 | Office | office | commercial | 365 | 28 | 0.97 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records, Restroom | 8 (9) | 9 | 424 | 83.3 (11.0 interior doors) |
| 2229546196 | Office | office | commercial | 276 | 30 | 0.96 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (8) | 6 | 311 | 50.2 (9.8 interior rooms) |
| 2229565806 | Office | office | commercial | 208 | 28 | 1.00 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (7) | 6 lit 2 | 298 | 59.2 (12.6 interior rooms) |
| 2229996040 | Office | office | house | 202 | 28 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice, Records, Restroom | 6 (7) | 7 lit 1 | 251 | 38.9 (6.1 interior rooms) |
| 2229553350 | Office | office | commercial | 156 | 29 | 0.93 | Lobby, Office, Restroom | 3 (4) | 4 lit 1 | 113 | 14.1 (4.4 interior rooms) |
| 2229553352 | Office | office | commercial | 136 | 30 | 0.86 | Lobby, Office, Restroom | 2 (3) | 4 | 111 | 14.6 (4.9 interior rooms) |
| 2229546210 | Pharmacy | pharmacy | commercial | 1,195 | 29 | 1.00 | BackOffice, Dispensary, Restroom×2, Sales | 14 (16) | 6 | 307 | 41.7 (14.2 interior rooms) |
| 2230225126 | Pharmacy | pharmacy | commercial / shop:medical_supply | 549 | 27 | 0.96 | BackOffice, Dispensary, Restroom, Sales, Stock | 11 (12) | 6 lit 2 | 275 | 39.0 (10.2 interior rooms) |
| 2229427062 | Police | police | civic / police | 578 | 29 | 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 581 | 126.2 (15.5 interior walls) |
| 2229565816 | Restaurant | commercial | commercial | 490 | 30 | 0.71 | CommercialKitchen, Eatery×2, Restroom×2 | 6 (11) | 9 | 540 | 65.8 (16.7 interior rooms) |
| 2229996042 | Restaurant | commercial | commercial | 254 | 29 | 0.95 | CommercialKitchen, Eatery, Restroom×2 | 4 (8) | 8 | 283 | 33.8 (10.0 interior rooms) |
| 2229984514 | Restaurant | restaurant | house | 94 | 31 | 0.99 | CommercialKitchen, Eatery | 4 (4) | 7 | 122 | 16.4 (10.5 interior rooms) |
| 2229549578 | Salon | salon | commercial | 364 | 31 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 6 (9) | 5 lit 2 | 184 | 35.8 (14.7 interior rooms) |
| 2229546202 | Salon | salon | commercial | 336 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 lit 1 | 141 | 18.9 (5.8 interior rooms) |
| 2229959138 | Salon | salon | commercial / shop:hairdresser | 327 | 30 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (8) | 4 | 128 | 36.2 (16.8 interior rooms) |
| 2229567552 | Salon | salon | commercial / shop:hairdresser | 250 | 28 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 4 lit 1 | 146 | 18.6 (4.0 interior rooms) |
| 2229833828 | Salon | salon | commercial / shop:hairdresser | 87 | 28 | 0.99 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 95 | 13.2 (3.3 interior rooms) |
| 2229532150 | Salon | salon | commercial / shop:hairdresser | 63 | 30 | 0.97 | Restroom, Sales, Stock | 2 (3) | 3 lit 1 | 89 | 13.8 (3.5 interior rooms) |
| 2229533468 | Store | department_store | commercial / shop:variety_store | 744 | 29 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 14 (16) | 5 | 272 | 32.6 (10.7 interior rooms) |
| 2229514212 | Store | clothing | commercial / shop:clothes | 727 | 30 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 11 (13) | 4 | 219 | 26.7 (8.4 interior rooms) |
| 2230027974 | Store | shop | commercial / shop:tobacco | 611 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 11 (12) | 4 | 240 | 42.8 (14.1 interior doors) |
| 2229565814 | Store | clothing | commercial / shop:second_hand | 553 | 29 | 0.99 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 | 215 | 40.9 (11.5 interior rooms) |
| 2229551880 | Store | general_store | commercial / shop:general | 544 | 28 | 0.78 | BackOffice, Restroom, Sales, Stock×2 | 11 (12) | 5 lit 1 | 250 | 30.4 (7.8 interior rooms) |
| 2229515476 | Store | shop | commercial / shop:shoe_repair | 508 | 28 | 0.96 | BackOffice, Restroom, Sales, Stock | 10 (11) | 5 | 195 | 41.9 (13.3 interior rooms) |
| 2229833826 | Store | guns | commercial | 427 | 29 | 0.91 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 2 | 204 | 27.0 (8.6 interior rooms) |
| 2229553354 | Store | shop | commercial | 369 | 29 | 0.95 | BackOffice, Restroom, Sales, Stock | 8 (9) | 4 | 171 | 23.6 (7.5 interior rooms) |
| 2230229660 | Store | shop | commercial / shop:florist | 361 | 27 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 184 | 28.0 (8.4 interior rooms) |
| 2229565812 | Store | furniture | commercial / shop:furniture | 333 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 176 | 21.4 (6.6 interior rooms) |
| 2229996044 | Store | furniture | commercial / shop:interior_decoration | 331 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 177 | 51.4 (12.6 interior rooms) |
| 2229833830 | Store | clothing | retail | 286 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 25.8 (6.6 interior rooms) |
| 2230229658 | Store | commercial | commercial | 282 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 185 | 26.9 (7.9 interior rooms) |
| 2229822678 | Store | commercial | commercial | 272 | 31 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 170 | 19.7 (6.5 interior rooms) |
| 2229546208 | Store | shop | commercial / shop:florist | 252 | 29 | 0.93 | Restroom, Sales, Stock | 6 (7) | 4 | 121 | 15.7 (5.0 interior rooms) |
| 2229546200 | Store | furniture | commercial / shop:furniture | 236 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 4 lit 1 | 119 | 32.3 (13.8 interior rooms) |
| 2229959136 | Store | clothing | commercial / shop:clothes | 230 | 30 | 0.99 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 117 | 14.3 (5.3 interior rooms) |
| 2229822688 | Store | laundry | house | 227 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 5 | 160 | 29.4 (7.7 interior merge) |
| 2229546198 | Store | shop | commercial / shop:books | 219 | 29 | 0.97 | Restroom, Sales, Stock | 5 (6) | 4 lit 2 | 114 | 11.8 (4.1 interior rooms) |
| 2229565808 | Store | shop | house | 212 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 164 | 21.5 (5.6 interior rooms) |
| 2992366852 | Store | shop | house | 199 | 29 | 0.96 | Restroom, Sales, Stock | 4 (5) | 3 | 109 | 11.9 (3.5 interior rooms) |
| 2229959134 | Store | furniture | house | 197 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 117 | 16.6 (6.9 interior rooms) |
| 2234865948 | Store | pet_store | commercial / shop:pet_grooming | 143 | 2 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (5) | 5 lit 1 | 118 | 18.1 (4.4 interior rooms) |
| 2229546204 | Store | shop | house | 128 | 30 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 109 | 11.8 (4.3 interior rooms) |
| 2229565804 | Store | pet_store | commercial / shop:pet_grooming | 112 | 29 | 0.92 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 89 | 9.9 (2.3 interior rooms) |
| 2229984512 | Store | clothing | house | 97 | 31 | 0.98 | Sales, Stock | 3 (3) | 2 | 68 | 6.7 (2.3 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Table in Civic 1474017440): 322 of its triangles hidden in its room's furniture mesh, **OK**.

## Pikeville

KentuckyMap.Build 4,470 ms. Kentucky map Pikeville (build ky-local-f2): 57 buildings, 27 beyond the edge, 164 street pieces, 531 containers, 485 lights, 702 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 54 buildings furnished (488 rooms), 0 unplannable, 0 failed, 455 containers, 461 lights, 3805 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 22,456 | 20,856 |
| loot containers | 531 | 455 (1,764 more stripped of their storage) |
| lights | 486 | 461 (32 rooms left lit) |
| vertices | 6,280,400 | 5,074,347 |
| renderers (material slots) | | 9,597 (22,616; 1,442 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 3,604 ms in all, slowest step 66.9 ms |
| buildings furnished | | 54 (0 failed) |

<details><summary>Every building (54)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2638699002 | Apartments | apartments | apartments | 1,548 | 24 | 0.72 | Bathroom×19, Bedroom×19, BreakRoom, Hall, Laundry×3, Living×19, Lobby | 24 (24) | 55 lit 1 | 2161 | 459.5 (30.8 interior finish) |
| 2638698994 | AutoShop | commercial | commercial | 338 | 30 | 0.93 | Bays, Restroom, Sales | 5 (6) | 4 | 111 | 28.3 (15.3 interior doors) |
| 2638698982 | BigStore | commercial | commercial | 892 | 29 | 0.50 | BackOffice, Office×2, Restroom×2, Sales, Stock×2 | 14 (16) | 5 lit 1 | 313 | 45.4 (11.2 interior rooms) |
| 2230524586 | Church | church | church / place_of_worship | 841 | 36 | 0.81 | Meeting×2, Nave, Sacristy×3 | 5 (5) | 6 lit 1 | 440 | 55.0 (15.3 interior rooms) |
| 2230524588 | Church | church | church / place_of_worship | 685 | 35 | 0.92 | Meeting, Nave, Sacristy×2 | 3 (3) | 6 | 388 | 33.9 (13.4 interior rooms) |
| 1729270072 | Civic | community | commercial / exhibition_centre | 7,904 | 9 | 0.82 | BreakRoom, Meeting, Office×3, Restroom×2, Stock×2 | 14 (16) | 9 lit 2 | 592 | 182.6 (39.2 interior rooms) |
| 2230524598 | Civic | courthouse | commercial / courthouse | 3,337 | 30 | 0.84 | BreakRoom, Hall, Lobby, Meeting×5, Office×12, Records×5, Stock×6 | 16 (16) | 13 lit 1 | 1310 | 210.8 (13.0 interior rooms) |
| 2230524592 | Civic | community | commercial | 2,480 | 31 | 0.89 | BreakRoom, Meeting, Office×3, Restroom×2, Stock | 9 (11) | 8 lit 1 | 527 | 101.9 (21.2 interior merge) |
| 2230524590 | Civic | courthouse | commercial | 1,723 | 30 | 0.79 | BreakRoom, Hall, Lobby, Meeting×2, Office×7, Records×2, Stock×3 | 16 (16) | 9 lit 2 | 703 | 104.6 (12.3 interior rooms) |
| 2230524584 | Civic | courthouse | commercial | 834 | 20 | 0.68 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records×2, Restroom | 10 (11) | 10 lit 1 | 527 | 70.6 (6.5 interior rooms) |
| 1924315432 | Civic | community | commercial / theatre | 568 | 27 | 0.83 | BreakRoom, Meeting, Office×2, Restroom×2, Stock×2 | 8 (10) | 8 lit 1 | 496 | 64.9 (16.1 interior merge) |
| 2638698934 | Hardware | commercial | commercial | 590 | 30 | 0.90 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 1 | 203 | 40.7 (18.0 interior rooms) |
| 2638698974 | House | home | house | 203 | 39 | 0.90 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 7 (7) | 11 lit 1 | 271 | 61.6 (12.7 interior rooms) |
| 2638698942 | House | home | house | 144 | 28 | 0.80 | Bathroom, Bedroom, Kitchen, Living | 6 (6) | 9 lit 1 | 191 | 26.6 (6.3 interior rooms) |
| 2638698992 | House | home | house | 144 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 215 | 34.6 (8.9 interior rooms) |
| 2638698920 | House | home | house | 138 | 40 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 250 | 36.2 (9.3 interior rooms) |
| 2638699000 | House | home | house | 137 | 26 | 0.93 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 203 | 54.2 (20.4 interior rooms) |
| 2638698912 | House | home | house | 136 | 31 | 0.93 | Bathroom, Bedroom, Kitchen, Living | 6 (6) | 9 | 193 | 27.4 (7.2 interior rooms) |
| 2638698948 | House | home | house | 113 | 26 | 0.98 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 220 | 34.6 (12.2 interior rooms) |
| 2638698962 | House | home | house | 109 | 34 | 0.75 | Bathroom, Bedroom, Laundry, Living | 5 (5) | 9 | 175 | 28.4 (9.2 interior rooms) |
| 2638698946 | House | home | house | 72 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 7 | 153 | 39.2 (11.5 interior rooms) |
| 2230524594 | Office | office | commercial | 1,355 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×9, OpenOffice×4, Records×4, Stock×2 | 16 (16) | 10 | 758 | 141.9 (12.6 interior rooms) |
| 2638698986 | Office | commercial | commercial | 618 | 19 | 0.76 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records, Stock | 12 (12) | 9 lit 1 | 555 | 95.4 (18.0 interior rooms) |
| 2638698998 | Office | commercial | commercial | 555 | 29 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 10 (12) | 9 | 491 | 86.9 (27.9 interior rooms) |
| 2638698924 | Office | commercial | commercial | 412 | 38 | 0.96 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom, Stock×3 | 10 (10) | 11 lit 1 | 463 | 80.8 (18.1 interior walls) |
| 2230524604 | Office | office | commercial | 321 | 27 | 0.80 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records×2, Restroom, Stock | 6 (8) | 9 | 441 | 63.5 (4.1 interior rooms) |
| 2638698936 | Office | commercial | commercial | 317 | 26 | 0.83 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (8) | 7 | 377 | 56.5 (19.8 interior rooms) |
| 2576866036 | Police | police | commercial / police | 362 | 30 | 1.00 | Armory, Cells, Hall, Lobby, Office, Squad | 7 (8) | 5 | 255 | 50.0 (24.4 interior rooms) |
| 1696416174 | Restaurant | restaurant | commercial / restaurant | 436 | 10 | 0.57 | CommercialKitchen, Eatery×4, Restroom×2 | 6 (9) | 9 lit 1 | 428 | 56.3 (12.1 interior rooms) |
| 2638698972 | Restaurant | commercial | commercial | 364 | 39 | 0.94 | CommercialKitchen, Eatery, Restroom×2 | 2 (9) | 9 | 325 | 140.8 (66.9 interior rooms) |
| 2638698954 | Restaurant | commercial | commercial | 294 | 31 | 0.84 | CommercialKitchen, Eatery, Restroom×2 | 6 (8) | 10 | 358 | 48.6 (18.4 interior rooms) |
| 1728600610 | Restaurant | restaurant | commercial / restaurant | 212 | 20 | 0.83 | CommercialKitchen, Eatery×2, Restroom×2 | 5 (7) | 8 lit 1 | 275 | 80.9 (31.3 interior rooms) |
| 1729248694 | School | school | school | 1,775 | 40 | 0.67 | BreakRoom, Classroom×14, Hall, Lobby, Office | 13 (14) | 12 | 894 | 134.0 (16.7 interior rooms) |
| 1742875468 | School | school | school | 1,488 | 36 | 0.96 | BreakRoom, Classroom×8, Hall, Lobby, Office | 11 (12) | 10 | 609 | 89.6 (7.7 interior merge) |
| 1729270030 | School | school | school | 1,206 | 28 | 0.87 | BreakRoom, Classroom×9, Hall, Lobby, Office | 10 (11) | 10 lit 1 | 686 | 90.5 (8.0 interior doors) |
| 1729270046 | School | school | school | 801 | 18 | 0.69 | BreakRoom, Classroom×5, Hall, Lobby, Office×2 | 8 (9) | 8 | 503 | 110.0 (45.1 interior rooms) |
| 1728609676 | School | school | school | 516 | 21 | 0.95 | BreakRoom, Classroom×7, Hall, Lobby, Office×2, Restroom | 7 (8) | 10 | 511 | 69.8 (7.3 interior rooms) |
| 2638698944 | Store | commercial | commercial | 668 | 20 | 0.77 | BackOffice, Office×2, Restroom, Sales, Stock×2 | 12 (13) | 6 | 303 | 41.7 (7.9 interior rooms) |
| 2638698996 | Store | commercial | commercial | 656 | 28 | 0.84 | BackOffice, Office, Restroom, Sales, Stock | 12 (13) | 6 | 264 | 31.2 (9.4 interior rooms) |
| 2638698966 | Store | commercial | commercial | 435 | 19 | 0.84 | BackOffice, Restroom, Sales, Stock×3 | 9 (10) | 5 lit 1 | 237 | 26.9 (6.8 interior rooms) |
| 2638698958 | Store | commercial | commercial | 389 | 30 | 0.66 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 6 | 215 | 32.1 (6.7 interior rooms) |
| 2638698914 | Store | commercial | commercial | 372 | 31 | 0.86 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 161 | 20.3 (5.3 interior rooms) |
| 2638698984 | Store | commercial | commercial | 346 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 2 | 179 | 30.3 (8.4 interior rooms) |
| 2638698968 | Store | commercial | commercial | 303 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 24.1 (6.0 interior rooms) |
| 2230524602 | Store | commercial | retail | 179 | 30 | 0.96 | BackOffice, Restroom, Sales, Stock | 5 (6) | 5 lit 2 | 148 | 18.0 (4.3 interior rooms) |
| 2230524600 | Warehouse | commercial | commercial | 2,302 | 31 | 0.84 | Office, Restroom, Stock×2, Warehouse×2, Workshop×2 | 15 (16) | 5 lit 1 | 274 | 54.3 (13.3 interior doors) |
| 1919622998 | Warehouse | distillery | commercial | 1,891 | 34 | 0.76 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 9 (9) | 5 | 263 | 31.6 (7.1 interior rooms) |
| 2638698918 | Warehouse | commercial | commercial | 662 | 29 | 0.82 | Office, Restroom, Warehouse | 6 (7) | 5 | 128 | 12.8 (4.0 interior rooms) |
| 2638698956 | Warehouse | commercial | commercial | 466 | 25 | 0.86 | Office, Restroom, Stock×3, Warehouse×2 | 7 (9) | 5 | 174 | 37.8 (11.6 interior doors) |
| 2638698976 | Warehouse | commercial | commercial | 405 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 5 (7) | 5 | 138 | 22.1 (5.7 interior rooms) |
| 2638698952 | Warehouse | commercial | commercial | 387 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 5 lit 2 | 143 | 19.8 (4.0 interior rooms) |
| 2638698932 | Workshop | commercial | commercial | 450 | 30 | 0.84 | Office, Restroom, Stock×2, Workshop×2 | 8 (9) | 5 | 171 | 25.9 (4.8 interior rooms) |
| 2638698930 | Workshop | commercial | commercial | 409 | 38 | 0.90 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 149 | 20.2 (4.1 interior rooms) |
| 2638698990 | Workshop | commercial | commercial | 268 | 40 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 157 | 18.9 (3.9 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Table in School 1728609676): 320 of its triangles hidden in its room's furniture mesh, **OK**.

## Middlesboro

KentuckyMap.Build 2,009 ms. Kentucky map Middlesboro (build ky-local-f2): 31 buildings, 15 beyond the edge, 58 street pieces, 399 containers, 225 lights, 2398 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 31 buildings furnished (191 rooms), 0 unplannable, 0 failed, 285 containers, 205 lights, 1485 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 9,404 | 7,900 |
| loot containers | 399 | 285 (555 more stripped of their storage) |
| lights | 225 | 205 (5 rooms left lit) |
| vertices | 2,998,926 | 1,940,431 |
| renderers (material slots) | | 3,884 (9,035; 355 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 1,385 ms in all, slowest step 25.6 ms |
| buildings furnished | | 31 (0 failed) |

<details><summary>Every building (31)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2784318768 | Bank | bank | commercial | 2,005 | 7 | 0.88 | BreakRoom, Lobby, Office×3, Records, Restroom, Stock, Vault | 15 (16) | 9 | 409 | 75.7 (18.4 interior rooms) |
| 2784318778 | BigStore | guns | commercial | 2,059 | 7 | 0.75 | BackOffice, Office×2, Restroom×2, Sales, Stock | 14 (16) | 6 lit 1 | 376 | 53.2 (15.4 interior rooms) |
| 2784318782 | BigStore | guns | commercial | 1,804 | 7 | 0.68 | BackOffice, Restroom×2, Sales, Stock×4 | 14 (16) | 5 | 357 | 45.8 (11.7 interior rooms) |
| 2207404908 | Church | church | church / place_of_worship | 460 | 7 | 0.66 | Meeting×3, Nave, Sacristy×2 | 5 (5) | 6 lit 1 | 298 | 38.9 (8.4 interior rooms) |
| 2589948122 | FireStation | fire_station | commercial / fire_station | 835 | 7 | 0.91 | Bays, Bunks, DayRoom, Office, Restroom, Stock | 11 (12) | 7 | 300 | 44.2 (7.1 interior rooms) |
| 1807972230 | Funeral | funeral | commercial / shop:funeral_directors | 550 | 6 | 0.77 | Chapel, Office, Restroom, Stock | 4 (4) | 5 | 231 | 32.9 (6.3 interior rooms) |
| 3004391868 | House | home | house | 233 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 7 (8) | 11 | 224 | 36.1 (11.2 interior rooms) |
| 3004391878 | House | home | house | 206 | 6 | 0.88 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 6 (7) | 11 | 233 | 48.0 (21.4 interior rooms) |
| 3004391870 | House | home | house | 193 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (7) | 11 | 224 | 36.4 (10.0 interior rooms) |
| 3004391882 | House | home | house | 190 | 6 | 0.95 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 239 | 50.3 (16.2 interior rooms) |
| 3004391880 | House | home | house | 188 | 6 | 0.87 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (6) | 11 | 202 | 28.4 (8.1 interior rooms) |
| 3004391872 | House | home | house | 73 | 5 | 0.97 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 10 | 166 | 24.1 (5.9 interior rooms) |
| 2589948126 | Library | library | commercial / library | 1,001 | 6 | 0.80 | Office×3, Records, Restroom, Stacks, Stock | 11 (12) | 6 | 333 | 107.6 (25.6 interior rooms) |
| 2784318772 | Office | commercial | commercial | 442 | 7 | 0.98 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock | 9 (10) | 9 | 462 | 91.7 (14.6 interior rooms) |
| 1774615624 | Pharmacy | pharmacy | commercial | 1,533 | 7 | 0.94 | BackOffice, Dispensary, Office, Restroom×2, Sales | 14 (16) | 6 | 387 | 55.4 (20.8 interior rooms) |
| 2589948124 | Police | police | commercial / police | 489 | 6 | 1.00 | Armory, BreakRoom, Cells, Hall, Lobby, Office×3, Records, Squad | 10 (11) | 7 | 376 | 73.9 (20.4 interior rooms) |
| 2589948132 | PostOffice | post_office | commercial / post_office | 744 | 7 | 0.91 | BreakRoom, Lobby, Office, Restroom, Sorting, Stock×2 | 13 (14) | 7 lit 1 | 339 | 52.5 (13.0 interior rooms) |
| 2784318788 | Salon | salon | commercial | 690 | 7 | 0.91 | BackOffice, Office, Restroom×2, Sales, Stock | 6 (11) | 6 | 218 | 32.2 (5.1 interior rooms) |
| 3004391874 | Store | clothing | commercial | 448 | 6 | 1.00 | BackOffice, Restroom, Sales, Stock | 10 (11) | 4 | 161 | 50.8 (14.0 interior rooms) |
| 3004391876 | Store | commercial | commercial | 444 | 6 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 9 (10) | 5 lit 1 | 219 | 48.7 (11.0 interior doors) |
| 3004391864 | Store | commercial | commercial | 392 | 7 | 0.99 | Restroom, Sales, Stock | 8 (9) | 4 | 144 | 16.6 (7.5 interior rooms) |
| 3004391866 | Store | clothing | commercial | 343 | 7 | 0.98 | Restroom, Sales, Stock | 7 (8) | 4 lit 1 | 139 | 30.2 (22.2 interior rooms) |
| 2784318774 | Warehouse | warehouse | warehouse | 2,797 | 7 | 0.81 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 11 (11) | 5 | 269 | 38.7 (7.2 interior rooms) |
| 2784318780 | Warehouse | commercial | commercial | 1,983 | 7 | 0.92 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 14 (16) | 5 | 262 | 49.9 (13.4 interior rooms) |
| 2784318790 | Warehouse | commercial | commercial | 1,302 | 8 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 190 | 23.0 (6.7 interior rooms) |
| 3004391884 | Warehouse | commercial | commercial | 1,247 | 6 | 0.99 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 194 | 26.4 (7.2 interior rooms) |
| 2784318786 | Warehouse | commercial | commercial | 1,159 | 6 | 0.59 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 13 (14) | 5 | 233 | 48.1 (12.2 interior rooms) |
| 2784318792 | Warehouse | commercial | commercial | 1,146 | 7 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 4 | 182 | 24.4 (7.0 interior rooms) |
| 2784318770 | Workshop | commercial | commercial | 876 | 7 | 0.99 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 | 171 | 22.2 (4.5 interior rooms) |
| 2784318784 | Workshop | commercial | commercial | 762 | 7 | 0.95 | Office, Restroom, Stock, Workshop×3 | 10 (11) | 5 | 201 | 36.2 (5.4 interior rooms) |
| 1807972232 | Workshop | commercial | commercial | 372 | 7 | 0.75 | Office, Restroom, Workshop×2 | 8 (9) | 5 | 161 | 43.1 (19.3 interior doors) |

</details>

Dismantling a merged piece (Furniture_Table in Office 2784318772): 368 of its triangles hidden in its room's furniture mesh, **OK**.

## By use (every square)

| use | buildings | failed | rooms (mean) | containers mean / max | allotted mean | lights mean / max | lit rooms | objects mean / max | renderers mean (slots) | vertices mean (k) | ms mean / max | slowest step ms |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Apartments | 2 | 0 | 50.0 | 24.0 / 24 | 24.0 | 47.0 / 55 | 1 | 1,806 / 2161 | 1,058 (2,335) | 499.4 | 368.6 / 459.5 | 30.9 |
| AutoShop | 4 | 0 | 2.5 | 4.3 / 5 | 5.0 | 3.3 / 4 | 1 | 96 / 115 | 47 (109) | 21.6 | 20.1 / 28.3 | 19.2 |
| Bank | 6 | 0 | 7.7 | 9.8 / 15 | 10.5 | 6.7 / 9 | 2 | 322 / 409 | 155 (371) | 80.3 | 57.5 / 81.9 | 18.4 |
| Bar | 2 | 0 | 3.5 | 5.5 / 6 | 8.5 | 7.5 / 12 | 2 | 164 / 228 | 82 (159) | 37.7 | 43.2 / 66.2 | 22.8 |
| Barn | 6 | 0 | 3.8 | 5.0 / 6 | 5.3 | 3.8 / 4 | 1 | 106 / 121 | 45 (99) | 21.5 | 14.6 / 23.0 | 10.7 |
| BigStore | 5 | 0 | 7.0 | 11.8 / 14 | 13.8 | 5.2 / 6 | 3 | 309 / 376 | 155 (371) | 76.3 | 45.2 / 53.2 | 15.4 |
| Church | 7 | 0 | 5.7 | 4.7 / 6 | 4.7 | 5.6 / 6 | 3 | 381 / 443 | 79 (196) | 52.8 | 47.7 / 71.1 | 16.4 |
| Civic | 12 | 0 | 12.7 | 11.0 / 16 | 12.2 | 9.2 / 13 | 8 | 593 / 1310 | 229 (560) | 146.1 | 109.0 / 210.8 | 82.2 |
| Clinic | 2 | 0 | 14.5 | 13.0 / 14 | 13.5 | 9.5 / 10 | 1 | 585 / 627 | 286 (721) | 129.1 | 99.2 / 111.0 | 15.7 |
| Convenience | 2 | 0 | 5.5 | 8.5 / 10 | 9.5 | 5.0 / 5 | 0 | 220 / 229 | 109 (266) | 52.6 | 49.5 / 62.2 | 20.5 |
| Cottage | 2 | 0 | 3.0 | 2.0 / 2 | 3.0 | 4.5 / 5 | 0 | 102 / 115 | 53 (119) | 22.5 | 15.0 / 16.9 | 5.1 |
| FireStation | 2 | 0 | 5.0 | 9.5 / 11 | 10.0 | 6.5 / 7 | 0 | 257 / 300 | 119 (263) | 58.5 | 40.0 / 44.2 | 7.7 |
| Funeral | 1 | 0 | 4.0 | 4.0 / 4 | 4.0 | 5.0 / 5 | 0 | 231 / 231 | 78 (187) | 49.1 | 32.9 / 32.9 | 6.3 |
| Garage | 3 | 0 | 1.0 | 1.3 / 2 | 1.3 | 1.0 / 1 | 0 | 37 / 43 | 17 (37) | 5.7 | 3.4 / 3.5 | 2.4 |
| Hardware | 4 | 0 | 4.0 | 8.5 / 12 | 9.5 | 4.3 / 5 | 4 | 181 / 206 | 87 (210) | 40.4 | 29.5 / 40.7 | 18.0 |
| House | 103 | 0 | 6.4 | 6.4 / 9 | 6.5 | 10.8 / 12 | 41 | 243 / 346 | 129 (284) | 55.5 | 42.0 / 101.8 | 37.5 |
| Library | 3 | 0 | 6.7 | 9.3 / 11 | 10.3 | 5.3 / 6 | 1 | 290 / 333 | 141 (333) | 75.8 | 70.6 / 107.6 | 25.6 |
| Motel | 2 | 0 | 17.0 | 15.5 / 18 | 15.5 | 14.5 / 19 | 0 | 554 / 582 | 301 (678) | 130.7 | 202.2 / 254.9 | 45.6 |
| Office | 19 | 0 | 11.3 | 8.4 / 16 | 9.3 | 8.2 / 11 | 8 | 424 / 758 | 240 (590) | 109.1 | 77.9 / 154.5 | 41.3 |
| Pharmacy | 5 | 0 | 5.2 | 13.2 / 14 | 14.8 | 5.8 / 6 | 2 | 305 / 387 | 157 (369) | 63.6 | 49.0 / 72.5 | 20.8 |
| Police | 3 | 0 | 10.7 | 9.7 / 12 | 10.3 | 7.7 / 11 | 0 | 404 / 581 | 214 (517) | 102.3 | 83.3 / 126.2 | 24.4 |
| PostOffice | 1 | 0 | 7.0 | 13.0 / 13 | 14.0 | 7.0 / 7 | 1 | 339 / 339 | 154 (327) | 108.1 | 52.5 / 52.5 | 13.0 |
| Restaurant | 11 | 0 | 4.4 | 5.6 / 9 | 8.5 | 9.4 / 12 | 3 | 355 / 540 | 138 (250) | 73.3 | 61.9 / 140.8 | 66.9 |
| Salon | 10 | 0 | 4.3 | 4.9 / 7 | 7.6 | 4.3 / 6 | 8 | 152 / 218 | 74 (181) | 36.7 | 24.9 / 36.2 | 16.8 |
| School | 5 | 0 | 13.2 | 9.8 / 13 | 10.8 | 10.0 / 12 | 1 | 641 / 894 | 243 (621) | 158.2 | 98.8 / 134.0 | 45.1 |
| Shed | 2 | 0 | 1.0 | 1.0 / 1 | 1.0 | 1.0 / 1 | 0 | 32 / 32 | 15 (31) | 5.2 | 3.4 / 4.0 | 2.1 |
| Store | 47 | 0 | 4.1 | 7.6 / 14 | 8.6 | 4.6 / 6 | 24 | 177 / 309 | 86 (210) | 41.7 | 26.2 / 51.4 | 22.3 |
| Warehouse | 18 | 0 | 5.8 | 8.8 / 15 | 10.0 | 4.9 / 5 | 4 | 192 / 274 | 94 (224) | 50.8 | 47.3 / 370.5 | 144.5 |
| Workshop | 14 | 0 | 5.0 | 7.7 / 14 | 8.7 | 4.6 / 5 | 3 | 163 / 211 | 83 (187) | 38.3 | 25.7 / 50.6 | 19.3 |

303 buildings furnished, 2267 loot containers in them.

Pieces kept out of the rooms' merged furniture meshes, by what keeps them apart: RoomLight×1853, RaidStoreSwivelSeat×442, BasinFixture×382, CivicToggle×378, SleepSpot×235, CookingStation×199, CivicFridge×167, KitchenFridge×154, RaidHomeBath×140, CivicLook×55, RaidStoreVending×51, RaidStoreSafe×45, RaidStoreWaterCooler×32, RadioFixture×31, CivicSink×30, RaidHomeWasher×30, ….

<details><summary>Room furnishing time by role (ms)</summary>

| role | rooms | mean | median | p95 | max |
|---|--:|--:|--:|--:|--:|
| Armory | 3 | 4.5 | 3.3 | 7.6 | 7.6 |
| BackOffice | 58 | 4.9 | 4.6 | 9.4 | 13.3 |
| Bar | 2 | 7.1 | 9.3 | 9.3 | 9.3 |
| Barn | 10 | 2.6 | 2.2 | 10.7 | 10.7 |
| Bathroom | 143 | 3.9 | 2.9 | 11.7 | 27.2 |
| Bays | 6 | 8.6 | 7.6 | 19.2 | 19.2 |
| Bedroom | 222 | 3.7 | 3.2 | 5.9 | 20.4 |
| BreakRoom | 44 | 7.5 | 6.0 | 30.9 | 39.4 |
| Bunks | 2 | 6.9 | 7.7 | 7.7 | 7.7 |
| Cells | 3 | 4.4 | 4.0 | 6.1 | 6.1 |
| Chapel | 1 | 6.3 | 6.3 | 6.3 | 6.3 |
| Classroom | 43 | 5.3 | 4.0 | 15.1 | 45.1 |
| Closet | 1 | 1.2 | 1.2 | 1.2 | 1.2 |
| CommercialKitchen | 12 | 20.1 | 15.0 | 66.9 | 66.9 |
| DayRoom | 2 | 4.3 | 6.1 | 6.1 | 6.1 |
| Den | 6 | 3.7 | 3.2 | 8.8 | 8.8 |
| Dining | 31 | 3.9 | 4.1 | 6.1 | 6.7 |
| Dispensary | 7 | 8.2 | 6.4 | 20.1 | 20.1 |
| Eatery | 18 | 12.5 | 10.0 | 38.9 | 38.9 |
| Exam | 14 | 4.9 | 4.9 | 9.5 | 9.5 |
| Guest | 20 | 6.8 | 4.5 | 43.8 | 43.8 |
| Hall | 125 | 0.3 | 0.1 | 0.7 | 8.1 |
| HomeGarage | 16 | 2.6 | 2.3 | 8.1 | 8.1 |
| Kitchen | 98 | 9.8 | 8.6 | 21.4 | 37.5 |
| Laundry | 30 | 2.2 | 1.8 | 7.8 | 10.5 |
| Living | 135 | 5.5 | 4.3 | 11.0 | 29.1 |
| Lobby | 45 | 3.8 | 3.9 | 6.3 | 8.5 |
| Meeting | 42 | 5.0 | 3.5 | 14.9 | 19.2 |
| Narthex | 1 | 1.7 | 1.7 | 1.7 | 1.7 |
| Nave | 7 | 12.1 | 11.8 | 16.4 | 16.4 |
| Office | 231 | 5.6 | 4.3 | 12.5 | 144.5 |
| OpenOffice | 33 | 10.8 | 6.1 | 38.8 | 41.3 |
| Records | 54 | 3.9 | 2.8 | 13.0 | 15.7 |
| Restroom | 179 | 2.8 | 2.5 | 6.5 | 22.1 |
| Sacristy | 12 | 5.7 | 5.1 | 15.3 | 15.3 |
| Sales | 76 | 7.8 | 6.7 | 20.5 | 22.3 |
| Shed | 2 | 1.9 | 2.1 | 2.1 | 2.1 |
| Sorting | 1 | 13.0 | 13.0 | 13.0 | 13.0 |
| Squad | 3 | 18.3 | 20.4 | 24.4 | 24.4 |
| Stacks | 3 | 20.7 | 23.3 | 24.3 | 24.3 |
| Stock | 180 | 3.2 | 2.2 | 11.4 | 25.6 |
| Vault | 6 | 3.5 | 3.0 | 8.1 | 8.1 |
| Waiting | 2 | 3.6 | 4.9 | 4.9 | 4.9 |
| Warehouse | 33 | 4.2 | 2.6 | 12.2 | 26.1 |
| Workshop | 45 | 3.9 | 3.0 | 11.3 | 13.7 |

</details>

## Pictures

Left: from straight above in the building's own axes (its front door's wall at the bottom), roofs, upper storeys and ceilings off, plain overcast daylight without shadows. Right: the game's isometric camera (widened to fit the building), the red capsule standing in for the survivor in its biggest room, the cutaway run (its roof and the walls turned to the camera lifted), ceilings hidden as in the game; interior partitions aren't cut here (the game's IsoCutaway does that in play).

| file | place | building | footprint | rooms | containers (allotted) | lights | objects |
|---|---|---|---|---|--:|--:|--:|
| `01_house_2622504514.png` | Bardstown | House (home; house) 2622504514 | 219 m², turned 15°, 6 corners, fill 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 11 | 346 |
| `02_cottage_2622506312.png` | Bardstown | Cottage (outbuilding; house) 2622506312 | 57 m², turned 13°, 4 corners, fill 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 |
| `03_motel_1915619802.png` | Bardstown | Motel (hotel; commercial) 1915619802 | 823 m², turned 15°, 9 corners, fill 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 582 |
| `04_store_2622504474.png` | Bardstown | Store (clothing; commercial) 2622504474 | 727 m², turned 14°, 12 corners, fill 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 |
| `05_bigstore_2363563962.png` | Bardstown | BigStore (grocery; commercial / shop:pastry) 2363563962 | 432 m², turned 13°, 10 corners, fill 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 |
| `06_convenience_2622504504.png` | Bardstown | Convenience (convenience; commercial) 2622504504 | 304 m², turned 14°, 10 corners, fill 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 |
| `07_pharmacy_281586026.png` | Bardstown | Pharmacy (pharmacy; commercial / pharmacy) 281586026 | 994 m², turned 15°, 6 corners, fill 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 |
| `08_hardware_2622505338.png` | Bardstown | Hardware (commercial; commercial) 2622505338 | 336 m², turned 14°, 8 corners, fill 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 206 |
| `09_autoshop_2363563990.png` | Bardstown | AutoShop (commercial; commercial) 2363563990 | 254 m², turned 14°, 5 corners, fill 0.95 | Bays | 3 (3) | 2 | 58 |
| `10_salon_2363563974.png` (+ `10_salon_2363563974_night.png`) | Bardstown | Salon (salon; commercial) 2363563974 | 511 m², turned 12°, 6 corners, fill 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4, 2 lit | 188 |
| `11_restaurant_2622504478.png` (+ `11_restaurant_2622504478_night.png`) | Bardstown | Restaurant (restaurant; commercial) 2622504478 | 379 m², turned 14°, 10 corners, fill 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12, 1 lit | 513 |
| `12_bar_2363563996.png` (+ `12_bar_2363563996_night.png`) | Bardstown | Bar (bar; house) 2363563996 | 209 m², turned 13°, 5 corners, fill 0.99 | Bar, Restroom, Stock | 5 (6) | 3, 1 lit | 99 |
| `13_office_2622504446.png` | Bardstown | Office (commercial; commercial) 2622504446 | 388 m², turned 14°, 6 corners, fill 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 463 |
| `14_bank_2622504490.png` | Bardstown | Bank (bank; commercial) 2622504490 | 584 m², turned 14°, 8 corners, fill 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 353 |
| `15_civic_2622504470.png` | Bardstown | Civic (courthouse; commercial) 2622504470 | 554 m², turned 14°, 5 corners, fill 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 |
| `16_clinic_2363564004.png` | Bardstown | Clinic (clinic; commercial) 2363564004 | 543 m², turned 14°, 4 corners, fill 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 |
| `17_house_2238555434.png` (+ `17_house_2238555434_night.png`) | Princeton | House (home; house) 2238555434 | 248 m², turned 30°, 6 corners, fill 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12, 1 lit | 332 |
| `18_apartments_2229833810.png` | Princeton | Apartments (apartments; apartments) 2229833810 | 1,310 m², turned 27°, 6 corners, fill 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1450 |
| `19_library_1474017444.png` | Princeton | Library (library; commercial / library) 1474017444 | 613 m², turned 27°, 8 corners, fill 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5, 1 lit | 304 |
| `20_police_2229427062.png` | Princeton | Police (police; civic / police) 2229427062 | 578 m², turned 29°, 4 corners, fill 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 581 |
| `21_firestation_2229427060.png` | Princeton | FireStation (fire_station; civic / fire_station) 2229427060 | 401 m², turned 27°, 9 corners, fill 0.96 | Bays, Bunks, DayRoom, Office | 8 (8) | 6 | 213 |
| `22_church_2229567554.png` | Princeton | Church (church; church) 2229567554 | 663 m², turned 28°, 14 corners, fill 0.77 | Meeting×4, Nave, Sacristy | 5 (5) | 5 | 394 |
| `23_barn_2229549568.png` | Princeton | Barn (commercial; commercial) 2229549568 | 272 m², turned 29°, 6 corners, fill 0.86 | Barn, Stock×3 | 5 (5) | 4 | 110 |
| `24_garage_2238550564.png` | Princeton | Garage (outbuilding; house) 2238550564 | 32 m², turned 25°, 4 corners, fill 1.00 | HomeGarage | 1 (1) | 1 | 34 |
| `25_school_1729270046.png` | Pikeville | School (school; school) 1729270046 | 801 m², turned 18°, 10 corners, fill 0.69 | BreakRoom, Classroom×5, Hall, Lobby, Office×2 | 8 (9) | 8 | 503 |
| `26_warehouse_2638698918.png` | Pikeville | Warehouse (commercial; commercial) 2638698918 | 662 m², turned 29°, 15 corners, fill 0.82 | Office, Restroom, Warehouse | 6 (7) | 5 | 128 |
| `27_workshop_2638698932.png` | Pikeville | Workshop (commercial; commercial) 2638698932 | 450 m², turned 30°, 6 corners, fill 0.84 | Office, Restroom, Stock×2, Workshop×2 | 8 (9) | 5 | 171 |
| `28_postoffice_2589948132.png` | Middlesboro | PostOffice (post_office; commercial / post_office) 2589948132 | 744 m², turned 7°, 6 corners, fill 0.91 | BreakRoom, Lobby, Office, Restroom, Sorting, Stock×2 | 13 (14) | 7, 1 lit | 339 |
| `29_funeral_1807972230.png` | Middlesboro | Funeral (funeral; commercial / shop:funeral_directors) 1807972230 | 550 m², turned 6°, 6 corners, fill 0.77 | Chapel, Office, Restroom, Stock | 4 (4) | 5 | 231 |

<details><summary>What the pictured buildings are made of (bodies by name; lights by the body holding them)</summary>

- `01_house_2622504514.png`: pieces 84: Wall×18, DoorHead×8, RoomLight×5, Furniture_Dresser×4, Bed×3, Furniture Bedroom×3, Furniture_Chair×3, Wardrobe×3, Desk×2, Door×2, Floors×2, Fridge×2, Furniture Den×2, Furniture Dining Room×2, …; vertices 83,641: DoorHead×17312, Wall×9495, Fridge×5340, Stove×5119, Wardrobe×4824, Door×3736, Furniture×3690, Furniture Bedroom×2862, Furniture Living Room×2730, Furniture_Basin×2582, …; renderers 183 (425 material slots): Wall×41, DoorHead×24, Stove×14, Fridge×10, RoomLight×10, Bed×9, Wardrobe×9, Furniture_Nightstand×8, Bath×7, Desk×7, …; lights: RoomLight×5, Fridge×2, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1
- `02_cottage_2622506312.png`: pieces 24: Wall×4, RoomLight×3, DoorHead×2, Floors×2, Furniture Living Room×2, Bath×1, Coffee table×1, Colliders×1, Desk×1, Door×1, Furniture×1, Furniture_Armchair×1, Furniture_Bookshelf×1, Sofa×1, …; vertices 18,236: DoorHead×4328, Furniture×2538, Furniture Living Room×2425, Door×1868, Wardrobe×1690, Bath×1494, Wall×1248, Sofa×682, TV×645, Furniture_Bookshelf×556, …; renderers 41 (104 material slots): Bath×7, Wall×7, DoorHead×6, RoomLight×6, TV×3, Wardrobe×3, Door×2, Floors×2, Furniture Living Room×2, Furniture×1, …; lights: RoomLight×3, TV×1
- `03_motel_1915619802.png`: pieces 154: Wall×31, Chair×17, Furniture Room×17, DoorHead×14, Bed×12, Furniture_Dresser×12, Wardrobe×12, Furniture_Tv×9, RoomLight×5, Door×2, Floors×2, Furniture_Nightstand×2, Shelves×2, Table×2, …; vertices 136,975: DoorHead×30296, Wardrobe×19940, Furniture Room×14183, Wall×11108, Furniture_Tv×9385, Bed×8556, Furniture×7026, Stove×5119, Floors×4595, Door×3736, …; renderers 296 (667 material slots): Wall×54, DoorHead×42, Bed×36, Wardrobe×36, Furniture_Tv×27, Furniture Room×17, Stove×14, Furniture_Dresser×11, RoomLight×10, Furniture_Nightstand×8, …; lights: Furniture_Tv×9, RoomLight×5, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1, TV×1
- `04_store_2622504474.png`: pieces 101: Store shelves×32, Wall×13, Lockers×11, Shelves×11, Wardrobe×9, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Checkout counter×1, Colliders×1, …; vertices 82,161: DoorHead×12984, Lockers×12965, Wardrobe×10772, Store shelves×9248, Furniture×8541, Shelves×7652, Furniture_Basin×5164, Door×3736, Wall×3432, Floors×2696, …; renderers 151 (333 material slots): Store shelves×32, Wardrobe×21, DoorHead×18, Wall×17, Furniture_Basin×12, Lockers×11, Shelves×11, RoomLight×8, Filing cabinet×5, Door×4, …; lights: RoomLight×4
- `05_bigstore_2363563962.png`: pieces 81: Shelves×17, Wall×12, Store shelves×11, DoorHead×6, Furniture_Basin×4, RoomLight×4, Bookcase×3, Drinks cooler×3, Filing cabinet×3, Chair×2, Door×2, Floors×2, Furniture Office×2, Office chair×2, …; vertices 70,849: DoorHead×12984, Shelves×12132, Furniture_Basin×10328, Office desk×5305, Furniture×4565, Door×3736, Wall×3524, Filing cabinet×3230, Store shelves×3184, Floors×2562, …; renderers 149 (358 material slots): Furniture_Basin×24, DoorHead×18, Wall×18, Shelves×17, Office desk×16, Store shelves×11, Filing cabinet×9, RoomLight×8, Drinks cooler×6, Door×4, …; lights: RoomLight×4, Office desk×1
- `06_convenience_2622504504.png`: pieces 63: Shelves×18, Wall×10, Store shelves×6, DoorHead×5, RoomLight×4, Bookcase×2, Crate×2, Door×2, Filing cabinet×2, Floors×2, Chair×1, Checkout counter×1, Colliders×1, Drinks cooler×1, …; vertices 51,649: Shelves×12884, DoorHead×10820, Furniture×5052, Wall×3164, Office desk×2818, Furniture_Basin×2582, Filing cabinet×2496, Door×2156, Floors×1838, Store shelves×1736, …; renderers 107 (264 material slots): Shelves×18, Wall×17, DoorHead×15, Office desk×9, RoomLight×8, Filing cabinet×7, Furniture_Basin×6, Store shelves×6, Crate×4, Door×4, …; lights: RoomLight×4, Office desk×1
- `07_pharmacy_281586026.png`: pieces 89: Store shelves×30, Shelves×10, Wall×10, Medicine cabinet×7, DoorHead×5, stall×5, RoomLight×4, Checkout counter×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Chair×1, Colliders×1, …; vertices 67,068: DoorHead×10820, Store shelves×8736, Shelves×7540, Furniture×7038, Medicine cabinet×5748, Furniture_Basin×5164, Floors×3744, Door×3736, Office desk×2858, Wall×2780, …; renderers 161 (380 material slots): Medicine cabinet×35, Store shelves×30, DoorHead×15, Wall×13, Furniture_Basin×12, Shelves×10, Office desk×9, RoomLight×8, Medical fridge×5, stall×5, …; lights: RoomLight×4, Medical fridge×1, Office desk×1
- `08_hardware_2622505338.png`: pieces 61: Shelves×16, Store shelves×8, Wall×8, DoorHead×4, RoomLight×4, Bookcase×2, Door×2, Filing cabinet×2, Floors×2, Office chair×2, Office desk×2, Chair×1, Checkout counter×1, Colliders×1, …; vertices 48,408: Shelves×10760, DoorHead×8656, Office desk×4952, Furniture×3592, Furniture_Basin×2582, Floors×2528, Wall×2516, Store shelves×2324, Door×2156, Office chair×1760, …; renderers 103 (259 material slots): Office desk×16, Shelves×16, Wall×14, DoorHead×12, RoomLight×8, Store shelves×8, Furniture_Basin×6, Door×4, Filing cabinet×4, Bookcase×2, …; lights: RoomLight×4, Office desk×1
- `09_autoshop_2363563990.png`: pieces 15: Door×2, Floors×2, RoomLight×2, Car×1, Colliders×1, Filing cabinet×1, Furniture×1, Jerry can×1, Light switch plate×1, Shelves×1, Stack of tyres×1, Tool bench×1; vertices 12,253: Door×3736, Car×1768, Filing cabinet×1752, Tool bench×1528, Floors×1211, Shelves×776, Furniture×450, Stack of tyres×400, Jerry can×264, RoomLight×224, …; renderers 28 (62 material slots): Filing cabinet×5, Door×4, RoomLight×4, Tool bench×4, Car×3, Floors×2, Light switch plate×2, Furniture×1, Jerry can×1, Shelves×1, …; lights: RoomLight×2
- `10_salon_2363563974.png`: pieces 58: Shelves×17, Wall×8, DoorHead×4, Office chair×4, RoomLight×4, Waiting chair×3, salon counter×3, Door×2, Floors×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Back Office×1, …; vertices 45,422: Shelves×11684, DoorHead×8656, Furniture×4832, Furniture Salon×3288, Furniture_Basin×2582, Wall×2392, Office desk×2266, Floors×2162, Door×2156, Furniture Back Office×1248, …; renderers 87 (225 material slots): Shelves×17, Wall×13, DoorHead×12, RoomLight×8, Office desk×7, Furniture_Basin×6, Medicine cabinet×5, Door×4, salon counter×3, Filing cabinet×2, …; lights: RoomLight×4
- `11_restaurant_2622504478.png`: pieces 103: Table×17, Stool×16, Booth×14, Wall×10, stall×6, DoorHead×5, Fridge×4, Furniture_Basin×4, RoomLight×4, Stove×4, Furniture Dining Room×3, Back counter×2, Checkout counter×2, Door×2, …; vertices 102,910: Furniture Dining Room×28342, Stove×20476, DoorHead×10820, Fridge×10458, Furniture_Basin×10328, Wall×5608, Back counter×4434, Checkout counter×2160, Door×2156, Furniture×2130, …; renderers 225 (401 material slots): Stove×56, Back counter×33, Wall×26, Furniture_Basin×24, Fridge×20, Steel counter×16, DoorHead×15, RoomLight×8, Vending machine×6, stall×6, …; lights: Fridge×4, RoomLight×4, Stove×4
- `12_bar_2363563996.png`: pieces 33: Stool×8, Table×4, Wall×4, RoomLight×3, Door×2, DoorHead×2, Floors×2, stall×2, Colliders×1, Crate×1, Furniture×1, Furniture Bar×1, Light switch plate×1, Shelves×1; vertices 17,030: DoorHead×4328, Furniture Bar×4250, Furniture×2289, Door×2156, Floors×1415, Wall×1000, Shelves×632, Crate×432, RoomLight×336, Light switch plate×144, …; renderers 32 (75 material slots): DoorHead×6, RoomLight×6, Wall×5, Door×4, Crate×2, Floors×2, Light switch plate×2, stall×2, Furniture×1, Furniture Bar×1, …; lights: RoomLight×3
- `13_office_2622504446.png`: pieces 120: Wall×23, Filing cabinet×12, Chair×11, DoorHead×11, Office chair×10, Bookcase×6, Office desk×6, RoomLight×6, Furniture Office×5, Desk×4, Furniture_Chair×4, Door×2, Floors×2, Records shelf×2, …; vertices 113,295: DoorHead×23804, Office desk×14985, Desk×9583, Filing cabinet×8842, Office chair×8800, Wall×8224, Furniture×6010, Furniture Office×5352, Stove×5119, Floors×3282, …; renderers 256 (627 material slots): Office desk×46, Wall×42, DoorHead×33, Desk×28, Filing cabinet×22, Stove×14, RoomLight×12, Office chair×10, Bookcase×6, Chair×6, …; lights: RoomLight×6, Office desk×2, Furniture_Fridge×1, Stove×1
- `14_bank_2622504490.png`: pieces 91: Wall×14, Chair×12, DoorHead×7, Filing cabinet×7, Desk×5, Safe×5, Furniture_Chair×4, RoomLight×4, Bookcase×3, Furniture Office×3, Office chair×3, Office desk×3, Door×2, Floors×2, …; vertices 86,822: DoorHead×15148, Furniture Banking Hall×9446, Office desk×8404, Filing cabinet×5830, Wall×5438, Furniture_Basin×5164, Stove×5119, Furniture×4744, Door×3736, Furniture Office×3360, …; renderers 171 (411 material slots): Office desk×25, Wall×24, DoorHead×21, Filing cabinet×14, Stove×14, Furniture_Basin×12, Safe×10, RoomLight×8, Furniture_Fridge×5, Desk×4, …; lights: RoomLight×4, Office desk×2, Furniture_Fridge×1, Stove×1
- `15_civic_2622504470.png`: pieces 122: Wall×21, Chairs×20, Chair×14, Filing cabinet×11, DoorHead×10, Office chair×6, Bookcase×5, Furniture Office×5, Office desk×5, RoomLight×5, Table×3, Door×2, Floors×2, Furniture Meeting Room×2, …; vertices 107,706: DoorHead×21640, Office desk×12534, Furniture Meeting Room×12500, Filing cabinet×8000, Wall×7880, Furniture Office×5664, Floors×5311, Office chair×5280, Stove×5119, Furniture×3948, …; renderers 203 (500 material slots): Office desk×39, Wall×39, DoorHead×30, Filing cabinet×21, Stove×14, RoomLight×10, Front desk×7, Office chair×6, Bookcase×5, Furniture Office×5, …; lights: RoomLight×5, Office desk×2, Furniture_Fridge×1, Stove×1
- `16_clinic_2363564004.png`: pieces 150: Wall×26, DoorHead×12, Furniture Exam Room×12, Filing cabinet×11, Office chair×10, Waiting-room chair×10, Medicine cabinet×7, Counter×6, Desk×6, Examination couch×6, RoomLight×6, Stool×6, Shelves×5, Bookcase×4, …; vertices 118,838: DoorHead×25968, Furniture Exam Room×11654, Office desk×10473, Counter×8840, Office chair×8800, Filing cabinet×8454, Wall×8296, Furniture Office×4872, Medicine cabinet×4860, Floors×4350, …; renderers 263 (673 material slots): Wall×44, Counter×36, DoorHead×36, Office desk×32, Medicine cabinet×23, Filing cabinet×19, Furniture Exam Room×12, RoomLight×12, Office chair×10, Examination couch×6, …; lights: RoomLight×6, Office desk×2, Medical fridge×1
- `17_house_2238555434.png`: pieces 78: Wall×16, DoorHead×7, Furniture_Dresser×5, RoomLight×5, Bed×4, Furniture Bedroom×4, Furniture_Chair×4, Wardrobe×4, Door×2, Floors×2, Fridge×2, Furniture Dining Room×2, Furniture Kitchen×2, Furniture Living Room×2, …; vertices 80,531: DoorHead×15148, Wall×10327, Wardrobe×6504, Furniture Kitchen×5574, Stove×5119, Fridge×4860, Furniture×4330, Furniture Bedroom×3816, Door×3736, Bed×2852, …; renderers 172 (373 material slots): Wall×41, DoorHead×21, Stove×14, Bed×12, Wardrobe×12, Fridge×10, RoomLight×10, Bath×7, Furniture_Basin×6, Furniture_Fridge×5, …; lights: RoomLight×5, Fridge×2, Furniture_FloorLamp×1, Furniture_Fridge×1, Furniture_Nightstand×1, Stove×1, TV×1
- `18_apartments_2229833810.png`: pieces 305: Wall×62, DoorHead×33, Furniture Living Room×22, Furniture_Fridge×12, Sofa×12, Stove×12, Bath×11, Bed×11, Coffee table×11, Furniture Bedroom×11, Furniture_Armchair×11, Furniture_Basin×11, Furniture_Bookshelf×11, Furniture_Dresser×11, …; vertices 391,498: DoorHead×71412, Stove×61428, Wall×38921, Furniture×34770, Furniture_Fridge×30692, Furniture_Basin×28402, Furniture Living Room×26123, Wardrobe×17764, Bath×16818, Furniture Bedroom×10494, …; renderers 817 (1811 material slots): Stove×168, Wall×150, DoorHead×99, Bath×77, Furniture_Basin×66, Furniture_Fridge×60, Bed×33, Wardrobe×33, TV×24, Furniture Living Room×22, …; lights: Furniture_Fridge×12, Stove×12, TV×8, RoomLight×5, Furniture_Nightstand×2
- `19_library_1474017444.png`: pieces 94: Stacks×18, Filing cabinet×16, Wall×12, DoorHead×6, Shelves×6, Shelving×6, Bookcase×4, Reading table×4, RoomLight×4, Door×2, Floors×2, Furniture Office×2, Furniture_Basin×2, Office chair×2, …; vertices 80,406: DoorHead×12984, Filing cabinet×12624, Stacks×8640, Office desk×5588, Furniture×5211, Furniture_Basin×5164, Furniture Library×4820, Shelves×4288, Floors×3784, Door×3736, …; renderers 146 (339 material slots): Filing cabinet×26, DoorHead×18, Stacks×18, Wall×17, Office desk×16, Furniture_Basin×12, RoomLight×8, Shelves×6, Shelving×6, Bookcase×4, …; lights: RoomLight×4, Office desk×1
- `20_police_2229427062.png`: pieces 159: Wall×30, Filing cabinet×21, Chair×16, DoorHead×14, Office chair×9, RoomLight×7, Desk×6, Records shelf×6, Bookcase×5, Cell bars×5, Furniture Chief's Office×5, Office desk×5, Furniture_Chair×4, Ammunition crate×2, …; vertices 150,554: DoorHead×30296, Filing cabinet×17328, Desk×12905, Office desk×12596, Wall×10282, Furniture×9024, Office chair×7920, Furniture Chief's Office×5616, Furniture_Basin×5164, Stove×5119, …; renderers 314 (746 material slots): Wall×49, DoorHead×42, Desk×40, Office desk×39, Filing cabinet×38, RoomLight×14, Stove×14, Furniture_Basin×12, Office chair×9, Chair×6, …; lights: RoomLight×7, Office desk×2, Furniture_Fridge×1, Stove×1
- `21_firestation_2229427060.png`: pieces 50: Wall×6, Bunk×4, Footlocker×4, Locker×4, RoomLight×4, DoorHead×3, Table×3, Door×2, Floors×2, Furniture Bunk Room×2, Gear locker×2, Bookcase×1, Colliders×1, Filing cabinet×1, …; vertices 43,616: DoorHead×6492, Locker×5487, Furniture Day Room×4544, Door×3736, Gear locker×3411, Wall×2894, Office desk×2828, Floors×2767, Bunk×2088, Furniture Bunk Room×1752, …; renderers 91 (203 material slots): Wall×14, Locker×13, DoorHead×9, Office desk×9, Gear locker×8, RoomLight×8, Bunk×4, Door×4, Tool bench×4, TV×3, …; lights: RoomLight×4, Office desk×1, TV×1
- `22_church_2229567554.png`: pieces 82: Pew×35, Wall×10, Table×8, DoorHead×5, RoomLight×4, Pantry shelves×3, Door×2, Floors×2, Furniture Fellowship Hall×2, Bookcase×1, Chair×1, Colliders×1, Desk×1, Filing cabinet×1, …; vertices 51,880: DoorHead×10820, Furniture Fellowship Hall×9316, Furniture Sanctuary×8616, Door×3736, Floors×3524, Desk×2774, Wall×2772, Pantry shelves×1872, Wardrobe×1838, Table×1222, …; renderers 70 (173 material slots): DoorHead×15, Wall×14, Desk×9, RoomLight×8, Door×4, Pantry shelves×3, Wardrobe×3, Filing cabinet×2, Floors×2, Furniture Fellowship Hall×2, …; lights: RoomLight×4, Desk×1
- `23_barn_2229549568.png`: pieces 28: Wall×6, Shelves×5, Crate×4, RoomLight×4, DoorHead×3, Door×2, Floors×2, Colliders×1, Furniture×1; vertices 22,129: DoorHead×6348, Furniture×5535, Door×3736, Shelves×3276, Crate×1776, Wall×640, RoomLight×448, Floors×370, Colliders×0; renderers 46 (99 material slots): DoorHead×9, Wall×9, Crate×8, RoomLight×8, Shelves×5, Door×4, Floors×2, Furniture×1; lights: RoomLight×4
- `24_garage_2238550564.png`: pieces 10: Floors×2, Colliders×1, Door×1, Furniture×1, Jerry can×1, Light switch plate×1, RoomLight×1, Stack of tyres×1, Tool bench×1; vertices 4,864: Door×1868, Tool bench×1580, Furniture×426, Floors×270, Jerry can×264, Stack of tyres×200, Light switch plate×144, RoomLight×112, Colliders×0; renderers 15 (32 material slots): Tool bench×4, Door×2, Floors×2, Light switch plate×2, RoomLight×2, Furniture×1, Jerry can×1, Stack of tyres×1; lights: RoomLight×1
- `25_school_1729270046.png`: pieces 95: Wall×17, Chair×14, Furniture Classroom×10, DoorHead×8, Bookcase×7, RoomLight×5, Teacher's desk×5, Desk×4, Filing cabinet×3, Table×3, Door×2, Floors×2, Office chair×2, Office desk×2, …; vertices 117,725: Furniture×38092, DoorHead×17312, Wall×11012, Furniture Classroom×10743, Floors×7534, Office desk×6188, Teacher's desk×4196, Furniture Break Room×4050, Door×3736, Bookcase×3220, …; renderers 171 (446 material slots): Wall×50, DoorHead×24, Teacher's desk×20, Office desk×18, Furniture Classroom×10, RoomLight×10, Bookcase×7, Filing cabinet×5, Door×4, Locker×4, …; lights: RoomLight×5, Office desk×2, TV×1
- `26_warehouse_2638698918.png`: pieces 34: Racking×10, RoomLight×4, Wall×4, Door×2, DoorHead×2, Floors×2, Bookcase×1, Chair×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, Furniture_Basin×1, Office chair×1, …; vertices 31,978: Racking×7040, Furniture×4728, DoorHead×4304, Door×3736, Office desk×2848, Furniture_Basin×2582, Floors×1775, Furniture Office×1200, Wall×1052, Office chair×880, …; renderers 60 (143 material slots): Racking×10, Office desk×9, RoomLight×8, Wall×7, DoorHead×6, Furniture_Basin×6, Door×4, Filing cabinet×2, Floors×2, Bookcase×1, …; lights: RoomLight×4, Office desk×1
- `27_workshop_2638698932.png`: pieces 39: Wall×9, DoorHead×4, RoomLight×4, Tool bench×4, Shelves×3, Crate×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, …; vertices 40,601: DoorHead×8536, Tool bench×6516, Furniture_Basin×5164, Door×3736, Furniture×3647, Office desk×2838, Floors×2196, Shelves×2180, Wall×1312, Furniture Office×1224, …; renderers 88 (197 material slots): Tool bench×16, DoorHead×12, Furniture_Basin×12, Wall×12, Office desk×9, RoomLight×8, Crate×4, Door×4, Shelves×3, Filing cabinet×2, …; lights: RoomLight×4, Office desk×1
- `28_postoffice_2589948132.png`: pieces 96: Sorting case×25, Wall×12, Chair×11, Crate×6, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Lockers×2, Shelves×2, Bookcase×1, …; vertices 106,816: Sorting case×28728, Furniture×14876, DoorHead×12984, Furniture_Basin×5164, Stove×5119, Wall×4392, Floors×4252, post boxes×3864, Door×3736, Lockers×3182, …; renderers 153 (322 material slots): Sorting case×25, Wall×21, DoorHead×18, Stove×14, Furniture_Basin×12, Office desk×9, RoomLight×8, Crate×6, Furniture_Fridge×5, Door×4, …; lights: RoomLight×4, Furniture_Fridge×1, Office desk×1, Stove×1
- `29_funeral_1807972230.png`: pieces 68: Chairs×28, Wall×6, RoomLight×4, Shelves×4, DoorHead×3, Filing cabinet×3, stall×3, Desk×2, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Bookcase×1, Colliders×1, …; vertices 48,381: Furniture Chapel×10368, DoorHead×6492, Furniture_Basin×5164, Furniture×4492, Door×3736, Floors×2922, Office desk×2876, Shelves×2680, Filing cabinet×2076, Furniture Stock Room×1938, …; renderers 77 (182 material slots): Furniture_Basin×12, Wall×11, DoorHead×9, Office desk×9, RoomLight×8, Desk×4, Door×4, Filing cabinet×4, Shelves×4, stall×3, …; lights: RoomLight×4, Office desk×1

</details>

## Streamed: the statewide map's lazy interiors

Piece 11913_9092 of Bardstown (Full) built in 77 ms: 0 interiors in it as built (none, as it should be), 8 buildings' plans left with the streamer; the plans took 16.1 ms over 7 steps (slowest 3.1 ms).

Furnished round a survivor at the piece's middle: 6 buildings in 117 frames (2.0 s at 60 fps), 6 standing, 0 failed; the slowest frame's furnishing took 13.1 ms (budget 3 ms, plus the step that crosses it). 6 interiors furnished (0 failed, 0 taken down), 6 standing, 8 known.

Loot memory: building 1915619802 (Motel, 18 containers): the first carried off, the second emptied; walked 2 km away (taken down) and back (furnished again): 18 containers, the carried-off one gone, the emptied one still empty, 16 of 16 others as they were. **OK**.

Determinism: the piece built again and building 1915619802 furnished again: identical (objects, names, places, meshes). **OK**.

Plan steps in downtown Louisville's pieces (a step runs until the site's slice is spent, then finishes the plan or the wall under way):

| piece | plans ms in all | steps | slowest step ms | slowest single piece of work |
|---|--:|--:|--:|---|
| 11711_9477 | 22.6 | 6 | 7.2 | 5.6 ms: the plan of Civic 662565482 (6244 m², 43 rooms, 28 corners, 121 windows) |
| 11712_9477 | 10.5 | 3 | 4.6 | 4.1 ms: the plan of Bank 662565810 (1425 m², 8 rooms, 8 corners, 22 windows) |
| 11713_9477 | 7.3 | 2 | 5.8 | 5.8 ms: the plan of Bank 9295913 (4278 m², 9 rooms, 9 corners, 61 windows) |
| 11711_9478 | 25.1 | 4 | 11.5 | 9.9 ms: the plan of Civic 662565944 (1947 m², 25 rooms, 31 corners, 35 windows) |
| 11712_9478 | 29.7 | 8 | 10.7 | 10.4 ms: the plan of Civic 662565612 (2224 m², 29 rooms, 46 corners, 65 windows) |
| 11713_9478 | 32.0 | 11 | 7.9 | 7.7 ms: the plan of Warehouse 662566094 (4917 m², 6 rooms, 10 corners, 49 windows) |
| 11711_9479 | 9.8 | 4 | 4.7 | 4.4 ms: the plan of Motel 1291039792 (1680 m², 44 rooms, 6 corners, 38 windows) |
| 11712_9479 | 16.7 | 6 | 4.1 | 2.8 ms: the plan of Warehouse 662565608 (2854 m², 5 rooms, 4 corners, 43 windows) |
| 11713_9479 | 21.4 | 5 | 8.0 | 7.7 ms: the plan of Warehouse 662565798 (4744 m², 6 rooms, 15 corners, 52 windows) |

Slowest plan step in these pieces: 11.5 ms.


## Log

0 errors, 0 warnings logged during the run.

## Stages

- setup: ok, 35 ms
- square: Bardstown: ok, 15,797 ms
- discard: Bardstown: ok, 344 ms
- square: Princeton: ok, 9,053 ms
- discard: Princeton: ok, 258 ms
- square: Pikeville: ok, 5,694 ms
- discard: Pikeville: ok, 228 ms
- square: Middlesboro: ok, 2,667 ms
- discard: Middlesboro: ok, 88 ms
- summary by use: ok, 15 ms
- streamed pieces: ok, 2,029 ms

**Exit 0** after 36.2 s.
