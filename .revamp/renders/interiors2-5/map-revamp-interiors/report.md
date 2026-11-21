# CLONE-Z interiors harness

Build ky-local-f2. Unity 6000.7.0a2, Direct3D11. Places: Bardstown, Princeton, Pikeville, Middlesboro. Edit mode: nothing wakes, so the live kitchen sink is drawn plain and the ceiling fittings show their bare bulbs, as the isometric view has them. Map data © OpenStreetMap contributors (ODbL).

## Bardstown

KentuckyMap.Build 6,837 ms. Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 899 containers, 952 lights, 337 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 116 buildings furnished (697 rooms), 0 unplannable, 0 failed, 809 containers, 928 lights, 4752 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 29,660 | 27,461 |
| loot containers | 899 | 809 (2,185 more stripped of their storage) |
| lights | 968 | 928 (38 rooms left lit) |
| vertices | 8,832,493 | 7,127,476 |
| renderers (material slots) | | 14,674 (32,947; 952 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 3,924 ms in all, slowest step 113.9 ms |
| buildings furnished | | 116 (0 failed) |

<details><summary>Every building (116)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2363563990 | AutoShop | commercial | commercial | 254 | 14 | 0.95 | Bays | 3 (3) | 2 | 58 | 21.1 (18.5 interior rooms) |
| 2622504490 | Bank | bank | commercial | 584 | 14 | 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 352 | 46.7 (7.2 interior rooms) |
| 2239973254 | Bank | bank | commercial / bank | 202 | 13 | 0.99 | Lobby, Office, Vault | 4 (4) | 4 | 145 | 21.4 (5.9 interior rooms) |
| 2363563980 | Bar | bar | commercial | 460 | 14 | 1.00 | Bar, CommercialKitchen, Restroom×2 | 6 (11) | 12 lit 1 | 228 | 54.6 (29.2 interior floors) |
| 2363563996 | Bar | bar | house | 209 | 13 | 0.99 | Bar, Restroom, Stock | 5 (6) | 3 lit 1 | 99 | 8.0 (2.6 interior rooms) |
| 2622506322 | Barn | commercial | commercial | 369 | 14 | 0.58 | Barn×3, Stock×2 | 6 (7) | 4 | 121 | 12.3 (3.1 interior rooms) |
| 2622506284 | Barn | commercial | commercial | 261 | 14 | 0.68 | Barn×2, Stock×2 | 5 (5) | 4 | 111 | 18.8 (5.4 interior rooms) |
| 2622506324 | Barn | commercial | commercial | 254 | 14 | 0.98 | Barn×2, Stock×2 | 5 (6) | 4 | 113 | 10.9 (2.8 interior rooms) |
| 2239973246 | BigStore | grocery | commercial | 460 | 14 | 0.96 | BackOffice, Restroom×2, Sales, Stock | 9 (11) | 5 lit 1 | 220 | 27.6 (6.8 interior rooms) |
| 2363563962 | BigStore | grocery | commercial / shop:pastry | 432 | 13 | 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 | 35.5 (12.8 interior rooms) |
| 2239973256 | Church | church | church / place_of_worship | 1,231 | 14 | 0.84 | Meeting×4, Nave, Sacristy | 5 (5) | 5 lit 1 | 443 | 32.9 (10.8 interior rooms) |
| 2239973252 | Church | church | church / place_of_worship | 362 | 14 | 0.76 | Meeting×2, Nave, Sacristy×2 | 4 (4) | 6 | 287 | 30.5 (9.0 interior rooms) |
| 2622504470 | Civic | courthouse | commercial | 554 | 14 | 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 | 60.5 (11.6 interior rooms) |
| 2363564004 | Clinic | clinic | commercial | 543 | 14 | 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 | 77.4 (16.6 interior rooms) |
| 124092542 | Convenience | convenience | commercial / shop:convenience | 464 | 14 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 10 (11) | 5 | 229 | 48.6 (13.9 interior rooms) |
| 2622504504 | Convenience | convenience | commercial | 304 | 14 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 | 21.4 (5.0 interior rooms) |
| 2648437042 | Cottage | home | house | 66 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 5 | 114 | 12.2 (5.0 interior rooms) |
| 2622506312 | Cottage | outbuilding | house | 57 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 | 10.9 (3.7 interior rooms) |
| 2622504458 | Garage | outbuilding | house | 38 | 14 | 0.99 | HomeGarage | 1 (1) | 1 | 34 | 2.8 (1.6 interior rooms) |
| 2648437046 | Garage | outbuilding | house | 33 | 15 | 0.97 | HomeGarage | 2 (2) | 1 | 43 | 3.7 (2.5 interior rooms) |
| 2622504448 | Hardware | hardware | commercial | 587 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 12 (13) | 4 lit 1 | 205 | 20.3 (8.7 interior rooms) |
| 2622505338 | Hardware | commercial | commercial | 336 | 14 | 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 206 | 20.3 (4.1 interior rooms) |
| 2622504452 | House | home | house | 246 | 14 | 0.95 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (8) | 11 | 244 | 54.1 (21.3 interior rooms) |
| 2363563982 | House | home | house | 245 | 15 | 0.99 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 317 | 44.7 (14.5 interior rooms) |
| 2622504486 | House | home | house | 243 | 14 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 278 | 38.3 (9.6 interior rooms) |
| 2622505348 | House | home | house | 237 | 14 | 0.87 | Bathroom, Bedroom×3, Dining, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 lit 2 | 305 | 47.8 (8.8 interior rooms) |
| 2622506314 | House | home | house | 237 | 13 | 0.95 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 321 | 35.0 (4.7 interior rooms) |
| 2622504494 | House | home | house | 227 | 15 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 7 (8) | 11 | 224 | 51.3 (24.2 interior rooms) |
| 2648411562 | House | home | house | 222 | 13 | 0.95 | Bathroom, Bedroom×2, Closet, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 252 | 26.5 (5.9 interior rooms) |
| 2622506286 | House | home | house | 221 | 14 | 0.82 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 | 291 | 75.2 (22.0 interior rooms) |
| 2622505344 | House | home | house | 221 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 275 | 30.1 (4.7 interior rooms) |
| 2622504514 | House | home | house | 219 | 15 | 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 11 | 339 | 39.1 (6.4 interior rooms) |
| 2363563976 | House | home | house | 219 | 14 | 0.99 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 8 (8) | 11 | 256 | 82.0 (30.1 interior rooms) |
| 2648411526 | House | home | house | 218 | 13 | 0.90 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 7 (7) | 11 lit 1 | 290 | 43.1 (10.5 interior doors) |
| 2363564000 | House | home | house | 214 | 13 | 0.84 | Bathroom, Bedroom×2, Kitchen, Living | 7 (7) | 11 | 240 | 30.9 (8.1 interior rooms) |
| 2622505346 | House | home | house | 214 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 8 (8) | 11 | 256 | 29.4 (7.3 interior rooms) |
| 2622506276 | House | home | house | 208 | 13 | 0.78 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 6 (7) | 12 | 311 | 33.2 (4.9 interior rooms) |
| 2622506288 | House | home | house | 206 | 14 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 258 | 37.4 (7.0 interior rooms) |
| 2239973242 | House | home | house | 206 | 14 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 242 | 37.0 (13.4 interior rooms) |
| 2622506282 | House | home | house | 204 | 14 | 0.68 | Bathroom, Bedroom×2, Den, Hall, Kitchen, Living | 7 (7) | 11 | 262 | 29.7 (7.4 interior rooms) |
| 2622504468 | House | home | house | 203 | 12 | 0.78 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 7 (7) | 12 lit 1 | 246 | 27.3 (6.8 interior rooms) |
| 2622504456 | House | home | house | 202 | 14 | 0.68 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Laundry, Living | 7 (7) | 11 | 294 | 32.3 (7.4 interior rooms) |
| 2622504454 | House | home | house | 199 | 15 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 7 (7) | 12 lit 1 | 261 | 38.5 (10.1 interior doors) |
| 2622504484 | House | home | house | 196 | 14 | 1.00 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Living | 6 (7) | 12 | 306 | 34.3 (5.7 interior rooms) |
| 2648437058 | House | home | house | 194 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 282 | 72.7 (16.2 interior rooms) |
| 2622506316 | House | home | house | 194 | 13 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 221 | 25.3 (8.6 interior rooms) |
| 2622504496 | House | home | house | 186 | 15 | 0.82 | Bathroom, Bedroom, Kitchen, Laundry, Living | 7 (7) | 9 | 233 | 36.0 (7.9 interior rooms) |
| 2622506266 | House | home | house | 185 | 14 | 0.78 | Bathroom, Bedroom×3, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 284 | 42.2 (11.5 interior rooms) |
| 2648437044 | House | home | house | 182 | 14 | 1.00 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 7 (7) | 12 | 232 | 29.6 (7.6 interior rooms) |
| 2622504444 | House | home | house | 174 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 279 | 29.9 (3.6 interior rooms) |
| 2648411528 | House | home | house | 174 | 13 | 0.92 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Living | 7 (7) | 11 | 260 | 27.9 (7.7 interior rooms) |
| 2647449226 | House | home | house | 171 | 15 | 0.87 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 7 (7) | 12 lit 1 | 257 | 28.7 (7.0 interior rooms) |
| 2622504462 | House | home | house | 169 | 14 | 0.89 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 7 (7) | 11 | 269 | 38.2 (8.0 interior rooms) |
| 2648411530 | House | home | house | 164 | 13 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 238 | 25.9 (7.3 interior rooms) |
| 2622506326 | House | home | house | 161 | 13 | 0.78 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 6 (6) | 11 lit 1 | 286 | 30.8 (7.8 interior rooms) |
| 2622505340 | House | home | house | 158 | 14 | 0.89 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 6 (6) | 12 | 229 | 35.6 (12.4 interior rooms) |
| 2622504464 | House | home | house | 154 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 6 (6) | 11 lit 2 | 250 | 26.1 (5.4 interior rooms) |
| 2648437050 | House | home | house | 152 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 225 | 24.3 (5.6 interior rooms) |
| 2622506246 | House | home | house | 148 | 14 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 6 (6) | 11 | 256 | 27.8 (7.9 interior rooms) |
| 2622505358 | House | home | house | 141 | 13 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 221 | 23.4 (5.3 interior rooms) |
| 2648437056 | House | home | house | 137 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 226 | 25.4 (8.4 interior rooms) |
| 2648411540 | House | home | house | 131 | 14 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 227 | 22.7 (5.2 interior rooms) |
| 2622504488 | House | home | house | 127 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 240 | 24.3 (6.7 interior rooms) |
| 2622505342 | House | home | house | 126 | 14 | 0.86 | Bathroom, Bedroom×2, Kitchen, Living | 6 (6) | 11 lit 1 | 231 | 25.7 (6.9 interior rooms) |
| 2622504450 | House | home | house | 125 | 14 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 | 207 | 22.1 (6.2 interior rooms) |
| 2648411532 | House | home | house | 124 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 246 | 25.7 (7.6 interior rooms) |
| 2622506250 | House | home | house | 120 | 13 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 6 (6) | 11 lit 1 | 230 | 26.9 (9.3 interior rooms) |
| 2622504480 | House | home | house | 96 | 14 | 0.88 | Bathroom, Bedroom, Living | 5 (5) | 5 | 132 | 16.4 (5.0 interior rooms) |
| 2648411536 | House | home | house | 96 | 14 | 0.96 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (5) | 11 | 209 | 55.6 (21.3 interior rooms) |
| 2648411534 | House | home | house | 89 | 13 | 0.98 | Bathroom, Hall, Kitchen, Living | 4 (4) | 9 lit 2 | 186 | 25.5 (8.2 interior rooms) |
| 2622506320 | House | home | house | 88 | 13 | 1.00 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 184 | 19.0 (5.3 interior rooms) |
| 2622506318 | House | home | house | 85 | 12 | 0.87 | Bathroom, Bedroom, Den, Kitchen, Living | 3 (5) | 9 | 186 | 31.3 (8.9 interior rooms) |
| 2648411564 | House | home | house | 77 | 14 | 0.92 | Bathroom, Bedroom, Living | 3 (3) | 4 lit 1 | 115 | 12.7 (3.1 interior rooms) |
| 2648411538 | House | home | house | 77 | 14 | 0.98 | Bathroom, Bedroom, Living | 4 (4) | 5 | 122 | 11.0 (2.7 interior rooms) |
| 2648437054 | House | home | house | 76 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 10 | 173 | 17.7 (4.8 interior rooms) |
| 2648437040 | House | home | house | 74 | 13 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 174 | 18.0 (5.0 interior rooms) |
| 1915619802 | Motel | hotel | commercial | 823 | 15 | 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 566 | 112.0 (29.0 interior rooms) |
| 1843815690 | Motel | hotel | commercial | 515 | 13 | 0.98 | Bathroom×8, Guest×8, Hall, Lobby | 13 (13) | 10 | 517 | 158.3 (44.9 interior floors) |
| 2363563966 | Office | office | commercial | 669 | 13 | 0.93 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock×2 | 14 (14) | 11 | 649 | 95.1 (14.9 interior rooms) |
| 2622504446 | Office | commercial | commercial | 388 | 14 | 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 462 | 60.0 (7.5 interior rooms) |
| 2622504502 | Office | commercial | commercial | 380 | 15 | 0.98 | Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (9) | 6 lit 1 | 389 | 51.6 (28.0 interior rooms) |
| 2363563984 | Office | commercial | commercial | 286 | 15 | 0.99 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 7 (8) | 9 | 395 | 53.4 (8.9 interior rooms) |
| 281586026 | Pharmacy | pharmacy | commercial / pharmacy | 994 | 15 | 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 | 53.8 (13.2 interior rooms) |
| 2363563978 | Pharmacy | pharmacy | commercial | 607 | 12 | 1.00 | BackOffice, Dispensary, Restroom, Sales | 13 (14) | 5 | 245 | 39.6 (9.8 interior rooms) |
| 2363563964 | Restaurant | restaurant | commercial / restaurant | 629 | 13 | 0.99 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 10 | 389 | 47.2 (22.9 interior rooms) |
| 2622504500 | Restaurant | restaurant | commercial | 513 | 14 | 0.98 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 11 | 453 | 41.2 (14.7 interior rooms) |
| 2622504478 | Restaurant | restaurant | commercial | 379 | 14 | 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12 lit 1 | 513 | 62.1 (15.9 interior rooms) |
| 2363563998 | Restaurant | restaurant | house | 125 | 14 | 0.99 | CommercialKitchen, Eatery, Restroom | 3 (6) | 10 | 220 | 22.6 (13.4 interior rooms) |
| 2363564002 | Salon | salon | commercial | 843 | 14 | 0.97 | BackOffice, Restroom×2, Sales, Stock | 6 (11) | 4 | 185 | 17.6 (3.1 interior rooms) |
| 2363563974 | Salon | salon | commercial | 511 | 12 | 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4 lit 2 | 188 | 19.6 (4.3 interior rooms) |
| 2363563986 | Salon | salon | house | 216 | 15 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (6) | 5 | 149 | 14.4 (3.2 interior rooms) |
| 2648437048 | Shed | shed | house | 25 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 2.9 (1.8 interior rooms) |
| 2648437052 | Shed | shed | house | 18 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 2.6 (1.5 interior rooms) |
| 2622504474 | Store | clothing | commercial | 727 | 14 | 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 | 27.6 (6.8 interior rooms) |
| 2363563992 | Store | clothing | commercial | 521 | 14 | 0.98 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 lit 2 | 196 | 20.5 (5.9 interior rooms) |
| 2622504442 | Store | laundry | commercial | 457 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 213 | 19.4 (5.5 interior rooms) |
| 2363563968 | Store | shop | commercial | 392 | 13 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 191 | 18.1 (5.2 interior rooms) |
| 2363563988 | Store | shop | commercial / shop:art | 328 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 8 (9) | 5 lit 1 | 176 | 18.1 (4.7 interior rooms) |
| 2363563994 | Store | shop | commercial | 267 | 13 | 0.97 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 127 | 21.2 (7.8 interior rooms) |
| 2648411568 | Store | commercial | commercial | 253 | 13 | 0.85 | BackOffice, Restroom, Sales, Stock×2 | 6 (7) | 5 | 194 | 30.6 (9.7 interior rooms) |
| 2622504498 | Store | commercial | commercial | 251 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 176 | 16.5 (4.5 interior rooms) |
| 2239973244 | Store | grocery | house | 134 | 13 | 0.97 | BackOffice, Restroom, Sales, Stock | 5 (6) | 6 | 135 | 13.6 (3.2 interior rooms) |
| 2239973248 | Warehouse | commercial | commercial | 871 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 175 | 24.0 (7.1 interior rooms) |
| 2622504482 | Warehouse | commercial | commercial | 793 | 14 | 0.86 | Office, Restroom, Warehouse, Workshop | 10 (11) | 5 | 161 | 14.5 (3.2 interior rooms) |
| 2622504460 | Warehouse | commercial | commercial | 777 | 14 | 0.96 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 178 | 16.3 (4.5 interior rooms) |
| 124090468 | Warehouse | commercial | commercial | 742 | 15 | 0.88 | Office, Restroom, Stock×2, Warehouse×2, Workshop | 11 (12) | 5 | 208 | 269.4 (113.9 interior rooms) |
| 2622504472 | Warehouse | commercial | commercial | 308 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 4 | 125 | 12.3 (2.9 interior rooms) |
| 2622504466 | Warehouse | commercial | commercial | 266 | 13 | 0.69 | Office, Restroom, Stock×2, Warehouse, Workshop | 7 (8) | 5 lit 1 | 162 | 15.2 (3.5 interior rooms) |
| 2622504492 | Workshop | commercial | commercial | 822 | 14 | 0.46 | Office, Restroom, Stock, Workshop×4 | 14 (15) | 4 lit 1 | 211 | 23.6 (3.2 interior rooms) |
| 2622504476 | Workshop | commercial | commercial | 798 | 14 | 1.00 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 lit 1 | 167 | 17.2 (3.1 interior rooms) |
| 2239973240 | Workshop | commercial | commercial | 379 | 14 | 0.84 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 lit 1 | 166 | 20.5 (4.5 interior rooms) |
| 2647445510 | Workshop | commercial | commercial | 343 | 16 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 118 | 10.7 (3.2 interior rooms) |
| 2622506278 | Workshop | commercial | commercial | 329 | 14 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 | 151 | 16.0 (3.1 interior rooms) |
| 2622506280 | Workshop | commercial | commercial | 306 | 14 | 0.78 | Office, Restroom, Stock×2, Workshop×3 | 7 (8) | 5 | 192 | 19.2 (3.2 interior rooms) |
| 2622506264 | Workshop | commercial | commercial | 295 | 14 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 122 | 11.9 (3.1 interior rooms) |
| 2648411560 | Workshop | commercial | commercial | 283 | 13 | 0.75 | Office, Restroom, Workshop×2 | 7 (8) | 5 | 148 | 14.0 (3.3 interior rooms) |

</details>

## Princeton

KentuckyMap.Build 3,810 ms. Kentucky map Princeton (build ky-local-f2): 102 buildings, 42 beyond the edge, 161 street pieces, 856 containers, 803 lights, 459 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 102 buildings furnished (631 rooms), 0 unplannable, 0 failed, 718 containers, 779 lights, 3173 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 28,201 | 25,914 |
| loot containers | 856 | 718 (2,098 more stripped of their storage) |
| lights | 815 | 779 (47 rooms left lit) |
| vertices | 8,433,852 | 6,828,706 |
| renderers (material slots) | | 13,244 (30,433; 1,104 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 2,970 ms in all, slowest step 25.0 ms |
| buildings furnished | | 102 (0 failed) |

<details><summary>Every building (102)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2229833810 | Apartments | apartments | apartments | 1,310 | 27 | 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1437 | 172.4 (13.9 interior rooms) |
| 2229822680 | AutoShop | commercial | commercial | 437 | 28 | 0.96 | Bays, Restroom, Sales | 5 (6) | 4 lit 1 | 115 | 8.5 (2.6 interior rooms) |
| 2229822692 | AutoShop | auto | commercial / shop:car_repair | 126 | 29 | 1.00 | Bays, Restroom, Sales | 4 (5) | 3 | 98 | 13.6 (8.1 interior rooms) |
| 2229447462 | Bank | bank | commercial | 1,169 | 29 | 0.80 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 11 (12) | 6 lit 2 | 343 | 37.5 (5.3 interior rooms) |
| 2229447460 | Bank | bank | commercial | 948 | 29 | 0.77 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 12 (13) | 6 | 333 | 41.0 (7.2 interior rooms) |
| 2229549564 | Bank | bank | commercial / bank | 368 | 29 | 0.70 | BreakRoom, Lobby, Office×4, Records, Restroom, Stock, Vault | 8 (8) | 7 | 350 | 39.0 (4.2 interior rooms) |
| 2229549568 | Barn | commercial | commercial | 272 | 29 | 0.86 | Barn, Stock×3 | 5 (5) | 4 | 110 | 8.5 (1.8 interior doors) |
| 2230229656 | Barn | commercial | commercial | 250 | 28 | 1.00 | Barn, Stock×2 | 5 (5) | 4 lit 1 | 97 | 9.1 (3.5 interior doors) |
| 2229533466 | Barn | commercial | commercial | 175 | 30 | 0.95 | Barn, Stock×2 | 4 (4) | 3 | 82 | 6.7 (2.0 interior doors) |
| 2229566908 | Church | church | church / place_of_worship | 999 | 29 | 0.75 | Meeting×4, Narthex, Nave, Sacristy | 6 (6) | 5 | 415 | 25.9 (7.7 interior rooms) |
| 2229567554 | Church | church | church | 663 | 28 | 0.77 | Meeting×4, Nave, Sacristy | 5 (5) | 5 | 394 | 24.8 (8.1 interior rooms) |
| 2229361630 | Civic | courthouse | commercial / courthouse | 1,016 | 30 | 0.81 | BreakRoom, Hall, Lobby, Meeting×3, Office×7, Records×2, Stock×3 | 15 (16) | 12 | 708 | 87.2 (9.8 interior rooms) |
| 2229432452 | Civic | civic | commercial | 785 | 31 | 0.75 | BreakRoom, Hall, Lobby, Meeting×2, Office×6, Records×2, Stock×3 | 15 (16) | 11 | 634 | 75.6 (8.5 interior rooms) |
| 1474017440 | Civic | community | commercial | 547 | 30 | 0.99 | BreakRoom, Meeting, Office, Restroom, Stock | 4 (5) | 7 | 421 | 46.0 (22.5 interior rooms) |
| 2229519182 | Civic | courthouse | commercial / courthouse | 450 | 28 | 0.96 | BreakRoom, Hall, Lobby, Meeting, Office×4, Records | 9 (10) | 7 | 346 | 42.9 (11.6 interior rooms) |
| 2229822690 | Civic | community | commercial / social_centre | 381 | 29 | 0.94 | BreakRoom, Meeting, Office×2, Restroom×2, Stock | 5 (7) | 7 | 386 | 30.7 (7.1 interior rooms) |
| 2229553344 | Clinic | clinic | commercial | 705 | 29 | 0.82 | Dispensary, Exam×8, Hall, Office×3, Records, Waiting | 14 (15) | 10 lit 1 | 627 | 62.0 (3.2 interior rooms) |
| 2229427060 | FireStation | fire_station | civic / fire_station | 401 | 27 | 0.96 | Bays, Bunks, DayRoom, Office | 8 (8) | 6 | 213 | 25.8 (5.9 interior rooms) |
| 2238550564 | Garage | outbuilding | house | 32 | 25 | 1.00 | HomeGarage | 1 (1) | 1 | 34 | 2.5 (1.4 interior rooms) |
| 2229546206 | Hardware | hardware | commercial / shop:hardware | 141 | 30 | 1.00 | Restroom, Sales, Stock | 5 (6) | 3 lit 2 | 110 | 8.7 (2.3 interior rooms) |
| 2230673820 | House | home | house | 368 | 26 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 9 (9) | 12 | 249 | 37.0 (9.2 interior rooms) |
| 2229833824 | House | home | house | 293 | 28 | 0.91 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 9 (9) | 12 | 282 | 41.4 (12.1 interior doors) |
| 2238546558 | House | home | house | 261 | 28 | 0.75 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 8 (8) | 12 | 259 | 40.9 (9.7 interior rooms) |
| 2229833820 | House | home | house | 255 | 27 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 9 (9) | 12 lit 2 | 265 | 25.8 (6.4 interior rooms) |
| 2238546246 | House | home | house | 250 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 12 | 299 | 42.1 (9.1 interior rooms) |
| 2238555434 | House | home | house | 248 | 30 | 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 324 | 34.1 (6.4 interior rooms) |
| 2238546252 | House | home | house | 234 | 29 | 0.90 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 329 | 33.8 (6.5 interior rooms) |
| 2238555432 | House | home | house | 233 | 31 | 0.72 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 8 (8) | 11 lit 1 | 295 | 43.9 (11.1 interior rooms) |
| 2238546556 | House | home | house | 231 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 300 | 30.0 (6.1 interior rooms) |
| 2229549576 | House | home | house | 225 | 32 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (8) | 11 | 252 | 25.7 (7.0 interior rooms) |
| 2229565810 | House | home | house | 214 | 28 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 lit 1 | 256 | 26.0 (6.0 interior rooms) |
| 2238555430 | House | home | house | 214 | 27 | 0.85 | Bathroom, Bedroom×4, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 290 | 29.1 (7.0 interior rooms) |
| 2229833814 | House | home | house | 207 | 26 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 255 | 24.8 (6.9 interior rooms) |
| 2229549570 | House | home | house | 206 | 29 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 8 (8) | 12 | 256 | 44.0 (13.0 interior rooms) |
| 2238554394 | House | home | house | 202 | 26 | 0.89 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (7) | 11 lit 1 | 233 | 25.0 (5.9 interior rooms) |
| 2229567550 | House | home | house | 193 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (7) | 11 | 215 | 53.9 (20.3 interior rooms) |
| 2229833812 | House | home | house | 180 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 7 (7) | 11 lit 2 | 237 | 32.8 (9.1 interior rooms) |
| 2229565802 | House | home | house | 171 | 26 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 7 (7) | 11 | 230 | 32.5 (16.1 interior rooms) |
| 2238554392 | House | home | house | 168 | 28 | 0.79 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Laundry, Living | 5 (6) | 12 | 250 | 38.4 (8.4 interior doors) |
| 2238546248 | House | home | house | 165 | 28 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 7 (7) | 12 | 232 | 23.0 (5.2 interior rooms) |
| 2238554396 | House | home | house | 161 | 32 | 0.73 | Bathroom, Bedroom, Hall, HomeGarage, Kitchen, Laundry, Living | 6 (6) | 11 | 247 | 35.8 (8.5 interior rooms) |
| 2238554390 | House | home | house | 158 | 29 | 0.90 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 6 (6) | 12 lit 1 | 233 | 57.5 (21.2 interior rooms) |
| 2238550556 | House | home | house | 141 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 2 | 246 | 24.6 (7.1 interior rooms) |
| 2238550566 | House | home | house | 132 | 26 | 0.82 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Laundry, Living | 6 (6) | 11 lit 1 | 261 | 24.5 (5.2 interior rooms) |
| 2229822682 | House | home | house | 131 | 27 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 6 (6) | 11 | 245 | 23.9 (6.6 interior rooms) |
| 2229549574 | House | home | house | 125 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 218 | 35.9 (8.0 interior rooms) |
| 2229549566 | House | home | house | 125 | 29 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 | 235 | 23.3 (6.2 interior rooms) |
| 2229833816 | House | home | house | 122 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 | 217 | 22.1 (7.0 interior rooms) |
| 2229549580 | House | home | house | 114 | 32 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 213 | 21.5 (5.0 interior rooms) |
| 2229833822 | House | home | house | 102 | 28 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 10 | 197 | 43.7 (25.0 interior rooms) |
| 2238550562 | House | home | house | 101 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 5 (5) | 9 lit 1 | 187 | 18.4 (5.2 interior rooms) |
| 2229833818 | House | home | house | 91 | 26 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 | 210 | 20.0 (6.1 interior rooms) |
| 2238550560 | House | home | house | 89 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 5 (5) | 11 | 220 | 21.9 (5.2 interior rooms) |
| 2238550558 | House | home | house | 69 | 28 | 0.99 | Bathroom, Bedroom, Living | 4 (4) | 8 | 145 | 14.6 (5.3 interior rooms) |
| 1474017444 | Library | library | commercial / library | 613 | 27 | 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5 lit 1 | 304 | 33.9 (8.8 interior rooms) |
| 2229546212 | Library | library | commercial / library | 288 | 29 | 0.97 | Office×2, Records, Restroom, Stacks, Stock | 7 (8) | 5 | 234 | 32.5 (8.4 interior rooms) |
| 2229553356 | Office | commercial | commercial | 488 | 29 | 0.95 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom | 10 (11) | 10 | 629 | 67.2 (17.5 interior rooms) |
| 2229822684 | Office | office | commercial | 460 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records | 10 (11) | 9 lit 1 | 473 | 59.8 (10.3 interior doors) |
| 2229533470 | Office | office | commercial | 365 | 28 | 0.97 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records, Restroom | 8 (9) | 9 | 423 | 51.8 (9.3 interior walls) |
| 2229546196 | Office | office | commercial | 276 | 30 | 0.96 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (8) | 6 | 310 | 33.2 (7.2 interior rooms) |
| 2229565806 | Office | office | commercial | 208 | 28 | 1.00 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (7) | 6 lit 2 | 297 | 31.4 (7.2 interior rooms) |
| 2229996040 | Office | office | house | 202 | 28 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice, Records, Restroom | 6 (7) | 7 lit 1 | 251 | 24.6 (3.0 interior rooms) |
| 2229553350 | Office | office | commercial | 156 | 29 | 0.93 | Lobby, Office, Restroom | 3 (4) | 4 lit 1 | 113 | 9.7 (3.1 interior rooms) |
| 2229553352 | Office | office | commercial | 136 | 30 | 0.86 | Lobby, Office, Restroom | 2 (3) | 4 | 111 | 9.6 (3.2 interior rooms) |
| 2229546210 | Pharmacy | pharmacy | commercial | 1,195 | 29 | 1.00 | BackOffice, Dispensary, Restroom×2, Sales | 14 (16) | 6 | 307 | 24.3 (8.1 interior rooms) |
| 2230225126 | Pharmacy | pharmacy | commercial / shop:medical_supply | 549 | 27 | 0.96 | BackOffice, Dispensary, Restroom, Sales, Stock | 11 (12) | 6 lit 2 | 275 | 35.9 (18.4 interior rooms) |
| 2229427062 | Police | police | civic / police | 578 | 29 | 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 580 | 113.7 (16.2 interior rooms) |
| 2229565816 | Restaurant | commercial | commercial | 490 | 30 | 0.71 | CommercialKitchen, Eatery×2, Restroom×2 | 6 (11) | 9 | 540 | 45.6 (11.5 interior rooms) |
| 2229996042 | Restaurant | commercial | commercial | 254 | 29 | 0.95 | CommercialKitchen, Eatery, Restroom×2 | 4 (8) | 8 | 283 | 23.0 (6.6 interior rooms) |
| 2229984514 | Restaurant | restaurant | house | 94 | 31 | 0.99 | CommercialKitchen, Eatery | 4 (4) | 7 | 122 | 11.0 (6.7 interior rooms) |
| 2229549578 | Salon | salon | commercial | 364 | 31 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 6 (9) | 5 lit 2 | 184 | 16.0 (2.8 interior rooms) |
| 2229546202 | Salon | salon | commercial | 336 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 lit 1 | 141 | 12.4 (2.9 interior rooms) |
| 2229959138 | Salon | salon | commercial / shop:hairdresser | 327 | 30 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (8) | 4 | 128 | 11.1 (2.8 interior rooms) |
| 2229567552 | Salon | salon | commercial / shop:hairdresser | 250 | 28 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 4 lit 1 | 146 | 12.8 (3.0 interior rooms) |
| 2229833828 | Salon | salon | commercial / shop:hairdresser | 87 | 28 | 0.99 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 95 | 8.3 (2.1 interior rooms) |
| 2229532150 | Salon | salon | commercial / shop:hairdresser | 63 | 30 | 0.97 | Restroom, Sales, Stock | 2 (3) | 3 lit 1 | 89 | 8.8 (2.3 interior rooms) |
| 2229533468 | Store | department_store | commercial / shop:variety_store | 744 | 29 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 14 (16) | 5 | 272 | 25.5 (7.7 interior rooms) |
| 2229514212 | Store | clothing | commercial / shop:clothes | 727 | 30 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 11 (13) | 4 | 219 | 31.4 (16.1 interior rooms) |
| 2230027974 | Store | shop | commercial / shop:tobacco | 611 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 11 (12) | 4 | 240 | 19.4 (6.9 interior rooms) |
| 2229565814 | Store | clothing | commercial / shop:second_hand | 553 | 29 | 0.99 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 | 215 | 18.8 (5.7 interior rooms) |
| 2229551880 | Store | general_store | commercial / shop:general | 544 | 28 | 0.78 | BackOffice, Restroom, Sales, Stock×2 | 11 (12) | 5 lit 1 | 250 | 33.2 (16.3 interior rooms) |
| 2229515476 | Store | shop | commercial / shop:shoe_repair | 508 | 28 | 0.96 | BackOffice, Restroom, Sales, Stock | 10 (11) | 5 | 195 | 18.3 (4.8 interior rooms) |
| 2229833826 | Store | guns | commercial | 427 | 29 | 0.91 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 2 | 204 | 17.5 (5.5 interior rooms) |
| 2229553354 | Store | shop | commercial | 369 | 29 | 0.95 | BackOffice, Restroom, Sales, Stock | 8 (9) | 4 | 171 | 25.5 (6.1 interior doors) |
| 2230229660 | Store | shop | commercial / shop:florist | 361 | 27 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 184 | 31.1 (14.2 interior rooms) |
| 2229565812 | Store | furniture | commercial / shop:furniture | 333 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 176 | 26.4 (10.7 interior rooms) |
| 2229996044 | Store | furniture | commercial / shop:interior_decoration | 331 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 177 | 14.8 (4.4 interior rooms) |
| 2229833830 | Store | clothing | retail | 286 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 16.2 (4.0 interior rooms) |
| 2230229658 | Store | commercial | commercial | 282 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 185 | 19.7 (6.7 interior rooms) |
| 2229822678 | Store | commercial | commercial | 272 | 31 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 170 | 14.7 (4.1 interior rooms) |
| 2229546208 | Store | shop | commercial / shop:florist | 252 | 29 | 0.93 | Restroom, Sales, Stock | 6 (7) | 4 | 121 | 10.8 (3.4 interior rooms) |
| 2229546200 | Store | furniture | commercial / shop:furniture | 236 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 4 lit 1 | 119 | 19.6 (13.2 interior rooms) |
| 2229959136 | Store | clothing | commercial / shop:clothes | 230 | 30 | 0.99 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 117 | 9.1 (3.3 interior rooms) |
| 2229822688 | Store | laundry | house | 227 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 5 | 160 | 14.2 (3.4 interior rooms) |
| 2229546198 | Store | shop | commercial / shop:books | 219 | 29 | 0.97 | Restroom, Sales, Stock | 5 (6) | 4 lit 2 | 114 | 10.1 (3.6 interior rooms) |
| 2229565808 | Store | shop | house | 212 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 164 | 15.0 (3.8 interior rooms) |
| 2992366852 | Store | shop | house | 199 | 29 | 0.96 | Restroom, Sales, Stock | 4 (5) | 3 | 109 | 8.5 (2.8 interior rooms) |
| 2229959134 | Store | furniture | house | 197 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 117 | 10.1 (3.5 interior rooms) |
| 2234865948 | Store | pet_store | commercial / shop:pet_grooming | 143 | 2 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (5) | 5 lit 1 | 118 | 10.8 (2.7 interior rooms) |
| 2229546204 | Store | shop | house | 128 | 30 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 109 | 8.4 (2.4 interior rooms) |
| 2229565804 | Store | pet_store | commercial / shop:pet_grooming | 112 | 29 | 0.92 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 89 | 7.4 (1.7 interior doors) |
| 2229984512 | Store | clothing | house | 97 | 31 | 0.98 | Sales, Stock | 3 (3) | 2 | 68 | 16.5 (9.0 interior rooms) |

</details>

## Pikeville

KentuckyMap.Build 3,153 ms. Kentucky map Pikeville (build ky-local-f2): 57 buildings, 27 beyond the edge, 164 street pieces, 535 containers, 485 lights, 702 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 54 buildings furnished (488 rooms), 0 unplannable, 0 failed, 455 containers, 461 lights, 2617 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 22,416 | 20,788 |
| loot containers | 535 | 455 (1,764 more stripped of their storage) |
| lights | 486 | 461 (32 rooms left lit) |
| vertices | 7,091,634 | 5,877,755 |
| renderers (material slots) | | 9,763 (22,912; 1,208 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 2,437 ms in all, slowest step 35.1 ms |
| buildings furnished | | 54 (0 failed) |

<details><summary>Every building (54)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2638699002 | Apartments | apartments | apartments | 1,548 | 24 | 0.72 | Bathroom×19, Bedroom×19, BreakRoom, Hall, Laundry×3, Living×19, Lobby | 24 (24) | 55 lit 1 | 2125 | 315.1 (35.1 interior finish) |
| 2638698994 | AutoShop | commercial | commercial | 338 | 30 | 0.93 | Bays, Restroom, Sales | 5 (6) | 4 | 111 | 10.6 (4.4 interior rooms) |
| 2638698982 | BigStore | commercial | commercial | 892 | 29 | 0.50 | BackOffice, Office×2, Restroom×2, Sales, Stock×2 | 14 (16) | 5 lit 1 | 313 | 33.1 (8.4 interior rooms) |
| 2230524586 | Church | church | church / place_of_worship | 841 | 36 | 0.81 | Meeting×2, Nave, Sacristy×3 | 5 (5) | 6 lit 1 | 440 | 38.7 (8.9 interior rooms) |
| 2230524588 | Church | church | church / place_of_worship | 685 | 35 | 0.92 | Meeting, Nave, Sacristy×2 | 3 (3) | 6 | 388 | 23.5 (9.4 interior rooms) |
| 1729270072 | Civic | community | commercial / exhibition_centre | 7,904 | 9 | 0.82 | BreakRoom, Meeting, Office×3, Restroom×2, Stock×2 | 14 (16) | 9 lit 2 | 592 | 55.1 (12.6 interior rooms) |
| 2230524598 | Civic | courthouse | commercial / courthouse | 3,337 | 30 | 0.84 | BreakRoom, Hall, Lobby, Meeting×5, Office×12, Records×5, Stock×6 | 16 (16) | 13 lit 1 | 1310 | 134.9 (7.8 interior rooms) |
| 2230524592 | Civic | community | commercial | 2,480 | 31 | 0.89 | BreakRoom, Meeting, Office×3, Restroom×2, Stock | 9 (11) | 8 lit 1 | 527 | 46.8 (12.0 interior rooms) |
| 2230524590 | Civic | courthouse | commercial | 1,723 | 30 | 0.79 | BreakRoom, Hall, Lobby, Meeting×2, Office×7, Records×2, Stock×3 | 16 (16) | 9 lit 2 | 703 | 80.2 (11.1 interior rooms) |
| 2230524584 | Civic | courthouse | commercial | 834 | 20 | 0.68 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records×2, Restroom | 10 (11) | 10 lit 1 | 526 | 47.1 (4.4 interior rooms) |
| 1924315432 | Civic | community | commercial / theatre | 568 | 27 | 0.83 | BreakRoom, Meeting, Office×2, Restroom×2, Stock×2 | 8 (10) | 8 lit 1 | 495 | 48.2 (10.3 interior rooms) |
| 2638698934 | Hardware | commercial | commercial | 590 | 30 | 0.90 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 1 | 203 | 18.4 (5.8 interior rooms) |
| 2638698974 | House | home | house | 203 | 39 | 0.90 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 7 (7) | 11 lit 1 | 266 | 29.0 (5.5 interior rooms) |
| 2638698942 | House | home | house | 144 | 28 | 0.80 | Bathroom, Bedroom, Kitchen, Living | 6 (6) | 9 lit 1 | 189 | 19.5 (5.0 interior rooms) |
| 2638698992 | House | home | house | 144 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 212 | 22.6 (7.4 interior rooms) |
| 2638698920 | House | home | house | 138 | 40 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 245 | 25.1 (8.3 interior rooms) |
| 2638699000 | House | home | house | 137 | 26 | 0.93 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (6) | 11 lit 1 | 200 | 20.4 (7.6 interior rooms) |
| 2638698912 | House | home | house | 136 | 31 | 0.93 | Bathroom, Bedroom, Kitchen, Living | 6 (6) | 9 | 191 | 24.3 (7.4 interior rooms) |
| 2638698948 | House | home | house | 113 | 26 | 0.98 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 217 | 22.4 (7.0 interior rooms) |
| 2638698962 | House | home | house | 109 | 34 | 0.75 | Bathroom, Bedroom, Laundry, Living | 5 (5) | 9 | 174 | 18.8 (6.3 interior rooms) |
| 2638698946 | House | home | house | 72 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 7 | 152 | 15.5 (3.7 interior rooms) |
| 2230524594 | Office | office | commercial | 1,355 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×9, OpenOffice×4, Records×4, Stock×2 | 16 (16) | 10 | 758 | 130.6 (16.5 interior doors) |
| 2638698986 | Office | commercial | commercial | 618 | 19 | 0.76 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records, Stock | 12 (12) | 9 lit 1 | 554 | 108.0 (22.7 interior rooms) |
| 2638698998 | Office | commercial | commercial | 555 | 29 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 10 (12) | 9 | 491 | 79.2 (19.7 interior rooms) |
| 2638698924 | Office | commercial | commercial | 412 | 38 | 0.96 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom, Stock×3 | 10 (10) | 11 lit 1 | 462 | 46.3 (4.3 interior rooms) |
| 2230524604 | Office | office | commercial | 321 | 27 | 0.80 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records×2, Restroom, Stock | 6 (8) | 9 | 440 | 56.2 (12.7 interior walls) |
| 2638698936 | Office | commercial | commercial | 317 | 26 | 0.83 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (8) | 7 | 376 | 41.1 (13.2 interior rooms) |
| 2576866036 | Police | police | commercial / police | 362 | 30 | 1.00 | Armory, Cells, Hall, Lobby, Office, Squad | 7 (8) | 5 | 255 | 26.5 (8.0 interior rooms) |
| 1696416174 | Restaurant | restaurant | commercial / restaurant | 436 | 10 | 0.57 | CommercialKitchen, Eatery×4, Restroom×2 | 6 (9) | 9 lit 1 | 428 | 36.3 (9.2 interior rooms) |
| 2638698972 | Restaurant | commercial | commercial | 364 | 39 | 0.94 | CommercialKitchen, Eatery, Restroom×2 | 2 (9) | 9 | 325 | 50.4 (29.0 interior rooms) |
| 2638698954 | Restaurant | commercial | commercial | 294 | 31 | 0.84 | CommercialKitchen, Eatery, Restroom×2 | 6 (8) | 10 | 358 | 37.4 (12.2 interior rooms) |
| 1728600610 | Restaurant | restaurant | commercial / restaurant | 212 | 20 | 0.83 | CommercialKitchen, Eatery×2, Restroom×2 | 5 (7) | 8 lit 1 | 275 | 26.2 (6.8 interior rooms) |
| 1729248694 | School | school | school | 1,775 | 40 | 0.67 | BreakRoom, Classroom×14, Hall, Lobby, Office | 13 (14) | 12 | 894 | 97.5 (17.6 interior finish) |
| 1742875468 | School | school | school | 1,488 | 36 | 0.96 | BreakRoom, Classroom×8, Hall, Lobby, Office | 11 (12) | 10 | 609 | 70.2 (8.6 interior walls) |
| 1729270030 | School | school | school | 1,206 | 28 | 0.87 | BreakRoom, Classroom×9, Hall, Lobby, Office | 10 (11) | 10 lit 1 | 686 | 68.9 (9.7 interior walls) |
| 1729270046 | School | school | school | 801 | 18 | 0.69 | BreakRoom, Classroom×5, Hall, Lobby, Office×2 | 8 (9) | 8 | 503 | 59.3 (14.5 interior rooms) |
| 1728609676 | School | school | school | 516 | 21 | 0.95 | BreakRoom, Classroom×7, Hall, Lobby, Office×2, Restroom | 7 (8) | 10 | 510 | 57.6 (8.8 interior walls) |
| 2638698944 | Store | commercial | commercial | 668 | 20 | 0.77 | BackOffice, Office×2, Restroom, Sales, Stock×2 | 12 (13) | 6 | 303 | 39.4 (12.4 interior rooms) |
| 2638698996 | Store | commercial | commercial | 656 | 28 | 0.84 | BackOffice, Office, Restroom, Sales, Stock | 12 (13) | 6 | 264 | 37.0 (8.9 interior rooms) |
| 2638698966 | Store | commercial | commercial | 435 | 19 | 0.84 | BackOffice, Restroom, Sales, Stock×3 | 9 (10) | 5 lit 1 | 237 | 19.6 (5.7 interior rooms) |
| 2638698958 | Store | commercial | commercial | 389 | 30 | 0.66 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 6 | 215 | 20.2 (3.9 interior rooms) |
| 2638698914 | Store | commercial | commercial | 372 | 31 | 0.86 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 161 | 34.7 (12.0 interior rooms) |
| 2638698984 | Store | commercial | commercial | 346 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 2 | 179 | 17.6 (5.0 interior rooms) |
| 2638698968 | Store | commercial | commercial | 303 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 18.3 (5.1 interior rooms) |
| 2230524602 | Store | commercial | retail | 179 | 30 | 0.96 | BackOffice, Restroom, Sales, Stock | 5 (6) | 5 lit 2 | 148 | 17.8 (4.9 interior rooms) |
| 2230524600 | Warehouse | commercial | commercial | 2,302 | 31 | 0.84 | Office, Restroom, Stock×2, Warehouse×2, Workshop×2 | 15 (16) | 5 lit 1 | 274 | 25.2 (4.6 interior rooms) |
| 1919622998 | Warehouse | distillery | commercial | 1,891 | 34 | 0.76 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 9 (9) | 5 | 263 | 23.5 (4.5 interior rooms) |
| 2638698918 | Warehouse | commercial | commercial | 662 | 29 | 0.82 | Office, Restroom, Warehouse | 6 (7) | 5 | 128 | 28.5 (16.5 interior rooms) |
| 2638698956 | Warehouse | commercial | commercial | 466 | 25 | 0.86 | Office, Restroom, Stock×3, Warehouse×2 | 7 (9) | 5 | 174 | 24.7 (9.5 interior doors) |
| 2638698976 | Warehouse | commercial | commercial | 405 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 5 (7) | 5 | 138 | 14.4 (3.1 interior rooms) |
| 2638698952 | Warehouse | commercial | commercial | 387 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 5 lit 2 | 143 | 15.0 (3.5 interior rooms) |
| 2638698932 | Workshop | commercial | commercial | 450 | 30 | 0.84 | Office, Restroom, Stock×2, Workshop×2 | 8 (9) | 5 | 171 | 29.7 (11.1 interior rooms) |
| 2638698930 | Workshop | commercial | commercial | 409 | 38 | 0.90 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 149 | 15.0 (3.4 interior rooms) |
| 2638698990 | Workshop | commercial | commercial | 268 | 40 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 157 | 15.4 (3.1 interior rooms) |

</details>

## Middlesboro

KentuckyMap.Build 1,350 ms. Kentucky map Middlesboro (build ky-local-f2): 31 buildings, 15 beyond the edge, 58 street pieces, 399 containers, 225 lights, 2397 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 31 buildings furnished (191 rooms), 0 unplannable, 0 failed, 285 containers, 205 lights, 971 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 9,388 | 7,884 |
| loot containers | 399 | 285 (555 more stripped of their storage) |
| lights | 225 | 205 (5 rooms left lit) |
| vertices | 3,140,799 | 2,083,787 |
| renderers (material slots) | | 3,941 (9,140; 282 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 887 ms in all, slowest step 22.6 ms |
| buildings furnished | | 31 (0 failed) |

<details><summary>Every building (31)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2784318768 | Bank | bank | commercial | 2,005 | 7 | 0.88 | BreakRoom, Lobby, Office×3, Records, Restroom, Stock, Vault | 15 (16) | 9 | 409 | 53.6 (9.7 interior rooms) |
| 2784318778 | BigStore | guns | commercial | 2,059 | 7 | 0.75 | BackOffice, Office×2, Restroom×2, Sales, Stock | 14 (16) | 6 lit 1 | 376 | 32.2 (9.6 interior rooms) |
| 2784318782 | BigStore | guns | commercial | 1,804 | 7 | 0.68 | BackOffice, Restroom×2, Sales, Stock×4 | 14 (16) | 5 | 357 | 29.6 (7.6 interior rooms) |
| 2207404908 | Church | church | church / place_of_worship | 460 | 7 | 0.66 | Meeting×3, Nave, Sacristy×2 | 5 (5) | 6 lit 1 | 298 | 21.3 (5.1 interior rooms) |
| 2589948122 | FireStation | fire_station | commercial / fire_station | 835 | 7 | 0.91 | Bays, Bunks, DayRoom, Office, Restroom, Stock | 11 (12) | 7 | 300 | 29.6 (5.5 interior rooms) |
| 1807972230 | Funeral | funeral | commercial / shop:funeral_directors | 550 | 6 | 0.77 | Chapel, Office, Restroom, Stock | 4 (4) | 5 | 231 | 19.1 (5.0 interior rooms) |
| 3004391868 | House | home | house | 233 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 7 (8) | 11 | 221 | 33.4 (8.6 interior rooms) |
| 3004391878 | House | home | house | 206 | 6 | 0.88 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 6 (7) | 11 | 231 | 33.1 (16.5 interior rooms) |
| 3004391870 | House | home | house | 193 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 6 (7) | 11 | 221 | 22.3 (7.5 interior rooms) |
| 3004391882 | House | home | house | 190 | 6 | 0.95 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 7 (7) | 11 | 236 | 25.5 (7.1 interior rooms) |
| 3004391880 | House | home | house | 188 | 6 | 0.87 | Bathroom, Bedroom, Hall, Kitchen, Living | 5 (6) | 11 | 200 | 24.1 (5.9 interior rooms) |
| 3004391872 | House | home | house | 73 | 5 | 0.97 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 10 | 164 | 15.6 (4.4 interior rooms) |
| 2589948126 | Library | library | commercial / library | 1,001 | 6 | 0.80 | Office×3, Records, Restroom, Stacks, Stock | 11 (12) | 6 | 333 | 61.1 (22.6 interior rooms) |
| 2784318772 | Office | commercial | commercial | 442 | 7 | 0.98 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock | 9 (10) | 9 | 461 | 58.9 (7.2 interior rooms) |
| 1774615624 | Pharmacy | pharmacy | commercial | 1,533 | 7 | 0.94 | BackOffice, Dispensary, Office, Restroom×2, Sales | 14 (16) | 6 | 387 | 48.4 (16.7 interior rooms) |
| 2589948124 | Police | police | commercial / police | 489 | 6 | 1.00 | Armory, BreakRoom, Cells, Hall, Lobby, Office×3, Records, Squad | 10 (11) | 7 | 376 | 37.4 (7.1 interior rooms) |
| 2589948132 | PostOffice | post_office | commercial / post_office | 744 | 7 | 0.91 | BreakRoom, Lobby, Office, Restroom, Sorting, Stock×2 | 13 (14) | 7 lit 1 | 339 | 32.5 (8.5 interior rooms) |
| 2784318788 | Salon | salon | commercial | 690 | 7 | 0.91 | BackOffice, Office, Restroom×2, Sales, Stock | 6 (11) | 6 | 218 | 32.2 (8.4 interior doors) |
| 3004391874 | Store | clothing | commercial | 448 | 6 | 1.00 | BackOffice, Restroom, Sales, Stock | 10 (11) | 4 | 161 | 14.9 (3.2 interior rooms) |
| 3004391876 | Store | commercial | commercial | 444 | 6 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 9 (10) | 5 lit 1 | 219 | 51.5 (11.3 interior rooms) |
| 3004391864 | Store | commercial | commercial | 392 | 7 | 0.99 | Restroom, Sales, Stock | 8 (9) | 4 | 144 | 11.4 (4.9 interior rooms) |
| 3004391866 | Store | clothing | commercial | 343 | 7 | 0.98 | Restroom, Sales, Stock | 7 (8) | 4 lit 1 | 139 | 11.2 (4.6 interior rooms) |
| 2784318774 | Warehouse | warehouse | warehouse | 2,797 | 7 | 0.81 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 11 (11) | 5 | 269 | 25.3 (4.4 interior rooms) |
| 2784318780 | Warehouse | commercial | commercial | 1,983 | 7 | 0.92 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 14 (16) | 5 | 262 | 34.1 (15.5 interior rooms) |
| 2784318790 | Warehouse | commercial | commercial | 1,302 | 8 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 190 | 19.5 (5.2 interior rooms) |
| 3004391884 | Warehouse | commercial | commercial | 1,247 | 6 | 0.99 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 194 | 22.3 (6.5 interior rooms) |
| 2784318786 | Warehouse | commercial | commercial | 1,159 | 6 | 0.59 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 13 (14) | 5 | 233 | 19.7 (3.4 interior rooms) |
| 2784318792 | Warehouse | commercial | commercial | 1,146 | 7 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 4 | 182 | 18.3 (4.5 interior rooms) |
| 2784318770 | Workshop | commercial | commercial | 876 | 7 | 0.99 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 | 171 | 16.1 (3.1 interior rooms) |
| 2784318784 | Workshop | commercial | commercial | 762 | 7 | 0.95 | Office, Restroom, Stock, Workshop×3 | 10 (11) | 5 | 201 | 18.1 (2.8 interior rooms) |
| 1807972232 | Workshop | commercial | commercial | 372 | 7 | 0.75 | Office, Restroom, Workshop×2 | 8 (9) | 5 | 161 | 14.1 (3.0 interior rooms) |

</details>

## By use (every square)

| use | buildings | failed | rooms (mean) | containers mean / max | allotted mean | lights mean / max | lit rooms | objects mean / max | renderers mean (slots) | vertices mean (k) | ms mean / max | slowest step ms |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Apartments | 2 | 0 | 50.0 | 24.0 / 24 | 24.0 | 47.0 / 55 | 1 | 1,781 / 2125 | 1,102 (2,422) | 540.0 | 243.7 / 315.1 | 35.1 |
| AutoShop | 4 | 0 | 2.5 | 4.3 / 5 | 5.0 | 3.3 / 4 | 1 | 96 / 115 | 47 (109) | 21.6 | 13.4 / 21.1 | 18.5 |
| Bank | 6 | 0 | 7.7 | 9.8 / 15 | 10.5 | 6.7 / 9 | 2 | 322 / 409 | 156 (372) | 96.7 | 39.8 / 53.6 | 9.7 |
| Bar | 2 | 0 | 3.5 | 5.5 / 6 | 8.5 | 7.5 / 12 | 2 | 164 / 228 | 82 (159) | 47.9 | 31.3 / 54.6 | 29.2 |
| Barn | 6 | 0 | 3.8 | 5.0 / 6 | 5.3 | 3.8 / 4 | 1 | 106 / 121 | 45 (99) | 21.6 | 11.1 / 18.8 | 5.4 |
| BigStore | 5 | 0 | 7.0 | 11.8 / 14 | 13.8 | 5.2 / 6 | 3 | 309 / 376 | 155 (371) | 78.5 | 31.6 / 35.5 | 12.8 |
| Church | 7 | 0 | 5.7 | 4.7 / 6 | 4.7 | 5.6 / 6 | 3 | 381 / 443 | 79 (196) | 85.8 | 28.2 / 38.7 | 10.8 |
| Civic | 12 | 0 | 12.7 | 11.0 / 16 | 12.2 | 9.2 / 13 | 8 | 592 / 1310 | 230 (562) | 155.6 | 62.9 / 134.9 | 22.5 |
| Clinic | 2 | 0 | 14.5 | 13.0 / 14 | 13.5 | 9.5 / 10 | 1 | 585 / 627 | 286 (721) | 169.5 | 69.7 / 77.4 | 16.6 |
| Convenience | 2 | 0 | 5.5 | 8.5 / 10 | 9.5 | 5.0 / 5 | 0 | 220 / 229 | 109 (266) | 54.2 | 35.0 / 48.6 | 13.9 |
| Cottage | 2 | 0 | 3.0 | 2.0 / 2 | 3.0 | 4.5 / 5 | 0 | 102 / 114 | 55 (123) | 26.9 | 11.5 / 12.2 | 5.0 |
| FireStation | 2 | 0 | 5.0 | 9.5 / 11 | 10.0 | 6.5 / 7 | 0 | 257 / 300 | 119 (263) | 71.7 | 27.7 / 29.6 | 5.9 |
| Funeral | 1 | 0 | 4.0 | 4.0 / 4 | 4.0 | 5.0 / 5 | 0 | 231 / 231 | 78 (187) | 56.0 | 19.1 / 19.1 | 5.0 |
| Garage | 3 | 0 | 1.0 | 1.3 / 2 | 1.3 | 1.0 / 1 | 0 | 37 / 43 | 17 (37) | 5.7 | 3.0 / 3.7 | 2.5 |
| Hardware | 4 | 0 | 4.0 | 8.5 / 12 | 9.5 | 4.3 / 5 | 4 | 181 / 206 | 87 (210) | 41.5 | 16.9 / 20.3 | 8.7 |
| House | 103 | 0 | 6.4 | 6.4 / 9 | 6.5 | 10.8 / 12 | 41 | 240 / 339 | 141 (306) | 59.2 | 31.3 / 82.0 | 30.1 |
| Library | 3 | 0 | 6.7 | 9.3 / 11 | 10.3 | 5.3 / 6 | 1 | 290 / 333 | 141 (333) | 92.3 | 42.5 / 61.1 | 22.6 |
| Motel | 2 | 0 | 17.0 | 15.5 / 18 | 15.5 | 14.5 / 19 | 0 | 542 / 566 | 320 (712) | 136.6 | 135.2 / 158.3 | 44.9 |
| Office | 19 | 0 | 11.3 | 8.4 / 16 | 9.3 | 8.2 / 11 | 8 | 423 / 758 | 242 (592) | 113.5 | 56.2 / 130.6 | 28.0 |
| Pharmacy | 5 | 0 | 5.2 | 13.2 / 14 | 14.8 | 5.8 / 6 | 2 | 305 / 387 | 157 (369) | 65.1 | 40.4 / 53.8 | 18.4 |
| Police | 3 | 0 | 10.7 | 9.7 / 12 | 10.3 | 7.7 / 11 | 0 | 404 / 580 | 215 (518) | 108.3 | 59.2 / 113.7 | 16.2 |
| PostOffice | 1 | 0 | 7.0 | 13.0 / 13 | 14.0 | 7.0 / 7 | 1 | 339 / 339 | 154 (327) | 115.2 | 32.5 / 32.5 | 8.5 |
| Restaurant | 11 | 0 | 4.4 | 5.6 / 9 | 8.5 | 9.4 / 12 | 3 | 355 / 540 | 138 (250) | 146.7 | 36.6 / 62.1 | 29.0 |
| Salon | 10 | 0 | 4.3 | 4.9 / 7 | 7.6 | 4.3 / 6 | 8 | 152 / 218 | 74 (181) | 39.7 | 15.3 / 32.2 | 8.4 |
| School | 5 | 0 | 13.2 | 9.8 / 13 | 10.8 | 10.0 / 12 | 1 | 640 / 894 | 244 (622) | 217.5 | 70.7 / 97.5 | 17.6 |
| Shed | 2 | 0 | 1.0 | 1.0 / 1 | 1.0 | 1.0 / 1 | 0 | 32 / 32 | 15 (31) | 5.2 | 2.8 / 2.9 | 1.8 |
| Store | 47 | 0 | 4.1 | 7.6 / 14 | 8.6 | 4.6 / 6 | 24 | 177 / 309 | 86 (210) | 43.5 | 19.8 / 51.5 | 16.3 |
| Warehouse | 18 | 0 | 5.8 | 8.8 / 15 | 10.0 | 4.9 / 5 | 4 | 192 / 274 | 94 (224) | 51.8 | 34.6 / 269.4 | 113.9 |
| Workshop | 14 | 0 | 5.0 | 7.7 / 14 | 8.7 | 4.6 / 5 | 3 | 163 / 211 | 83 (187) | 39.3 | 17.3 / 29.7 | 11.1 |

303 buildings furnished, 2267 loot containers in them.

<details><summary>Room furnishing time by role (ms)</summary>

| role | rooms | mean | median | p95 | max |
|---|--:|--:|--:|--:|--:|
| Armory | 3 | 4.3 | 2.0 | 9.2 | 9.2 |
| BackOffice | 58 | 3.3 | 3.1 | 7.6 | 10.7 |
| Bar | 2 | 3.5 | 4.4 | 4.4 | 4.4 |
| Barn | 10 | 2.2 | 2.1 | 5.4 | 5.4 |
| Bathroom | 143 | 2.8 | 2.1 | 6.4 | 30.1 |
| Bays | 6 | 7.5 | 5.7 | 18.5 | 18.5 |
| Bedroom | 222 | 2.8 | 2.4 | 6.3 | 12.3 |
| BreakRoom | 44 | 4.7 | 4.3 | 13.6 | 29.0 |
| Bunks | 2 | 4.9 | 5.9 | 5.9 | 5.9 |
| Cells | 3 | 5.3 | 2.5 | 11.3 | 11.3 |
| Chapel | 1 | 5.0 | 5.0 | 5.0 | 5.0 |
| Classroom | 43 | 3.3 | 2.9 | 4.8 | 14.5 |
| Closet | 1 | 0.8 | 0.8 | 0.8 | 0.8 |
| CommercialKitchen | 12 | 10.4 | 11.2 | 14.5 | 14.5 |
| DayRoom | 2 | 3.7 | 4.5 | 4.5 | 4.5 |
| Den | 6 | 2.7 | 2.8 | 3.5 | 3.5 |
| Dining | 31 | 3.7 | 3.0 | 8.0 | 8.9 |
| Dispensary | 7 | 7.4 | 5.0 | 16.7 | 16.7 |
| Eatery | 18 | 8.6 | 6.3 | 29.0 | 29.0 |
| Exam | 14 | 4.1 | 3.2 | 16.6 | 16.6 |
| Guest | 20 | 3.8 | 2.8 | 18.7 | 18.7 |
| Hall | 125 | 0.2 | 0.0 | 0.5 | 4.0 |
| HomeGarage | 16 | 1.7 | 1.5 | 3.4 | 3.4 |
| Kitchen | 98 | 8.1 | 7.0 | 21.1 | 25.0 |
| Laundry | 30 | 1.9 | 1.3 | 7.1 | 12.5 |
| Living | 135 | 4.0 | 3.2 | 8.1 | 19.7 |
| Lobby | 45 | 2.8 | 2.9 | 5.7 | 7.2 |
| Meeting | 42 | 3.4 | 2.2 | 12.0 | 22.5 |
| Narthex | 1 | 1.2 | 1.2 | 1.2 | 1.2 |
| Nave | 7 | 8.4 | 8.9 | 10.8 | 10.8 |
| Office | 231 | 4.0 | 3.0 | 8.6 | 113.9 |
| OpenOffice | 33 | 7.2 | 3.7 | 22.7 | 28.0 |
| Records | 54 | 2.5 | 1.9 | 8.4 | 11.1 |
| Restroom | 179 | 2.0 | 2.0 | 3.5 | 15.4 |
| Sacristy | 12 | 3.7 | 3.4 | 7.8 | 7.8 |
| Sales | 76 | 5.8 | 4.7 | 13.9 | 18.4 |
| Shed | 2 | 1.6 | 1.8 | 1.8 | 1.8 |
| Sorting | 1 | 8.5 | 8.5 | 8.5 | 8.5 |
| Squad | 3 | 7.2 | 7.1 | 8.0 | 8.0 |
| Stacks | 3 | 12.1 | 8.8 | 22.6 | 22.6 |
| Stock | 180 | 2.2 | 1.7 | 7.1 | 16.1 |
| Vault | 6 | 2.5 | 1.7 | 5.5 | 5.5 |
| Waiting | 2 | 3.1 | 3.2 | 3.2 | 3.2 |
| Warehouse | 33 | 3.4 | 2.5 | 15.5 | 17.3 |
| Workshop | 45 | 2.4 | 2.3 | 3.2 | 7.0 |

</details>

## Pictures

Left: from straight above in the building's own axes (its front door's wall at the bottom), roofs, upper storeys and ceilings off, plain overcast daylight without shadows. Right: the game's isometric camera (widened to fit the building), the red capsule standing in for the survivor in its biggest room, the cutaway run (its roof and the walls turned to the camera lifted), ceilings hidden as in the game; interior partitions aren't cut here (the game's IsoCutaway does that in play).

| file | place | building | footprint | rooms | containers (allotted) | lights | objects |
|---|---|---|---|---|--:|--:|--:|
| `01_house_2622504514.png` | Bardstown | House (home; house) 2622504514 | 219 m², turned 15°, 6 corners, fill 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 8 (8) | 11 | 339 |
| `02_cottage_2622506312.png` | Bardstown | Cottage (outbuilding; house) 2622506312 | 57 m², turned 13°, 4 corners, fill 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 |
| `03_motel_1915619802.png` | Bardstown | Motel (hotel; commercial) 1915619802 | 823 m², turned 15°, 9 corners, fill 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 566 |
| `04_store_2622504474.png` | Bardstown | Store (clothing; commercial) 2622504474 | 727 m², turned 14°, 12 corners, fill 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 |
| `05_bigstore_2363563962.png` | Bardstown | BigStore (grocery; commercial / shop:pastry) 2363563962 | 432 m², turned 13°, 10 corners, fill 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 |
| `06_convenience_2622504504.png` | Bardstown | Convenience (convenience; commercial) 2622504504 | 304 m², turned 14°, 10 corners, fill 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 |
| `07_pharmacy_281586026.png` | Bardstown | Pharmacy (pharmacy; commercial / pharmacy) 281586026 | 994 m², turned 15°, 6 corners, fill 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 |
| `08_hardware_2622505338.png` | Bardstown | Hardware (commercial; commercial) 2622505338 | 336 m², turned 14°, 8 corners, fill 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 206 |
| `09_autoshop_2363563990.png` | Bardstown | AutoShop (commercial; commercial) 2363563990 | 254 m², turned 14°, 5 corners, fill 0.95 | Bays | 3 (3) | 2 | 58 |
| `10_salon_2363563974.png` (+ `10_salon_2363563974_night.png`) | Bardstown | Salon (salon; commercial) 2363563974 | 511 m², turned 12°, 6 corners, fill 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4, 2 lit | 188 |
| `11_restaurant_2622504478.png` (+ `11_restaurant_2622504478_night.png`) | Bardstown | Restaurant (restaurant; commercial) 2622504478 | 379 m², turned 14°, 10 corners, fill 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12, 1 lit | 513 |
| `12_bar_2363563996.png` (+ `12_bar_2363563996_night.png`) | Bardstown | Bar (bar; house) 2363563996 | 209 m², turned 13°, 5 corners, fill 0.99 | Bar, Restroom, Stock | 5 (6) | 3, 1 lit | 99 |
| `13_office_2622504446.png` | Bardstown | Office (commercial; commercial) 2622504446 | 388 m², turned 14°, 6 corners, fill 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 462 |
| `14_bank_2622504490.png` | Bardstown | Bank (bank; commercial) 2622504490 | 584 m², turned 14°, 8 corners, fill 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 352 |
| `15_civic_2622504470.png` | Bardstown | Civic (courthouse; commercial) 2622504470 | 554 m², turned 14°, 5 corners, fill 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 |
| `16_clinic_2363564004.png` | Bardstown | Clinic (clinic; commercial) 2363564004 | 543 m², turned 14°, 4 corners, fill 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 |
| `17_house_2238555434.png` (+ `17_house_2238555434_night.png`) | Princeton | House (home; house) 2238555434 | 248 m², turned 30°, 6 corners, fill 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 8 (8) | 12, 1 lit | 324 |
| `18_apartments_2229833810.png` | Princeton | Apartments (apartments; apartments) 2229833810 | 1,310 m², turned 27°, 6 corners, fill 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1437 |
| `19_library_1474017444.png` | Princeton | Library (library; commercial / library) 1474017444 | 613 m², turned 27°, 8 corners, fill 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5, 1 lit | 304 |
| `20_police_2229427062.png` | Princeton | Police (police; civic / police) 2229427062 | 578 m², turned 29°, 4 corners, fill 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 580 |
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

- `01_house_2622504514.png`: pieces 77: Wall×18, DoorHead×8, RoomLight×5, Furniture_Dresser×4, Bed×3, Furniture_Chair×3, Wardrobe×3, Desk×2, Door×2, Floors×2, Fridge×2, Furniture Living Room×2, Furniture_Armchair×2, Furniture_Bookshelf×2, …; vertices 89,297: DoorHead×17312, Wall×9495, Furniture Living Room×6756, Fridge×5340, Stove×5119, Furniture_Dresser×4968, Wardrobe×4824, Door×3736, Furniture×3690, Furniture_Counter×2956, …; renderers 200 (457 material slots): Wall×41, DoorHead×24, Furniture_Dresser×16, Stove×14, Fridge×10, RoomLight×10, Bed×9, Wardrobe×9, Furniture_Nightstand×8, Bath×7, …; lights: RoomLight×5, Fridge×2, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1
- `02_cottage_2622506312.png`: pieces 24: Wall×4, RoomLight×3, DoorHead×2, Floors×2, Furniture Living Room×2, Bath×1, Coffee table×1, Colliders×1, Desk×1, Door×1, Furniture×1, Furniture_Armchair×1, Furniture_Bookshelf×1, Sofa×1, …; vertices 24,455: Furniture Living Room×8118, DoorHead×4328, Furniture×2538, Door×1868, Wardrobe×1690, Bath×1494, Wall×1248, Sofa×682, TV×645, Furniture_Bookshelf×556, …; renderers 42 (106 material slots): Bath×7, Wall×7, DoorHead×6, RoomLight×6, TV×3, Wardrobe×3, Door×2, Floors×2, Furniture Living Room×2, Furniture×1, …; lights: RoomLight×3, TV×1
- `03_motel_1915619802.png`: pieces 138: Wall×31, Chair×17, DoorHead×14, Bed×12, Furniture_Dresser×12, Wardrobe×12, Furniture_Tv×9, RoomLight×5, Door×2, Floors×2, Furniture_Nightstand×2, Shelves×2, Table×2, Colliders×1, …; vertices 148,783: DoorHead×30296, Wardrobe×19940, Furniture_Dresser×15320, Wall×11108, Furniture Common Room×10948, Furniture_Tv×9385, Bed×8556, Furniture×7026, Stove×5119, Floors×4595, …; renderers 323 (716 material slots): Wall×54, Furniture_Dresser×48, DoorHead×42, Bed×36, Wardrobe×36, Furniture_Tv×27, Stove×14, RoomLight×10, Furniture_Nightstand×8, Front desk×7, …; lights: Furniture_Tv×9, RoomLight×5, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1, TV×1
- `04_store_2622504474.png`: pieces 101: Store shelves×32, Wall×13, Lockers×11, Shelves×11, Wardrobe×9, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Checkout counter×1, Colliders×1, …; vertices 82,161: DoorHead×12984, Lockers×12965, Wardrobe×10772, Store shelves×9248, Furniture×8541, Shelves×7652, Furniture_Basin×5164, Door×3736, Wall×3432, Floors×2696, …; renderers 151 (333 material slots): Store shelves×32, Wardrobe×21, DoorHead×18, Wall×17, Furniture_Basin×12, Lockers×11, Shelves×11, RoomLight×8, Filing cabinet×5, Door×4, …; lights: RoomLight×4
- `05_bigstore_2363563962.png`: pieces 81: Shelves×17, Wall×12, Store shelves×11, DoorHead×6, Furniture_Basin×4, RoomLight×4, Bookcase×3, Drinks cooler×3, Filing cabinet×3, Chair×2, Door×2, Floors×2, Furniture Office×2, Office chair×2, …; vertices 73,105: DoorHead×12984, Shelves×12132, Furniture_Basin×10328, Office desk×5305, Furniture×4565, Furniture Office×4512, Door×3736, Wall×3524, Filing cabinet×3230, Store shelves×3184, …; renderers 149 (358 material slots): Furniture_Basin×24, DoorHead×18, Wall×18, Shelves×17, Office desk×16, Store shelves×11, Filing cabinet×9, RoomLight×8, Drinks cooler×6, Door×4, …; lights: RoomLight×4, Office desk×1
- `06_convenience_2622504504.png`: pieces 63: Shelves×18, Wall×10, Store shelves×6, DoorHead×5, RoomLight×4, Bookcase×2, Crate×2, Door×2, Filing cabinet×2, Floors×2, Chair×1, Checkout counter×1, Colliders×1, Drinks cooler×1, …; vertices 52,897: Shelves×12884, DoorHead×10820, Furniture×5052, Wall×3164, Office desk×2818, Furniture_Basin×2582, Filing cabinet×2496, Furniture Office×2496, Door×2156, Floors×1838, …; renderers 107 (264 material slots): Shelves×18, Wall×17, DoorHead×15, Office desk×9, RoomLight×8, Filing cabinet×7, Furniture_Basin×6, Store shelves×6, Crate×4, Door×4, …; lights: RoomLight×4, Office desk×1
- `07_pharmacy_281586026.png`: pieces 89: Store shelves×30, Shelves×10, Wall×10, Medicine cabinet×7, DoorHead×5, stall×5, RoomLight×4, Checkout counter×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Chair×1, Colliders×1, …; vertices 68,172: DoorHead×10820, Store shelves×8736, Shelves×7540, Furniture×7038, Medicine cabinet×5748, Furniture_Basin×5164, Floors×3744, Door×3736, Office desk×2858, Wall×2780, …; renderers 161 (380 material slots): Medicine cabinet×35, Store shelves×30, DoorHead×15, Wall×13, Furniture_Basin×12, Shelves×10, Office desk×9, RoomLight×8, Medical fridge×5, stall×5, …; lights: RoomLight×4, Medical fridge×1, Office desk×1
- `08_hardware_2622505338.png`: pieces 61: Shelves×16, Store shelves×8, Wall×8, DoorHead×4, RoomLight×4, Bookcase×2, Door×2, Filing cabinet×2, Floors×2, Office chair×2, Office desk×2, Chair×1, Checkout counter×1, Colliders×1, …; vertices 50,640: Shelves×10760, DoorHead×8656, Office desk×4952, Furniture×3592, Furniture_Basin×2582, Floors×2528, Wall×2516, Store shelves×2324, Furniture Office×2256, Furniture Back Office×2208, …; renderers 103 (259 material slots): Office desk×16, Shelves×16, Wall×14, DoorHead×12, RoomLight×8, Store shelves×8, Furniture_Basin×6, Door×4, Filing cabinet×4, Bookcase×2, …; lights: RoomLight×4, Office desk×1
- `09_autoshop_2363563990.png`: pieces 15: Door×2, Floors×2, RoomLight×2, Car×1, Colliders×1, Filing cabinet×1, Furniture×1, Jerry can×1, Light switch plate×1, Shelves×1, Stack of tyres×1, Tool bench×1; vertices 12,253: Door×3736, Car×1768, Filing cabinet×1752, Tool bench×1528, Floors×1211, Shelves×776, Furniture×450, Stack of tyres×400, Jerry can×264, RoomLight×224, …; renderers 28 (62 material slots): Filing cabinet×5, Door×4, RoomLight×4, Tool bench×4, Car×3, Floors×2, Light switch plate×2, Furniture×1, Jerry can×1, Shelves×1, …; lights: RoomLight×2
- `10_salon_2363563974.png`: pieces 58: Shelves×17, Wall×8, DoorHead×4, Office chair×4, RoomLight×4, Waiting chair×3, salon counter×3, Door×2, Floors×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Back Office×1, …; vertices 49,310: Shelves×11684, DoorHead×8656, Furniture Salon×5928, Furniture×4832, Furniture_Basin×2582, Furniture Back Office×2496, Wall×2392, Office desk×2266, Floors×2162, Door×2156, …; renderers 87 (225 material slots): Shelves×17, Wall×13, DoorHead×12, RoomLight×8, Office desk×7, Furniture_Basin×6, Medicine cabinet×5, Door×4, salon counter×3, Filing cabinet×2, …; lights: RoomLight×4
- `11_restaurant_2622504478.png`: pieces 103: Table×17, Stool×16, Booth×14, Wall×10, stall×6, DoorHead×5, Fridge×4, Furniture_Basin×4, RoomLight×4, Stove×4, Furniture Dining Room×3, Back counter×2, Checkout counter×2, Door×2, …; vertices 186,334: Furniture Dining Room×111410, Stove×20476, DoorHead×10820, Fridge×10458, Furniture_Basin×10328, Wall×5608, Back counter×4434, Checkout counter×2160, Door×2156, Furniture×2130, …; renderers 225 (401 material slots): Stove×56, Back counter×33, Wall×26, Furniture_Basin×24, Fridge×20, Steel counter×16, DoorHead×15, RoomLight×8, Vending machine×6, stall×6, …; lights: Fridge×4, RoomLight×4, Stove×4
- `12_bar_2363563996.png`: pieces 33: Stool×8, Table×4, Wall×4, RoomLight×3, Door×2, DoorHead×2, Floors×2, stall×2, Colliders×1, Crate×1, Furniture×1, Furniture Bar×1, Light switch plate×1, Shelves×1; vertices 27,012: Furniture Bar×14232, DoorHead×4328, Furniture×2289, Door×2156, Floors×1415, Wall×1000, Shelves×632, Crate×432, RoomLight×336, Light switch plate×144, …; renderers 32 (75 material slots): DoorHead×6, RoomLight×6, Wall×5, Door×4, Crate×2, Floors×2, Light switch plate×2, stall×2, Furniture×1, Furniture Bar×1, …; lights: RoomLight×3
- `13_office_2622504446.png`: pieces 119: Wall×23, Filing cabinet×12, Chair×11, DoorHead×11, Office chair×10, Bookcase×6, Office desk×6, RoomLight×6, Furniture Office×5, Desk×4, Furniture_Chair×4, Door×2, Floors×2, Records shelf×2, …; vertices 119,943: DoorHead×23804, Office desk×14985, Furniture Office×10704, Desk×9583, Filing cabinet×8842, Office chair×8800, Wall×8224, Furniture×6010, Stove×5119, Floors×3282, …; renderers 260 (631 material slots): Office desk×46, Wall×42, DoorHead×33, Desk×28, Filing cabinet×22, Stove×14, RoomLight×12, Office chair×10, Bookcase×6, Chair×6, …; lights: RoomLight×6, Office desk×2, Furniture_Fridge×1, Stove×1
- `14_bank_2622504490.png`: pieces 90: Wall×14, Chair×12, DoorHead×7, Filing cabinet×7, Desk×5, Safe×5, Furniture_Chair×4, RoomLight×4, Bookcase×3, Furniture Office×3, Office chair×3, Office desk×3, Door×2, Floors×2, …; vertices 106,380: Furniture Banking Hall×25644, DoorHead×15148, Office desk×8404, Furniture Office×6720, Filing cabinet×5830, Wall×5438, Furniture_Basin×5164, Stove×5119, Furniture×4744, Door×3736, …; renderers 175 (415 material slots): Office desk×25, Wall×24, DoorHead×21, Filing cabinet×14, Stove×14, Furniture_Basin×12, Safe×10, RoomLight×8, Furniture_Fridge×5, Desk×4, …; lights: RoomLight×4, Office desk×2, Furniture_Fridge×1, Stove×1
- `15_civic_2622504470.png`: pieces 122: Wall×21, Chairs×20, Chair×14, Filing cabinet×11, DoorHead×10, Office chair×6, Bookcase×5, Furniture Office×5, Office desk×5, RoomLight×5, Table×3, Door×2, Floors×2, Furniture Meeting Room×2, …; vertices 117,030: DoorHead×21640, Furniture Meeting Room×15008, Office desk×12534, Furniture Office×11328, Filing cabinet×8000, Wall×7880, Floors×5311, Office chair×5280, Stove×5119, Furniture×3948, …; renderers 203 (500 material slots): Office desk×39, Wall×39, DoorHead×30, Filing cabinet×21, Stove×14, RoomLight×10, Front desk×7, Office chair×6, Bookcase×5, Furniture Office×5, …; lights: RoomLight×5, Office desk×2, Furniture_Fridge×1, Stove×1
- `16_clinic_2363564004.png`: pieces 150: Wall×26, DoorHead×12, Furniture Exam Room×12, Filing cabinet×11, Office chair×10, Waiting-room chair×10, Medicine cabinet×7, Counter×6, Desk×6, Examination couch×6, RoomLight×6, Stool×6, Shelves×5, Bookcase×4, …; vertices 155,336: Furniture Exam Room×43280, DoorHead×25968, Office desk×10473, Furniture Office×9744, Counter×8840, Office chair×8800, Filing cabinet×8454, Wall×8296, Medicine cabinet×4860, Floors×4350, …; renderers 263 (673 material slots): Wall×44, Counter×36, DoorHead×36, Office desk×32, Medicine cabinet×23, Filing cabinet×19, Furniture Exam Room×12, RoomLight×12, Office chair×10, Examination couch×6, …; lights: RoomLight×6, Office desk×2, Medical fridge×1
- `17_house_2238555434.png`: pieces 70: Wall×16, DoorHead×7, Furniture_Dresser×5, RoomLight×5, Bed×4, Furniture_Chair×4, Wardrobe×4, Door×2, Floors×2, Fridge×2, Furniture Living Room×2, Furniture_Counter×2, Bath×1, Coffee table×1, …; vertices 83,155: DoorHead×15148, Wall×10327, Wardrobe×6504, Furniture_Dresser×6138, Furniture_Counter×5574, Stove×5119, Fridge×4860, Furniture Living Room×4596, Furniture×4330, Door×3736, …; renderers 196 (421 material slots): Wall×41, DoorHead×21, Furniture_Dresser×20, Stove×14, Bed×12, Wardrobe×12, Fridge×10, Furniture_Counter×10, RoomLight×10, Bath×7, …; lights: RoomLight×5, Fridge×2, Furniture_FloorLamp×1, Furniture_Fridge×1, Furniture_Nightstand×1, Stove×1, TV×1
- `18_apartments_2229833810.png`: pieces 292: Wall×62, DoorHead×33, Furniture Living Room×20, Furniture_Fridge×12, Sofa×12, Stove×12, Bath×11, Bed×11, Coffee table×11, Furniture_Armchair×11, Furniture_Basin×11, Furniture_Bookshelf×11, Furniture_Dresser×11, Wardrobe×11, …; vertices 451,539: DoorHead×71412, Stove×61428, Furniture Living Room×58866, Wall×38921, Furniture×34770, Furniture_Fridge×30692, Furniture_Basin×28402, Furniture Common Room×28356, Wardrobe×17764, Bath×16818, …; renderers 850 (1875 material slots): Stove×168, Wall×150, DoorHead×99, Bath×77, Furniture_Basin×66, Furniture_Fridge×60, Furniture_Dresser×44, Bed×33, Wardrobe×33, TV×24, …; lights: Furniture_Fridge×12, Stove×12, TV×8, RoomLight×5, Furniture_Nightstand×2
- `19_library_1474017444.png`: pieces 94: Stacks×18, Filing cabinet×16, Wall×12, DoorHead×6, Shelves×6, Shelving×6, Bookcase×4, Reading table×4, RoomLight×4, Door×2, Floors×2, Furniture Office×2, Furniture_Basin×2, Office chair×2, …; vertices 96,906: Furniture Library×19280, DoorHead×12984, Filing cabinet×12624, Stacks×8640, Office desk×5588, Furniture×5211, Furniture_Basin×5164, Shelves×4288, Furniture Office×4080, Floors×3784, …; renderers 146 (339 material slots): Filing cabinet×26, DoorHead×18, Stacks×18, Wall×17, Office desk×16, Furniture_Basin×12, RoomLight×8, Shelves×6, Shelving×6, Bookcase×4, …; lights: RoomLight×4, Office desk×1
- `20_police_2229427062.png`: pieces 158: Wall×30, Filing cabinet×21, Chair×16, DoorHead×14, Office chair×9, RoomLight×7, Desk×6, Records shelf×6, Bookcase×5, Cell bars×5, Furniture Chief's Office×5, Office desk×5, Furniture_Chair×4, Ammunition crate×2, …; vertices 158,138: DoorHead×30296, Filing cabinet×17328, Desk×12905, Office desk×12596, Furniture Chief's Office×11232, Wall×10282, Furniture×9024, Office chair×7920, Furniture_Basin×5164, Stove×5119, …; renderers 318 (750 material slots): Wall×49, DoorHead×42, Desk×40, Office desk×39, Filing cabinet×38, RoomLight×14, Stove×14, Furniture_Basin×12, Office chair×9, Chair×6, …; lights: RoomLight×7, Office desk×2, Furniture_Fridge×1, Stove×1
- `21_firestation_2229427060.png`: pieces 50: Wall×6, Bunk×4, Footlocker×4, Locker×4, RoomLight×4, DoorHead×3, Table×3, Door×2, Floors×2, Furniture Bunk Room×2, Gear locker×2, Bookcase×1, Colliders×1, Filing cabinet×1, …; vertices 60,780: Furniture Day Room×20532, DoorHead×6492, Locker×5487, Door×3736, Gear locker×3411, Wall×2894, Office desk×2828, Floors×2767, Furniture Office×2352, Bunk×2088, …; renderers 91 (203 material slots): Wall×14, Locker×13, DoorHead×9, Office desk×9, Gear locker×8, RoomLight×8, Bunk×4, Door×4, Tool bench×4, TV×3, …; lights: RoomLight×4, Office desk×1, TV×1
- `22_church_2229567554.png`: pieces 82: Pew×35, Wall×10, Table×8, DoorHead×5, RoomLight×4, Pantry shelves×3, Door×2, Floors×2, Furniture Fellowship Hall×2, Bookcase×1, Chair×1, Colliders×1, Desk×1, Filing cabinet×1, …; vertices 92,098: Furniture Fellowship Hall×39742, Furniture Sanctuary×17232, DoorHead×10820, Door×3736, Floors×3524, Desk×2774, Wall×2772, Furniture Pastor's Study×2352, Pantry shelves×1872, Wardrobe×1838, …; renderers 70 (173 material slots): DoorHead×15, Wall×14, Desk×9, RoomLight×8, Door×4, Pantry shelves×3, Wardrobe×3, Filing cabinet×2, Floors×2, Furniture Fellowship Hall×2, …; lights: RoomLight×4, Desk×1
- `23_barn_2229549568.png`: pieces 28: Wall×6, Shelves×5, Crate×4, RoomLight×4, DoorHead×3, Door×2, Floors×2, Colliders×1, Furniture×1; vertices 22,129: DoorHead×6348, Furniture×5535, Door×3736, Shelves×3276, Crate×1776, Wall×640, RoomLight×448, Floors×370, Colliders×0; renderers 46 (99 material slots): DoorHead×9, Wall×9, Crate×8, RoomLight×8, Shelves×5, Door×4, Floors×2, Furniture×1; lights: RoomLight×4
- `24_garage_2238550564.png`: pieces 10: Floors×2, Colliders×1, Door×1, Furniture×1, Jerry can×1, Light switch plate×1, RoomLight×1, Stack of tyres×1, Tool bench×1; vertices 4,864: Door×1868, Tool bench×1580, Furniture×426, Floors×270, Jerry can×264, Stack of tyres×200, Light switch plate×144, RoomLight×112, Colliders×0; renderers 15 (32 material slots): Tool bench×4, Door×2, Floors×2, Light switch plate×2, RoomLight×2, Furniture×1, Jerry can×1, Stack of tyres×1; lights: RoomLight×1
- `25_school_1729270046.png`: pieces 95: Wall×17, Chair×14, Furniture Classroom×10, DoorHead×8, Bookcase×7, RoomLight×5, Teacher's desk×5, Desk×4, Filing cabinet×3, Table×3, Door×2, Floors×2, Office chair×2, Office desk×2, …; vertices 164,557: Furniture Classroom×42999, Furniture×38092, Furniture Break Room×17474, DoorHead×17312, Wall×11012, Floors×7534, Office desk×6188, Teacher's desk×4196, Door×3736, Bookcase×3220, …; renderers 171 (446 material slots): Wall×50, DoorHead×24, Teacher's desk×20, Office desk×18, Furniture Classroom×10, RoomLight×10, Bookcase×7, Filing cabinet×5, Door×4, Locker×4, …; lights: RoomLight×5, Office desk×2, TV×1
- `26_warehouse_2638698918.png`: pieces 34: Racking×10, RoomLight×4, Wall×4, Door×2, DoorHead×2, Floors×2, Bookcase×1, Chair×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, Furniture_Basin×1, Office chair×1, …; vertices 33,178: Racking×7040, Furniture×4728, DoorHead×4304, Door×3736, Office desk×2848, Furniture_Basin×2582, Furniture Office×2400, Floors×1775, Wall×1052, Office chair×880, …; renderers 60 (143 material slots): Racking×10, Office desk×9, RoomLight×8, Wall×7, DoorHead×6, Furniture_Basin×6, Door×4, Filing cabinet×2, Floors×2, Bookcase×1, …; lights: RoomLight×4, Office desk×1
- `27_workshop_2638698932.png`: pieces 39: Wall×9, DoorHead×4, RoomLight×4, Tool bench×4, Shelves×3, Crate×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, …; vertices 41,825: DoorHead×8536, Tool bench×6516, Furniture_Basin×5164, Door×3736, Furniture×3647, Office desk×2838, Furniture Office×2448, Floors×2196, Shelves×2180, Wall×1312, …; renderers 88 (197 material slots): Tool bench×16, DoorHead×12, Furniture_Basin×12, Wall×12, Office desk×9, RoomLight×8, Crate×4, Door×4, Shelves×3, Filing cabinet×2, …; lights: RoomLight×4, Office desk×1
- `28_postoffice_2589948132.png`: pieces 96: Sorting case×25, Wall×12, Chair×11, Crate×6, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Lockers×2, Shelves×2, Bookcase×1, …; vertices 113,872: Sorting case×28728, Furniture×14876, DoorHead×12984, Furniture Sorting Room×5628, Furniture_Basin×5164, Stove×5119, Wall×4392, Floors×4252, post boxes×3864, Door×3736, …; renderers 153 (322 material slots): Sorting case×25, Wall×21, DoorHead×18, Stove×14, Furniture_Basin×12, Office desk×9, RoomLight×8, Crate×6, Furniture_Fridge×5, Door×4, …; lights: RoomLight×4, Furniture_Fridge×1, Office desk×1, Stove×1
- `29_funeral_1807972230.png`: pieces 68: Chairs×28, Wall×6, RoomLight×4, Shelves×4, DoorHead×3, Filing cabinet×3, stall×3, Desk×2, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Bookcase×1, Colliders×1, …; vertices 55,325: Furniture Chapel×10368, Furniture Stock Room×7802, DoorHead×6492, Furniture_Basin×5164, Furniture×4492, Door×3736, Floors×2922, Office desk×2876, Shelves×2680, Furniture Office×2160, …; renderers 77 (182 material slots): Furniture_Basin×12, Wall×11, DoorHead×9, Office desk×9, RoomLight×8, Desk×4, Door×4, Filing cabinet×4, Shelves×4, stall×3, …; lights: RoomLight×4, Office desk×1

</details>

## Streamed: the statewide map's lazy interiors

Piece 11913_9092 of Bardstown (Full) built in 71 ms: 0 interiors in it as built (none, as it should be), 8 buildings' plans left with the streamer; the plans took 12.2 ms over 5 steps (slowest 3.0 ms).

Furnished round a survivor at the piece's middle: 6 buildings in 105 frames (1.8 s at 60 fps), 6 standing, 0 failed; the slowest frame's furnishing took 8.4 ms (budget 3 ms, plus the step that crosses it). 6 interiors furnished (0 failed, 0 taken down), 6 standing, 8 known.

Loot memory: building 1915619802 (Motel, 18 containers): the first carried off, the second emptied; walked 2 km away (taken down) and back (furnished again): 18 containers, the carried-off one gone, the emptied one still empty, 16 of 16 others as they were. **OK**.

Determinism: the piece built again and building 1915619802 furnished again: identical (objects, names, places, meshes). **OK**.

Plan steps in downtown Louisville's pieces (a step runs until the site's slice is spent, then finishes the plan or the wall under way):

| piece | plans ms in all | steps | slowest step ms | slowest single piece of work |
|---|--:|--:|--:|---|
| 11711_9477 | 19.6 | 6 | 6.2 | 5.0 ms: the plan of Civic 662565482 (6244 m², 43 rooms, 28 corners, 121 windows) |
| 11712_9477 | 8.8 | 3 | 4.0 | 3.8 ms: the plan of Bank 662565810 (1425 m², 8 rooms, 8 corners, 22 windows) |
| 11713_9477 | 5.6 | 2 | 4.5 | 4.5 ms: the plan of Bank 9295913 (4278 m², 9 rooms, 9 corners, 61 windows) |
| 11711_9478 | 18.6 | 4 | 9.2 | 8.2 ms: the plan of Civic 662565944 (1947 m², 25 rooms, 31 corners, 35 windows) |
| 11712_9478 | 28.0 | 7 | 7.6 | 7.5 ms: the plan of Workshop 662565648 (1600 m², 5 rooms, 4 corners, 20 windows) |
| 11713_9478 | 29.2 | 9 | 7.7 | 7.5 ms: the plan of Warehouse 662566094 (4917 m², 6 rooms, 10 corners, 49 windows) |
| 11711_9479 | 8.1 | 3 | 5.2 | 3.8 ms: the plan of Motel 1291039792 (1680 m², 44 rooms, 6 corners, 38 windows) |
| 11712_9479 | 13.7 | 6 | 3.2 | 2.6 ms: the plan of Warehouse 662564942 (621 m², 7 rooms, 20 corners, 28 windows) |
| 11713_9479 | 18.3 | 5 | 7.2 | 7.1 ms: the plan of Warehouse 662565798 (4744 m², 6 rooms, 15 corners, 52 windows) |

Slowest plan step in these pieces: 9.2 ms.


## Log

0 errors, 0 warnings logged during the run.

## Stages

- setup: ok, 30 ms
- square: Bardstown: ok, 12,481 ms
- discard: Bardstown: ok, 210 ms
- square: Princeton: ok, 6,234 ms
- discard: Princeton: ok, 207 ms
- square: Pikeville: ok, 4,259 ms
- discard: Pikeville: ok, 151 ms
- square: Middlesboro: ok, 1,928 ms
- discard: Middlesboro: ok, 70 ms
- summary by use: ok, 15 ms
- streamed pieces: ok, 1,605 ms

**Exit 0** after 27.2 s.
