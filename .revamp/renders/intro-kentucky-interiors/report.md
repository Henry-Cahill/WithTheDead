# CLONE-Z interiors harness

Build ky-local-f2. Unity 6000.7.0a2, Direct3D11. Places: Bardstown, Princeton, Pikeville, Middlesboro. Edit mode: nothing wakes, so the live kitchen sink is drawn plain and the ceiling fittings show their bare bulbs, as the isometric view has them. Map data © OpenStreetMap contributors (ODbL).

## Bardstown

KentuckyMap.Build 5,763 ms. Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 804 containers, 949 lights, 335 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 116 buildings furnished (697 rooms), 0 unplannable, 0 failed, 698 containers, 927 lights, 3998 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 29,628 | 27,365 |
| loot containers | 804 | 698 (2,231 more stripped of their storage) |
| lights | 965 | 927 (38 rooms left lit) |
| vertices | 6,010,792 | 4,303,388 |
| renderers (material slots) | | 13,834 (29,206; 1,935 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 3,316 ms in all, slowest step 98.7 ms |
| buildings furnished | | 116 (0 failed) |

<details><summary>Every building (116)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2363563990 | AutoShop | commercial | commercial | 254 | 14 | 0.95 | Bays | 3 (3) | 2 | 58 | 11.2 (9.7 interior rooms) |
| 2622504490 | Bank | bank | commercial | 584 | 14 | 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 353 | 43.5 (8.2 interior rooms) |
| 2239973254 | Bank | bank | commercial / bank | 202 | 13 | 0.99 | Lobby, Office, Vault | 4 (4) | 4 | 145 | 17.8 (5.2 interior rooms) |
| 2363563980 | Bar | bar | commercial | 460 | 14 | 1.00 | Bar, CommercialKitchen, Restroom×2 | 6 (11) | 12 lit 1 | 228 | 41.3 (19.6 interior floors) |
| 2363563996 | Bar | bar | house | 209 | 13 | 0.99 | Bar, Restroom, Stock | 5 (6) | 3 lit 1 | 99 | 7.7 (2.6 interior rooms) |
| 2622506322 | Barn | commercial | commercial | 369 | 14 | 0.58 | Barn×3, Stock×2 | 6 (7) | 4 | 122 | 9.1 (2.2 interior rooms) |
| 2622506284 | Barn | commercial | commercial | 261 | 14 | 0.68 | Barn×2, Stock×2 | 5 (5) | 4 | 111 | 17.9 (7.3 interior doors) |
| 2622506324 | Barn | commercial | commercial | 254 | 14 | 0.98 | Barn×2, Stock×2 | 5 (6) | 4 | 113 | 9.1 (2.2 interior rooms) |
| 2239973246 | BigStore | grocery | commercial | 460 | 14 | 0.96 | BackOffice, Restroom×2, Sales, Stock | 9 (11) | 5 lit 1 | 220 | 19.7 (5.6 interior rooms) |
| 2363563962 | BigStore | grocery | commercial / shop:pastry | 432 | 13 | 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 | 26.4 (4.9 interior rooms) |
| 2239973256 | Church | church | church / place_of_worship | 1,231 | 14 | 0.84 | Meeting×4, Nave, Sacristy | 5 (5) | 5 lit 1 | 443 | 38.1 (10.2 interior rooms) |
| 2239973252 | Church | church | church / place_of_worship | 362 | 14 | 0.76 | Meeting×2, Nave, Sacristy×2 | 4 (4) | 6 | 287 | 27.2 (8.2 interior rooms) |
| 2622504470 | Civic | courthouse | commercial | 554 | 14 | 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 | 42.5 (4.7 interior rooms) |
| 2363564004 | Clinic | clinic | commercial | 543 | 14 | 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 | 63.8 (7.9 interior rooms) |
| 124092542 | Convenience | convenience | commercial / shop:convenience | 464 | 14 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 10 (11) | 5 | 229 | 59.6 (20.6 interior rooms) |
| 2622504504 | Convenience | convenience | commercial | 304 | 14 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 | 16.6 (3.2 interior rooms) |
| 2648437042 | Cottage | home | house | 66 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 115 | 19.6 (8.1 interior doors) |
| 2622506312 | Cottage | outbuilding | house | 57 | 13 | 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 | 7.7 (2.6 interior rooms) |
| 2622504458 | Garage | outbuilding | house | 38 | 14 | 0.99 | HomeGarage | 1 (1) | 1 | 34 | 2.4 (1.4 interior rooms) |
| 2648437046 | Garage | outbuilding | house | 33 | 15 | 0.97 | HomeGarage | 2 (2) | 1 | 43 | 2.8 (1.9 interior rooms) |
| 2622504448 | Hardware | hardware | commercial | 587 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 12 (13) | 4 lit 1 | 205 | 14.4 (6.1 interior rooms) |
| 2622505338 | Hardware | commercial | commercial | 336 | 14 | 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 203 | 18.3 (3.2 interior rooms) |
| 2622504452 | House | home | house | 246 | 14 | 0.95 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 5 (5) | 11 | 240 | 23.8 (6.5 interior rooms) |
| 2363563982 | House | home | house | 245 | 15 | 0.99 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 12 | 315 | 40.2 (12.1 interior rooms) |
| 2622504486 | House | home | house | 243 | 14 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 275 | 26.2 (3.8 interior rooms) |
| 2622505348 | House | home | house | 237 | 14 | 0.87 | Bathroom, Bedroom×3, Dining, Hall, HomeGarage, Kitchen, Living | 5 (5) | 11 lit 2 | 301 | 28.8 (4.7 interior rooms) |
| 2622506314 | House | home | house | 237 | 13 | 0.95 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 12 | 319 | 33.3 (4.0 interior rooms) |
| 2622504494 | House | home | house | 227 | 15 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (5) | 11 | 217 | 21.5 (6.7 interior rooms) |
| 2648411562 | House | home | house | 222 | 13 | 0.95 | Bathroom, Bedroom×2, Closet, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 248 | 22.5 (4.9 interior rooms) |
| 2622506286 | House | home | house | 221 | 14 | 0.82 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 5 (5) | 11 | 288 | 28.2 (5.6 interior rooms) |
| 2622505344 | House | home | house | 221 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 272 | 25.0 (3.6 interior rooms) |
| 2622504514 | House | home | house | 219 | 15 | 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 329 | 42.7 (6.3 interior rooms) |
| 2363563976 | House | home | house | 219 | 14 | 0.99 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 5 (5) | 11 | 254 | 39.8 (14.0 interior rooms) |
| 2648411526 | House | home | house | 218 | 13 | 0.90 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 5 (5) | 11 lit 1 | 287 | 35.8 (9.0 interior rooms) |
| 2363564000 | House | home | house | 214 | 13 | 0.84 | Bathroom, Bedroom×2, Kitchen, Living | 5 (5) | 11 | 236 | 28.2 (7.9 interior rooms) |
| 2622505346 | House | home | house | 214 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 5 (5) | 11 | 255 | 60.6 (17.3 interior rooms) |
| 2622506276 | House | home | house | 208 | 13 | 0.78 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 12 | 314 | 28.8 (3.6 interior rooms) |
| 2622506288 | House | home | house | 206 | 14 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 5 (5) | 12 lit 1 | 257 | 27.1 (4.9 interior rooms) |
| 2239973242 | House | home | house | 206 | 14 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 241 | 34.9 (12.7 interior rooms) |
| 2622506282 | House | home | house | 204 | 14 | 0.68 | Bathroom, Bedroom×2, Den, Hall, Kitchen, Living | 5 (5) | 11 | 263 | 29.8 (6.6 interior rooms) |
| 2622504468 | House | home | house | 203 | 12 | 0.78 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 4 (4) | 12 lit 1 | 237 | 23.1 (5.5 interior rooms) |
| 2622504456 | House | home | house | 202 | 14 | 0.68 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Laundry, Living | 4 (4) | 11 | 286 | 28.4 (6.2 interior rooms) |
| 2622504454 | House | home | house | 199 | 15 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 5 (5) | 12 lit 1 | 263 | 33.7 (6.1 interior rooms) |
| 2622504484 | House | home | house | 196 | 14 | 1.00 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 310 | 37.0 (7.3 interior walls) |
| 2648437058 | House | home | house | 194 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 282 | 40.0 (5.6 interior merge) |
| 2622506316 | House | home | house | 194 | 13 | 0.96 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 | 220 | 32.9 (11.0 interior rooms) |
| 2622504496 | House | home | house | 186 | 15 | 0.82 | Bathroom, Bedroom, Kitchen, Laundry, Living | 4 (4) | 9 | 230 | 23.1 (6.7 interior rooms) |
| 2622506266 | House | home | house | 185 | 14 | 0.78 | Bathroom, Bedroom×3, Hall, Kitchen, Laundry, Living | 4 (4) | 11 | 276 | 30.2 (6.4 interior rooms) |
| 2648437044 | House | home | house | 182 | 14 | 1.00 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 232 | 23.1 (5.3 interior rooms) |
| 2622504444 | House | home | house | 174 | 13 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 4 (4) | 11 | 272 | 26.2 (3.0 interior rooms) |
| 2648411528 | House | home | house | 174 | 13 | 0.92 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Living | 4 (4) | 11 | 251 | 26.2 (6.7 interior rooms) |
| 2647449226 | House | home | house | 171 | 15 | 0.87 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 4 (4) | 12 lit 1 | 250 | 35.2 (6.1 interior rooms) |
| 2622504462 | House | home | house | 169 | 14 | 0.89 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 4 (4) | 11 | 267 | 25.7 (6.8 interior rooms) |
| 2648411530 | House | home | house | 164 | 13 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 238 | 25.1 (7.3 interior rooms) |
| 2622506326 | House | home | house | 161 | 13 | 0.78 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 4 (4) | 11 lit 1 | 286 | 30.3 (6.9 interior rooms) |
| 2622505340 | House | home | house | 158 | 14 | 0.89 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 4 (4) | 12 | 229 | 21.0 (5.0 interior rooms) |
| 2622504464 | House | home | house | 154 | 14 | 1.00 | Bathroom, Bedroom×3, Hall, Kitchen, Living | 4 (4) | 11 lit 2 | 248 | 31.7 (4.7 interior rooms) |
| 2648437050 | House | home | house | 152 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 222 | 20.8 (4.7 interior rooms) |
| 2622506246 | House | home | house | 148 | 14 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 4 (4) | 11 | 249 | 24.0 (6.5 interior rooms) |
| 2622505358 | House | home | house | 141 | 13 | 0.94 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 218 | 20.1 (4.3 interior rooms) |
| 2648437056 | House | home | house | 137 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 | 221 | 23.0 (6.9 interior rooms) |
| 2648411540 | House | home | house | 131 | 14 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 224 | 19.9 (4.6 interior rooms) |
| 2622504488 | House | home | house | 127 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 238 | 22.7 (5.7 interior rooms) |
| 2622505342 | House | home | house | 126 | 14 | 0.86 | Bathroom, Bedroom×2, Kitchen, Living | 4 (4) | 11 lit 1 | 229 | 22.8 (6.3 interior rooms) |
| 2622504450 | House | home | house | 125 | 14 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 | 205 | 20.5 (5.5 interior rooms) |
| 2648411532 | House | home | house | 124 | 14 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 244 | 24.1 (6.5 interior rooms) |
| 2622506250 | House | home | house | 120 | 13 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 4 (4) | 11 lit 1 | 227 | 21.2 (6.4 interior rooms) |
| 2622504480 | House | home | house | 96 | 14 | 0.88 | Bathroom, Bedroom, Living | 4 (4) | 5 | 124 | 11.1 (2.4 interior rooms) |
| 2648411536 | House | home | house | 96 | 14 | 0.96 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 213 | 68.7 (21.6 interior rooms) |
| 2648411534 | House | home | house | 89 | 13 | 0.98 | Bathroom, Hall, Kitchen, Living | 4 (4) | 9 lit 2 | 188 | 36.7 (7.7 interior rooms) |
| 2622506320 | House | home | house | 88 | 13 | 1.00 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 9 lit 1 | 184 | 17.4 (4.4 interior rooms) |
| 2622506318 | House | home | house | 85 | 12 | 0.87 | Bathroom, Bedroom, Den, Kitchen, Living | 2 (4) | 9 | 186 | 17.0 (2.7 interior rooms) |
| 2648411564 | House | home | house | 77 | 14 | 0.92 | Bathroom, Bedroom, Living | 3 (3) | 4 lit 1 | 116 | 11.0 (2.7 interior rooms) |
| 2648411538 | House | home | house | 77 | 14 | 0.98 | Bathroom, Bedroom, Living | 4 (4) | 5 | 124 | 11.2 (2.6 interior rooms) |
| 2648437054 | House | home | house | 76 | 13 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 10 | 175 | 15.9 (4.1 interior rooms) |
| 2648437040 | House | home | house | 74 | 13 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 9 lit 1 | 174 | 21.1 (4.9 interior rooms) |
| 1915619802 | Motel | hotel | commercial | 823 | 15 | 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 582 | 84.1 (22.7 interior rooms) |
| 1843815690 | Motel | hotel | commercial | 515 | 13 | 0.98 | Bathroom×8, Guest×8, Hall, Lobby | 13 (13) | 10 | 525 | 109.6 (33.0 interior floors) |
| 2363563966 | Office | office | commercial | 669 | 13 | 0.93 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock×2 | 14 (14) | 11 | 650 | 76.8 (10.4 interior rooms) |
| 2622504446 | Office | commercial | commercial | 388 | 14 | 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 463 | 50.7 (6.7 interior rooms) |
| 2622504502 | Office | commercial | commercial | 380 | 15 | 0.98 | Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (9) | 6 lit 1 | 389 | 35.9 (15.3 interior rooms) |
| 2363563984 | Office | commercial | commercial | 286 | 15 | 0.99 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 7 (8) | 9 | 396 | 60.9 (8.2 interior rooms) |
| 281586026 | Pharmacy | pharmacy | commercial / pharmacy | 994 | 15 | 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 | 47.6 (12.8 interior rooms) |
| 2363563978 | Pharmacy | pharmacy | commercial | 607 | 12 | 1.00 | BackOffice, Dispensary, Restroom, Sales | 13 (14) | 5 | 245 | 28.7 (16.4 interior rooms) |
| 2363563964 | Restaurant | restaurant | commercial / restaurant | 629 | 13 | 0.99 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 10 | 391 | 39.2 (20.2 interior rooms) |
| 2622504500 | Restaurant | restaurant | commercial | 513 | 14 | 0.98 | CommercialKitchen, Eatery, Restroom×2 | 9 (11) | 11 | 455 | 43.6 (21.6 interior rooms) |
| 2622504478 | Restaurant | restaurant | commercial | 379 | 14 | 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12 lit 1 | 514 | 43.9 (12.5 interior rooms) |
| 2363563998 | Restaurant | restaurant | house | 125 | 14 | 0.99 | CommercialKitchen, Eatery, Restroom | 3 (6) | 10 | 220 | 26.8 (11.6 interior rooms) |
| 2363564002 | Salon | salon | commercial | 843 | 14 | 0.97 | BackOffice, Restroom×2, Sales, Stock | 6 (11) | 4 | 185 | 14.4 (2.7 interior rooms) |
| 2363563974 | Salon | salon | commercial | 511 | 12 | 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4 lit 2 | 188 | 17.4 (3.8 interior rooms) |
| 2363563986 | Salon | salon | house | 216 | 15 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (6) | 5 | 149 | 13.0 (2.9 interior rooms) |
| 2648437048 | Shed | shed | house | 25 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 2.9 (2.0 interior rooms) |
| 2648437052 | Shed | shed | house | 18 | 15 | 0.98 | Shed | 1 (1) | 1 | 32 | 2.3 (1.3 interior rooms) |
| 2622504474 | Store | clothing | commercial | 727 | 14 | 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 | 31.3 (11.1 interior rooms) |
| 2363563992 | Store | clothing | commercial | 521 | 14 | 0.98 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 lit 2 | 196 | 17.4 (4.4 interior rooms) |
| 2622504442 | Store | laundry | commercial | 457 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 213 | 16.4 (4.8 interior rooms) |
| 2363563968 | Store | shop | commercial | 392 | 13 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 191 | 15.0 (4.5 interior rooms) |
| 2363563988 | Store | shop | commercial / shop:art | 328 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 8 (9) | 5 lit 1 | 176 | 13.9 (4.2 interior rooms) |
| 2363563994 | Store | shop | commercial | 267 | 13 | 0.97 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 127 | 9.2 (3.6 interior rooms) |
| 2648411568 | Store | commercial | commercial | 253 | 13 | 0.85 | BackOffice, Restroom, Sales, Stock×2 | 6 (7) | 5 | 194 | 16.1 (3.5 interior rooms) |
| 2622504498 | Store | commercial | commercial | 251 | 14 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 176 | 14.4 (4.1 interior rooms) |
| 2239973244 | Store | grocery | house | 134 | 13 | 0.97 | BackOffice, Restroom, Sales, Stock | 5 (6) | 6 | 135 | 18.6 (4.6 interior rooms) |
| 2239973248 | Warehouse | commercial | commercial | 871 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 175 | 15.1 (4.0 interior rooms) |
| 2622504482 | Warehouse | commercial | commercial | 793 | 14 | 0.86 | Office, Restroom, Warehouse, Workshop | 10 (11) | 5 | 161 | 16.9 (3.7 interior merge) |
| 2622504460 | Warehouse | commercial | commercial | 777 | 14 | 0.96 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 178 | 13.8 (3.7 interior rooms) |
| 124090468 | Warehouse | commercial | commercial | 742 | 15 | 0.88 | Office, Restroom, Stock×2, Warehouse×2, Workshop | 11 (12) | 5 | 208 | 236.7 (98.7 interior rooms) |
| 2622504472 | Warehouse | commercial | commercial | 308 | 14 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 4 | 125 | 10.7 (2.5 interior rooms) |
| 2622504466 | Warehouse | commercial | commercial | 266 | 13 | 0.69 | Office, Restroom, Stock×2, Warehouse, Workshop | 7 (8) | 5 lit 1 | 162 | 33.8 (10.6 interior rooms) |
| 2622504492 | Workshop | commercial | commercial | 822 | 14 | 0.46 | Office, Restroom, Stock, Workshop×4 | 14 (15) | 4 lit 1 | 211 | 18.2 (3.2 interior rooms) |
| 2622504476 | Workshop | commercial | commercial | 798 | 14 | 1.00 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 lit 1 | 167 | 14.3 (2.8 interior rooms) |
| 2239973240 | Workshop | commercial | commercial | 379 | 14 | 0.84 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 lit 1 | 166 | 20.8 (4.0 interior rooms) |
| 2647445510 | Workshop | commercial | commercial | 343 | 16 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 119 | 10.1 (2.8 interior rooms) |
| 2622506278 | Workshop | commercial | commercial | 329 | 14 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 4 | 151 | 13.5 (2.5 interior rooms) |
| 2622506280 | Workshop | commercial | commercial | 306 | 14 | 0.78 | Office, Restroom, Stock×2, Workshop×3 | 7 (8) | 5 | 192 | 16.8 (2.7 interior rooms) |
| 2622506264 | Workshop | commercial | commercial | 295 | 14 | 1.00 | Office, Restroom, Workshop | 5 (6) | 4 | 122 | 15.4 (6.0 interior rooms) |
| 2648411560 | Workshop | commercial | commercial | 283 | 13 | 0.75 | Office, Restroom, Workshop×2 | 7 (8) | 5 | 148 | 12.8 (2.9 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Dresser in Motel 1843815690): 404 of its triangles hidden in its room's furniture mesh, **OK**.

## Princeton

KentuckyMap.Build 3,612 ms. Kentucky map Princeton (build ky-local-f2): 102 buildings, 42 beyond the edge, 161 street pieces, 783 containers, 800 lights, 459 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 102 buildings furnished (630 rooms), 0 unplannable, 0 failed, 643 containers, 776 lights, 3050 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 28,066 | 25,735 |
| loot containers | 783 | 643 (2,133 more stripped of their storage) |
| lights | 812 | 776 (47 rooms left lit) |
| vertices | 5,797,012 | 4,135,795 |
| renderers (material slots) | | 12,610 (27,117; 1,788 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 2,839 ms in all, slowest step 23.0 ms |
| buildings furnished | | 102 (0 failed) |

<details><summary>Every building (102)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2229833810 | Apartments | apartments | apartments | 1,310 | 27 | 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1450 | 177.6 (13.5 interior rooms) |
| 2229822680 | AutoShop | commercial | commercial | 437 | 28 | 0.96 | Bays, Restroom, Sales | 5 (6) | 4 lit 1 | 115 | 9.3 (2.9 interior rooms) |
| 2229822692 | AutoShop | auto | commercial / shop:car_repair | 126 | 29 | 1.00 | Bays, Restroom, Sales | 4 (5) | 3 | 98 | 7.9 (3.2 interior rooms) |
| 2229447462 | Bank | bank | commercial | 1,169 | 29 | 0.80 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 11 (12) | 6 lit 2 | 341 | 31.0 (4.3 interior rooms) |
| 2229447460 | Bank | bank | commercial | 948 | 29 | 0.77 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 12 (13) | 6 | 328 | 41.5 (6.4 interior rooms) |
| 2229549564 | Bank | bank | commercial / bank | 368 | 29 | 0.70 | BreakRoom, Lobby, Office×4, Records, Restroom, Stock, Vault | 8 (8) | 7 | 350 | 37.4 (3.5 interior rooms) |
| 2229549568 | Barn | commercial | commercial | 272 | 29 | 0.86 | Barn, Stock×3 | 5 (5) | 4 | 110 | 8.6 (1.6 interior rooms) |
| 2230229656 | Barn | commercial | commercial | 250 | 28 | 1.00 | Barn, Stock×2 | 5 (5) | 4 lit 1 | 100 | 7.9 (2.6 interior rooms) |
| 2229533466 | Barn | commercial | commercial | 175 | 30 | 0.95 | Barn, Stock×2 | 4 (4) | 3 | 82 | 5.4 (1.4 interior doors) |
| 2229566908 | Church | church | church / place_of_worship | 999 | 29 | 0.75 | Meeting×4, Narthex, Nave, Sacristy | 6 (6) | 5 | 415 | 26.9 (7.7 interior rooms) |
| 2229567554 | Church | church | church | 663 | 28 | 0.77 | Meeting×4, Nave, Sacristy | 5 (5) | 5 | 394 | 37.5 (11.8 interior rooms) |
| 2229361630 | Civic | courthouse | commercial / courthouse | 1,016 | 30 | 0.81 | BreakRoom, Hall, Lobby, Meeting×3, Office×7, Records×2, Stock×3 | 15 (16) | 12 | 708 | 98.2 (13.6 interior rooms) |
| 2229432452 | Civic | civic | commercial | 785 | 31 | 0.75 | BreakRoom, Hall, Lobby, Meeting×2, Office×6, Records×2, Stock×3 | 15 (16) | 11 | 635 | 68.0 (12.7 interior rooms) |
| 1474017440 | Civic | community | commercial | 547 | 30 | 0.99 | BreakRoom, Meeting, Office, Restroom, Stock | 4 (5) | 7 | 425 | 58.1 (17.0 interior doors) |
| 2229519182 | Civic | courthouse | commercial / courthouse | 450 | 28 | 0.96 | BreakRoom, Hall, Lobby, Meeting, Office×3, Records | 9 (10) | 8 | 285 | 39.3 (10.1 interior rooms) |
| 2229822690 | Civic | community | commercial / social_centre | 381 | 29 | 0.94 | BreakRoom, Meeting, Office×2, Restroom×2, Stock | 5 (7) | 7 | 381 | 31.1 (6.7 interior rooms) |
| 2229553344 | Clinic | clinic | commercial | 705 | 29 | 0.82 | Dispensary, Exam×8, Hall, Office×3, Records, Waiting | 14 (15) | 10 lit 1 | 627 | 64.2 (9.0 interior walls) |
| 2229427060 | FireStation | fire_station | civic / fire_station | 401 | 27 | 0.96 | Bays, Bunks, DayRoom, Office | 8 (8) | 6 | 211 | 21.3 (5.6 interior rooms) |
| 2238550564 | Garage | outbuilding | house | 32 | 25 | 1.00 | HomeGarage | 1 (1) | 1 | 34 | 3.1 (1.6 interior rooms) |
| 2229546206 | Hardware | hardware | commercial / shop:hardware | 141 | 30 | 1.00 | Restroom, Sales, Stock | 5 (6) | 3 lit 2 | 110 | 8.7 (2.5 interior rooms) |
| 2230673820 | House | home | house | 368 | 26 | 0.99 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 6 (6) | 12 | 248 | 23.3 (3.7 interior rooms) |
| 2229833824 | House | home | house | 293 | 28 | 0.91 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 6 (6) | 12 | 282 | 27.9 (5.6 interior rooms) |
| 2238546558 | House | home | house | 261 | 28 | 0.75 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 5 (5) | 12 | 258 | 43.4 (7.6 interior merge) |
| 2229833820 | House | home | house | 255 | 27 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 5 (5) | 12 lit 2 | 263 | 36.8 (8.7 interior doors) |
| 2238546246 | House | home | house | 250 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 12 | 294 | 52.1 (10.0 interior merge) |
| 2238555434 | House | home | house | 248 | 30 | 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 5 (5) | 12 lit 1 | 322 | 43.7 (6.9 interior rooms) |
| 2238546252 | House | home | house | 234 | 29 | 0.90 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 327 | 32.9 (6.3 interior rooms) |
| 2238555432 | House | home | house | 233 | 31 | 0.72 | Bathroom, Bedroom×3, Hall, HomeGarage, Kitchen, Living | 5 (5) | 11 lit 1 | 290 | 50.8 (11.9 interior rooms) |
| 2238546556 | House | home | house | 231 | 27 | 1.00 | Bathroom, Bedroom×3, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 299 | 29.7 (5.4 interior rooms) |
| 2229549576 | House | home | house | 225 | 32 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 5 (5) | 11 | 245 | 26.6 (7.7 interior rooms) |
| 2229565810 | House | home | house | 214 | 28 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 5 (5) | 12 lit 1 | 256 | 32.9 (9.8 interior rooms) |
| 2238555430 | House | home | house | 214 | 27 | 0.85 | Bathroom, Bedroom×4, Hall, Kitchen, Living | 5 (5) | 11 lit 1 | 290 | 30.4 (6.3 interior rooms) |
| 2229833814 | House | home | house | 207 | 26 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 255 | 25.0 (6.7 interior rooms) |
| 2229549570 | House | home | house | 206 | 29 | 1.00 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Living | 5 (5) | 12 | 256 | 38.2 (6.2 interior rooms) |
| 2238554394 | House | home | house | 202 | 26 | 0.89 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 230 | 23.5 (5.9 interior rooms) |
| 2229567550 | House | home | house | 193 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 3 (4) | 11 | 212 | 20.7 (6.2 interior rooms) |
| 2229833812 | House | home | house | 180 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 4 (4) | 11 lit 2 | 232 | 21.7 (4.5 interior rooms) |
| 2229565802 | House | home | house | 171 | 26 | 1.00 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 227 | 20.2 (4.7 interior rooms) |
| 2238554392 | House | home | house | 168 | 28 | 0.79 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Laundry, Living | 3 (4) | 12 | 242 | 23.9 (4.4 interior rooms) |
| 2238546248 | House | home | house | 165 | 28 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 4 (4) | 12 | 230 | 22.6 (4.9 interior rooms) |
| 2238554396 | House | home | house | 161 | 32 | 0.73 | Bathroom, Bedroom, Hall, HomeGarage, Kitchen, Laundry, Living | 4 (4) | 11 | 244 | 29.9 (6.5 interior rooms) |
| 2238554390 | House | home | house | 158 | 29 | 0.90 | Bathroom, Bedroom, Dining, Hall, HomeGarage, Kitchen, Living | 4 (4) | 12 lit 1 | 232 | 41.2 (8.5 interior merge) |
| 2238550556 | House | home | house | 141 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 lit 2 | 244 | 25.9 (7.3 interior rooms) |
| 2238550566 | House | home | house | 132 | 26 | 0.82 | Bathroom, Bedroom×2, Hall, HomeGarage, Kitchen, Laundry, Living | 4 (4) | 11 lit 1 | 254 | 24.8 (4.8 interior rooms) |
| 2229822682 | House | home | house | 131 | 27 | 0.92 | Bathroom, Bedroom×2, Hall, Kitchen, Laundry, Living | 4 (4) | 11 | 245 | 24.6 (6.4 interior rooms) |
| 2229549574 | House | home | house | 125 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 4 (4) | 11 | 217 | 42.8 (19.0 interior rooms) |
| 2229549566 | House | home | house | 125 | 29 | 0.98 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 232 | 22.9 (6.4 interior rooms) |
| 2229833816 | House | home | house | 122 | 29 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 | 218 | 21.6 (6.7 interior rooms) |
| 2229549580 | House | home | house | 114 | 32 | 0.99 | Bathroom, Bedroom, Dining, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 215 | 21.2 (4.8 interior rooms) |
| 2229833822 | House | home | house | 102 | 28 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 10 | 198 | 20.3 (6.0 interior rooms) |
| 2238550562 | House | home | house | 101 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 9 lit 1 | 187 | 20.4 (4.7 interior rooms) |
| 2229833818 | House | home | house | 91 | 26 | 1.00 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 | 210 | 22.6 (5.5 interior rooms) |
| 2238550560 | House | home | house | 89 | 29 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 | 221 | 22.6 (5.5 interior rooms) |
| 2238550558 | House | home | house | 69 | 28 | 0.99 | Bathroom, Bedroom, Living | 4 (4) | 6 | 131 | 13.0 (3.4 interior rooms) |
| 1474017444 | Library | library | commercial / library | 613 | 27 | 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5 lit 1 | 304 | 29.4 (8.1 interior rooms) |
| 2229546212 | Library | library | commercial / library | 288 | 29 | 0.97 | Office×2, Records, Restroom, Stacks, Stock | 7 (8) | 5 | 234 | 34.7 (11.9 interior rooms) |
| 2229553356 | Office | commercial | commercial | 488 | 29 | 0.95 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom | 10 (11) | 10 | 630 | 75.3 (16.1 interior rooms) |
| 2229822684 | Office | office | commercial | 460 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records | 10 (11) | 9 lit 1 | 473 | 70.7 (20.7 interior rooms) |
| 2229533470 | Office | office | commercial | 365 | 28 | 0.97 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records | 9 (9) | 8 | 383 | 48.4 (12.7 interior rooms) |
| 2229546196 | Office | office | commercial | 276 | 30 | 0.96 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (8) | 6 | 311 | 42.1 (15.2 interior rooms) |
| 2229565806 | Office | office | commercial | 208 | 28 | 1.00 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom | 6 (7) | 6 lit 2 | 298 | 39.2 (6.0 interior rooms) |
| 2229996040 | Office | office | house | 202 | 28 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice, Records, Restroom | 6 (7) | 7 lit 1 | 251 | 24.1 (2.9 interior rooms) |
| 2229553350 | Office | office | commercial | 156 | 29 | 0.93 | Lobby, Office, Restroom | 3 (4) | 4 lit 1 | 113 | 8.9 (2.8 interior rooms) |
| 2229553352 | Office | office | commercial | 136 | 30 | 0.86 | Lobby, Office, Restroom | 2 (3) | 4 | 111 | 8.9 (2.7 interior rooms) |
| 2229546210 | Pharmacy | pharmacy | commercial | 1,195 | 29 | 1.00 | BackOffice, Dispensary, Restroom×2, Sales | 14 (16) | 6 | 307 | 26.9 (9.8 interior rooms) |
| 2230225126 | Pharmacy | pharmacy | commercial / shop:medical_supply | 549 | 27 | 0.96 | BackOffice, Dispensary, Restroom, Sales, Stock | 11 (12) | 6 lit 2 | 275 | 22.3 (6.1 interior rooms) |
| 2229427062 | Police | police | civic / police | 578 | 29 | 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 581 | 70.3 (6.1 interior rooms) |
| 2229565816 | Restaurant | commercial | commercial | 490 | 30 | 0.71 | CommercialKitchen, Eatery×2, Restroom×2 | 6 (11) | 9 | 542 | 50.4 (19.8 interior rooms) |
| 2229996042 | Restaurant | commercial | commercial | 254 | 29 | 0.95 | CommercialKitchen, Eatery, Restroom×2 | 4 (8) | 8 | 284 | 22.7 (6.7 interior rooms) |
| 2229984514 | Restaurant | restaurant | house | 94 | 31 | 0.99 | CommercialKitchen, Eatery | 4 (4) | 7 | 122 | 11.4 (7.1 interior rooms) |
| 2229549578 | Salon | salon | commercial | 364 | 31 | 0.94 | BackOffice, Restroom, Sales, Stock×2 | 6 (9) | 5 lit 2 | 184 | 14.8 (2.9 interior rooms) |
| 2229546202 | Salon | salon | commercial | 336 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 lit 1 | 141 | 12.1 (3.3 interior rooms) |
| 2229959138 | Salon | salon | commercial / shop:hairdresser | 327 | 30 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (8) | 4 | 128 | 11.1 (2.6 interior rooms) |
| 2229567552 | Salon | salon | commercial / shop:hairdresser | 250 | 28 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 4 lit 1 | 146 | 11.5 (2.7 interior rooms) |
| 2229833828 | Salon | salon | commercial / shop:hairdresser | 87 | 28 | 0.99 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 95 | 7.7 (2.0 interior rooms) |
| 2229532150 | Salon | salon | commercial / shop:hairdresser | 63 | 30 | 0.97 | Restroom, Sales, Stock | 2 (3) | 3 lit 1 | 89 | 6.8 (1.6 interior rooms) |
| 2229533468 | Store | department_store | commercial / shop:variety_store | 744 | 29 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 14 (16) | 5 | 272 | 21.1 (7.9 interior rooms) |
| 2229514212 | Store | clothing | commercial / shop:clothes | 727 | 30 | 1.00 | BackOffice, Restroom×2, Sales, Stock | 11 (13) | 4 | 219 | 19.9 (5.8 interior rooms) |
| 2230027974 | Store | shop | commercial / shop:tobacco | 611 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 11 (12) | 4 | 240 | 62.0 (23.0 interior rooms) |
| 2229565814 | Store | clothing | commercial / shop:second_hand | 553 | 29 | 0.99 | BackOffice, Restroom, Sales, Stock | 11 (12) | 5 | 215 | 17.0 (5.5 interior rooms) |
| 2229551880 | Store | general_store | commercial / shop:general | 544 | 28 | 0.78 | BackOffice, Restroom, Sales, Stock×2 | 11 (12) | 5 lit 1 | 250 | 24.9 (5.7 interior rooms) |
| 2229515476 | Store | shop | commercial / shop:shoe_repair | 508 | 28 | 0.96 | BackOffice, Restroom, Sales, Stock | 10 (11) | 5 | 195 | 17.3 (5.8 interior rooms) |
| 2229833826 | Store | guns | commercial | 427 | 29 | 0.91 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 2 | 204 | 16.8 (5.3 interior rooms) |
| 2229553354 | Store | shop | commercial | 369 | 29 | 0.95 | BackOffice, Restroom, Sales, Stock | 8 (9) | 4 | 171 | 13.1 (4.1 interior rooms) |
| 2230229660 | Store | shop | commercial / shop:florist | 361 | 27 | 1.00 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 | 184 | 16.2 (5.0 interior rooms) |
| 2229565812 | Store | furniture | commercial / shop:furniture | 333 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 176 | 14.3 (4.3 interior rooms) |
| 2229996044 | Store | furniture | commercial / shop:interior_decoration | 331 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 177 | 18.7 (4.7 interior merge) |
| 2229833830 | Store | clothing | retail | 286 | 27 | 0.98 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 28.8 (10.2 interior rooms) |
| 2230229658 | Store | commercial | commercial | 282 | 30 | 0.97 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 185 | 15.9 (5.2 interior rooms) |
| 2229822678 | Store | commercial | commercial | 272 | 31 | 1.00 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 170 | 13.4 (4.0 interior rooms) |
| 2229546208 | Store | shop | commercial / shop:florist | 252 | 29 | 0.93 | Restroom, Sales, Stock | 6 (7) | 4 | 121 | 10.6 (3.8 interior rooms) |
| 2229546200 | Store | furniture | commercial / shop:furniture | 236 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 4 lit 1 | 119 | 9.1 (3.3 interior rooms) |
| 2229959136 | Store | clothing | commercial / shop:clothes | 230 | 30 | 0.99 | Restroom, Sales, Stock | 6 (7) | 4 lit 1 | 117 | 9.1 (3.2 interior rooms) |
| 2229822688 | Store | laundry | house | 227 | 29 | 1.00 | BackOffice, Restroom, Sales, Stock | 5 (6) | 4 | 145 | 23.4 (6.1 interior doors) |
| 2229546198 | Store | shop | commercial / shop:books | 219 | 29 | 0.97 | Restroom, Sales, Stock | 5 (6) | 4 lit 2 | 114 | 9.1 (3.3 interior rooms) |
| 2229565808 | Store | shop | house | 212 | 28 | 0.98 | BackOffice, Restroom, Sales, Stock | 6 (7) | 5 | 164 | 13.9 (3.3 interior rooms) |
| 2992366852 | Store | shop | house | 199 | 29 | 0.96 | Restroom, Sales, Stock | 4 (5) | 3 | 109 | 8.5 (2.8 interior rooms) |
| 2229959134 | Store | furniture | house | 197 | 29 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 117 | 9.5 (3.5 interior rooms) |
| 2234865948 | Store | pet_store | commercial / shop:pet_grooming | 143 | 2 | 0.99 | BackOffice, Restroom, Sales, Stock | 4 (5) | 5 lit 1 | 118 | 11.3 (3.0 interior rooms) |
| 2229546204 | Store | shop | house | 128 | 30 | 0.99 | Restroom, Sales, Stock | 4 (5) | 3 | 108 | 9.5 (2.5 interior rooms) |
| 2229565804 | Store | pet_store | commercial / shop:pet_grooming | 112 | 29 | 0.92 | Restroom, Sales, Stock | 3 (4) | 3 lit 1 | 89 | 7.2 (1.9 interior rooms) |
| 2229984512 | Store | clothing | house | 97 | 31 | 0.98 | Sales, Stock | 3 (3) | 2 | 68 | 5.1 (1.6 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Table in Civic 1474017440): 108 of its triangles hidden in its room's furniture mesh, **OK**.

## Pikeville

KentuckyMap.Build 2,864 ms. Kentucky map Pikeville (build ky-local-f2): 57 buildings, 27 beyond the edge, 164 street pieces, 517 containers, 493 lights, 702 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 55 buildings furnished (491 rooms), 0 unplannable, 0 failed, 443 containers, 469 lights, 2366 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 22,558 | 20,968 |
| loot containers | 517 | 443 (1,784 more stripped of their storage) |
| lights | 493 | 469 (32 rooms left lit) |
| vertices | 4,839,688 | 3,625,475 |
| renderers (material slots) | | 9,664 (20,873; 1,464 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 2,200 ms in all, slowest step 26.0 ms |
| buildings furnished | | 55 (0 failed) |

<details><summary>Every building (55)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2638699002 | Apartments | apartments | apartments | 1,548 | 24 | 0.72 | Bathroom×19, Bedroom×19, BreakRoom, Hall, Laundry×3, Living×19, Lobby | 24 (24) | 55 lit 1 | 2163 | 243.8 (25.0 interior finish) |
| 2638698994 | AutoShop | commercial | commercial | 338 | 30 | 0.93 | Bays, Restroom, Sales | 5 (6) | 4 | 111 | 9.0 (3.4 interior rooms) |
| 2638698982 | BigStore | commercial | commercial | 892 | 29 | 0.50 | BackOffice, Office×2, Restroom×2, Sales, Stock×2 | 14 (16) | 5 lit 1 | 313 | 26.4 (6.5 interior rooms) |
| 2230524586 | Church | church | church / place_of_worship | 841 | 36 | 0.81 | Meeting×2, Nave, Sacristy×3 | 5 (5) | 6 lit 1 | 440 | 28.0 (8.9 interior rooms) |
| 2230524588 | Church | church | church / place_of_worship | 685 | 35 | 0.92 | Meeting, Nave, Sacristy×2 | 3 (3) | 6 | 388 | 22.2 (9.6 interior rooms) |
| 1729270072 | Civic | community | commercial / exhibition_centre | 7,904 | 9 | 0.82 | BreakRoom, Meeting, Office×3, Restroom×2, Stock×2 | 14 (16) | 10 lit 2 | 601 | 63.2 (11.4 interior rooms) |
| 2230524598 | Civic | courthouse | commercial / courthouse | 3,337 | 30 | 0.84 | BreakRoom, Hall, Lobby, Meeting×5, Office×12, Records×5, Stock×6 | 16 (16) | 13 lit 1 | 1312 | 131.2 (11.1 interior rooms) |
| 2230524592 | Civic | community | commercial | 2,480 | 31 | 0.89 | BreakRoom, Meeting, Office×3, Restroom×2, Stock | 9 (11) | 8 lit 1 | 530 | 41.2 (10.9 interior rooms) |
| 2230524590 | Civic | courthouse | commercial | 1,723 | 30 | 0.79 | BreakRoom, Hall, Lobby, Meeting×2, Office×7, Records×2, Stock×3 | 16 (16) | 9 lit 2 | 703 | 73.7 (10.2 interior rooms) |
| 2230524584 | Civic | courthouse | commercial | 834 | 20 | 0.68 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records×2, Restroom | 10 (11) | 10 lit 1 | 527 | 58.0 (6.5 interior merge) |
| 1924315432 | Civic | community | commercial / theatre | 568 | 27 | 0.83 | BreakRoom, Meeting, Office×2, Restroom×2, Stock×2 | 8 (10) | 8 lit 1 | 499 | 50.1 (21.1 interior rooms) |
| 2638698934 | Hardware | commercial | commercial | 590 | 30 | 0.90 | BackOffice, Restroom, Sales, Stock | 9 (10) | 5 lit 1 | 203 | 16.4 (5.7 interior rooms) |
| 2638698974 | House | home | house | 203 | 39 | 0.90 | Bathroom, Bedroom×2, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 11 lit 1 | 265 | 27.8 (4.8 interior rooms) |
| 2638698942 | House | home | house | 144 | 28 | 0.80 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 9 lit 1 | 183 | 16.8 (4.7 interior rooms) |
| 2638698992 | House | home | house | 144 | 27 | 0.94 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 207 | 19.9 (6.2 interior rooms) |
| 2638698920 | House | home | house | 138 | 40 | 0.99 | Bathroom, Bedroom×2, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 244 | 23.9 (7.0 interior rooms) |
| 2638699000 | House | home | house | 137 | 26 | 0.93 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 199 | 19.4 (7.3 interior rooms) |
| 2638698912 | House | home | house | 136 | 31 | 0.93 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 9 | 185 | 30.8 (13.5 interior rooms) |
| 2638698948 | House | home | house | 113 | 26 | 0.98 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (4) | 11 lit 1 | 218 | 20.8 (6.3 interior rooms) |
| 2638698962 | House | home | house | 109 | 34 | 0.75 | Bathroom, Bedroom, Laundry, Living | 4 (4) | 9 | 173 | 15.9 (4.5 interior rooms) |
| 2638698946 | House | home | house | 72 | 27 | 0.99 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 7 | 153 | 19.7 (6.5 interior walls) |
| 2638698960 | House | home | house | 70 | 34 | 0.83 | Bathroom, Bedroom, Living | 2 (3) | 7 | 125 | 11.4 (5.5 interior rooms) |
| 2230524594 | Office | office | commercial | 1,355 | 29 | 0.99 | BreakRoom, Hall, Lobby, Office×9, OpenOffice×4, Records×4, Stock×2 | 16 (16) | 10 | 758 | 83.2 (10.7 interior rooms) |
| 2638698986 | Office | commercial | commercial | 618 | 19 | 0.76 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records, Stock | 12 (12) | 9 lit 1 | 555 | 66.4 (11.2 interior rooms) |
| 2638698998 | Office | commercial | commercial | 555 | 29 | 0.96 | BreakRoom, Hall, Lobby, Office×3, OpenOffice×2, Records, Restroom | 10 (12) | 9 | 491 | 85.6 (26.0 interior rooms) |
| 2638698924 | Office | commercial | commercial | 412 | 38 | 0.96 | BreakRoom, Hall, Lobby, Office×4, OpenOffice×2, Records×2, Restroom, Stock×3 | 10 (10) | 11 lit 1 | 463 | 66.4 (20.2 interior rooms) |
| 2230524604 | Office | office | commercial | 321 | 27 | 0.80 | BreakRoom, Hall, Lobby, Office×7, OpenOffice×2, Records×2, Restroom, Stock | 6 (8) | 9 | 441 | 41.7 (3.1 interior walls) |
| 2638698936 | Office | commercial | commercial | 317 | 26 | 0.83 | BreakRoom, Hall, Lobby, Office×2, OpenOffice, Records, Restroom, Stock | 7 (8) | 7 | 377 | 45.3 (16.8 interior rooms) |
| 2576866036 | Police | police | commercial / police | 362 | 30 | 1.00 | Armory, Cells, Hall, Lobby, Office, Squad | 7 (8) | 5 | 255 | 24.5 (7.4 interior rooms) |
| 1696416174 | Restaurant | restaurant | commercial / restaurant | 436 | 10 | 0.57 | CommercialKitchen, Eatery×4, Restroom×2 | 6 (9) | 9 lit 1 | 429 | 35.2 (8.2 interior rooms) |
| 2638698972 | Restaurant | commercial | commercial | 364 | 39 | 0.94 | CommercialKitchen, Eatery, Restroom×2 | 2 (9) | 9 | 327 | 36.2 (9.8 interior merge) |
| 2638698954 | Restaurant | commercial | commercial | 294 | 31 | 0.84 | CommercialKitchen, Eatery, Restroom×2 | 6 (8) | 10 | 359 | 30.0 (11.1 interior rooms) |
| 1728600610 | Restaurant | restaurant | commercial / restaurant | 212 | 20 | 0.83 | CommercialKitchen, Eatery×2, Restroom×2 | 5 (7) | 8 lit 1 | 275 | 22.3 (6.6 interior rooms) |
| 1729248694 | School | school | school | 1,775 | 40 | 0.67 | BreakRoom, Classroom×14, Hall, Lobby, Office | 13 (14) | 12 | 894 | 103.8 (19.9 interior walls) |
| 1742875468 | School | school | school | 1,488 | 36 | 0.96 | BreakRoom, Classroom×8, Hall, Lobby, Office | 11 (12) | 10 | 609 | 73.6 (8.5 interior rooms) |
| 1729270030 | School | school | school | 1,206 | 28 | 0.87 | BreakRoom, Classroom×9, Hall, Lobby, Office | 10 (11) | 10 lit 1 | 686 | 68.2 (11.6 interior rooms) |
| 1729270046 | School | school | school | 801 | 18 | 0.69 | BreakRoom, Classroom×5, Hall, Lobby, Office×2 | 8 (9) | 8 | 503 | 49.9 (4.8 interior merge) |
| 1728609676 | School | school | school | 516 | 21 | 0.95 | BreakRoom, Classroom×7, Hall, Lobby, Office×2, Restroom | 7 (8) | 10 | 511 | 62.3 (5.3 interior rooms) |
| 2638698944 | Store | commercial | commercial | 668 | 20 | 0.77 | BackOffice, Office×2, Restroom, Sales, Stock×2 | 12 (13) | 6 | 303 | 31.4 (5.5 interior rooms) |
| 2638698996 | Store | commercial | commercial | 656 | 28 | 0.84 | BackOffice, Office, Restroom, Sales, Stock | 12 (13) | 6 | 264 | 21.1 (6.5 interior rooms) |
| 2638698966 | Store | commercial | commercial | 435 | 19 | 0.84 | BackOffice, Restroom, Sales, Stock×3 | 9 (10) | 5 lit 1 | 237 | 17.5 (5.1 interior rooms) |
| 2638698958 | Store | commercial | commercial | 389 | 30 | 0.66 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 6 | 215 | 29.5 (14.9 interior rooms) |
| 2638698914 | Store | commercial | commercial | 372 | 31 | 0.86 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 | 161 | 14.2 (3.3 interior rooms) |
| 2638698984 | Store | commercial | commercial | 346 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 2 | 179 | 14.3 (4.3 interior rooms) |
| 2638698968 | Store | commercial | commercial | 303 | 30 | 0.89 | BackOffice, Restroom, Sales, Stock | 7 (8) | 5 lit 1 | 181 | 15.0 (4.1 interior rooms) |
| 2230524602 | Store | commercial | retail | 179 | 30 | 0.96 | BackOffice, Restroom, Sales, Stock | 5 (6) | 5 lit 2 | 148 | 12.5 (3.0 interior rooms) |
| 2230524600 | Warehouse | commercial | commercial | 2,302 | 31 | 0.84 | Office, Restroom, Stock×2, Warehouse×2, Workshop×2 | 15 (16) | 5 lit 1 | 274 | 34.2 (12.6 interior doors) |
| 1919622998 | Warehouse | distillery | commercial | 1,891 | 34 | 0.76 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 9 (9) | 5 | 266 | 20.5 (3.9 interior rooms) |
| 2638698918 | Warehouse | commercial | commercial | 662 | 29 | 0.82 | Office, Restroom, Warehouse | 6 (7) | 5 | 128 | 10.9 (3.2 interior rooms) |
| 2638698956 | Warehouse | commercial | commercial | 466 | 25 | 0.86 | Office, Restroom, Stock×3, Warehouse×2 | 7 (9) | 5 | 174 | 14.0 (2.7 interior rooms) |
| 2638698976 | Warehouse | commercial | commercial | 405 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 5 (7) | 5 | 143 | 11.7 (2.8 interior rooms) |
| 2638698952 | Warehouse | commercial | commercial | 387 | 35 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 4 (6) | 5 lit 2 | 143 | 11.8 (2.9 interior rooms) |
| 2638698932 | Workshop | commercial | commercial | 450 | 30 | 0.84 | Office, Restroom, Stock×2, Workshop×2 | 8 (9) | 5 | 171 | 28.4 (6.0 interior doors) |
| 2638698930 | Workshop | commercial | commercial | 409 | 38 | 0.90 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 149 | 35.0 (7.0 interior rooms) |
| 2638698990 | Workshop | commercial | commercial | 268 | 40 | 0.93 | Office, Restroom, Stock, Workshop×2 | 7 (8) | 5 | 157 | 13.8 (3.2 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Table in School 1728609676): 74 of its triangles hidden in its room's furniture mesh, **OK**.

## Middlesboro

KentuckyMap.Build 1,341 ms. Kentucky map Middlesboro (build ky-local-f2): 31 buildings, 15 beyond the edge, 58 street pieces, 388 containers, 225 lights, 2408 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL)

Interiors: 31 buildings furnished (191 rooms), 0 unplannable, 0 failed, 274 containers, 205 lights, 873 ms

| | whole square | interiors |
|---|--:|--:|
| objects | 9,410 | 7,909 |
| loot containers | 388 | 274 (568 more stripped of their storage) |
| lights | 225 | 205 (5 rooms left lit) |
| vertices | 2,427,540 | 1,369,055 |
| renderers (material slots) | | 3,887 (8,348; 371 pieces' renderers merged into rooms' furniture meshes) |
| furnishing time | | 795 ms in all, slowest step 16.6 ms |
| buildings furnished | | 31 (0 failed) |

<details><summary>Every building (31)</summary>

| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |
|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|
| 2784318768 | Bank | bank | commercial | 2,005 | 7 | 0.88 | BreakRoom, Lobby, Office×3, Records, Restroom, Stock, Vault | 15 (16) | 9 | 409 | 39.0 (4.0 interior rooms) |
| 2784318778 | BigStore | guns | commercial | 2,059 | 7 | 0.75 | BackOffice, Office×2, Restroom×2, Sales, Stock | 14 (16) | 6 lit 1 | 376 | 30.1 (9.1 interior rooms) |
| 2784318782 | BigStore | guns | commercial | 1,804 | 7 | 0.68 | BackOffice, Restroom×2, Sales, Stock×4 | 14 (16) | 5 | 357 | 27.7 (7.5 interior rooms) |
| 2207404908 | Church | church | church / place_of_worship | 460 | 7 | 0.66 | Meeting×3, Nave, Sacristy×2 | 5 (5) | 6 lit 1 | 298 | 20.5 (4.7 interior rooms) |
| 2589948122 | FireStation | fire_station | commercial / fire_station | 835 | 7 | 0.91 | Bays, Bunks, DayRoom, Office, Restroom, Stock | 11 (12) | 7 | 300 | 31.0 (5.0 interior rooms) |
| 1807972230 | Funeral | funeral | commercial / shop:funeral_directors | 550 | 6 | 0.77 | Chapel, Office, Restroom, Stock | 4 (4) | 5 | 231 | 19.6 (5.2 interior rooms) |
| 3004391868 | House | home | house | 233 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (5) | 11 | 214 | 22.3 (7.9 interior rooms) |
| 3004391878 | House | home | house | 206 | 6 | 0.88 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 4 (5) | 11 | 230 | 22.2 (5.7 interior rooms) |
| 3004391870 | House | home | house | 193 | 7 | 0.99 | Bathroom, Bedroom, Hall, Kitchen, Living | 4 (5) | 11 | 220 | 32.9 (6.9 interior rooms) |
| 3004391882 | House | home | house | 190 | 6 | 0.95 | Bathroom, Bedroom, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 235 | 23.3 (6.7 interior rooms) |
| 3004391880 | House | home | house | 188 | 6 | 0.87 | Bathroom, Bedroom, Hall, Kitchen, Living | 3 (4) | 11 | 199 | 32.1 (10.9 interior rooms) |
| 3004391872 | House | home | house | 73 | 5 | 0.97 | Bathroom, Bedroom, Kitchen, Living | 4 (4) | 10 | 184 | 17.0 (5.0 interior rooms) |
| 2589948126 | Library | library | commercial / library | 1,001 | 6 | 0.80 | Office×3, Records, Restroom, Stacks, Stock | 11 (12) | 6 | 333 | 33.8 (11.5 interior rooms) |
| 2784318772 | Office | commercial | commercial | 442 | 7 | 0.98 | BreakRoom, Hall, Lobby, Office×6, OpenOffice×3, Records×2, Restroom, Stock | 9 (10) | 9 | 462 | 53.5 (7.3 interior rooms) |
| 1774615624 | Pharmacy | pharmacy | commercial | 1,533 | 7 | 0.94 | BackOffice, Dispensary, Office, Restroom×2, Sales | 14 (16) | 6 | 387 | 39.0 (10.6 interior rooms) |
| 2589948124 | Police | police | commercial / police | 489 | 6 | 1.00 | Armory, BreakRoom, Cells, Hall, Lobby, Office×3, Records, Squad | 10 (11) | 7 | 376 | 38.9 (7.6 interior rooms) |
| 2589948132 | PostOffice | post_office | commercial / post_office | 744 | 7 | 0.91 | BreakRoom, Lobby, Office, Restroom, Sorting, Stock×2 | 13 (14) | 7 lit 1 | 339 | 39.7 (16.6 interior rooms) |
| 2784318788 | Salon | salon | commercial | 690 | 7 | 0.91 | BackOffice, Office, Restroom×2, Sales, Stock | 6 (11) | 6 | 218 | 39.9 (7.4 interior rooms) |
| 3004391874 | Store | clothing | commercial | 448 | 6 | 1.00 | BackOffice, Restroom, Sales, Stock | 10 (11) | 4 | 161 | 14.0 (2.9 interior rooms) |
| 3004391876 | Store | commercial | commercial | 444 | 6 | 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 9 (10) | 5 lit 1 | 219 | 19.1 (3.0 interior rooms) |
| 3004391864 | Store | commercial | commercial | 392 | 7 | 0.99 | Restroom, Sales, Stock | 8 (9) | 4 | 145 | 10.6 (4.5 interior rooms) |
| 3004391866 | Store | clothing | commercial | 343 | 7 | 0.98 | Restroom, Sales, Stock | 7 (8) | 4 lit 1 | 139 | 10.0 (4.3 interior rooms) |
| 2784318774 | Warehouse | warehouse | warehouse | 2,797 | 7 | 0.81 | Office, Restroom, Stock, Warehouse×2, Workshop×3 | 11 (11) | 5 | 269 | 21.5 (4.0 interior rooms) |
| 2784318780 | Warehouse | commercial | commercial | 1,983 | 7 | 0.92 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 14 (16) | 5 | 262 | 35.4 (9.1 interior rooms) |
| 2784318790 | Warehouse | commercial | commercial | 1,302 | 8 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 8 (9) | 5 | 190 | 26.3 (5.6 interior doors) |
| 3004391884 | Warehouse | commercial | commercial | 1,247 | 6 | 0.99 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 5 | 194 | 15.3 (4.1 interior rooms) |
| 2784318786 | Warehouse | commercial | commercial | 1,159 | 6 | 0.59 | Office, Restroom, Stock, Warehouse×2, Workshop×2 | 13 (14) | 5 | 233 | 19.7 (3.4 interior rooms) |
| 2784318792 | Warehouse | commercial | commercial | 1,146 | 7 | 1.00 | Office, Restroom, Stock, Warehouse×2 | 9 (10) | 4 | 182 | 14.1 (3.8 interior rooms) |
| 2784318770 | Workshop | commercial | commercial | 876 | 7 | 0.99 | Office, Restroom, Stock, Workshop×2 | 8 (9) | 5 | 171 | 14.7 (3.0 interior rooms) |
| 2784318784 | Workshop | commercial | commercial | 762 | 7 | 0.95 | Office, Restroom, Stock, Workshop×3 | 10 (11) | 5 | 201 | 17.3 (2.7 interior rooms) |
| 1807972232 | Workshop | commercial | commercial | 372 | 7 | 0.75 | Office, Restroom, Workshop×2 | 8 (9) | 5 | 175 | 14.3 (3.1 interior rooms) |

</details>

Dismantling a merged piece (Furniture_Table in Office 2784318772): 106 of its triangles hidden in its room's furniture mesh, **OK**.

## By use (every square)

| use | buildings | failed | rooms (mean) | containers mean / max | allotted mean | lights mean / max | lit rooms | objects mean / max | renderers mean (slots) | vertices mean (k) | ms mean / max | slowest step ms |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Apartments | 2 | 0 | 50.0 | 24.0 / 24 | 24.0 | 47.0 / 55 | 1 | 1,807 / 2163 | 1,054 (2,173) | 356.3 | 210.7 / 243.8 | 25.0 |
| AutoShop | 4 | 0 | 2.5 | 4.3 / 5 | 5.0 | 3.3 / 4 | 1 | 96 / 115 | 47 (100) | 14.8 | 9.3 / 11.2 | 9.7 |
| Bank | 6 | 0 | 7.7 | 9.8 / 15 | 10.5 | 6.7 / 9 | 2 | 321 / 409 | 154 (347) | 54.9 | 35.0 / 43.5 | 8.2 |
| Bar | 2 | 0 | 3.5 | 5.5 / 6 | 8.5 | 7.5 / 12 | 2 | 164 / 228 | 82 (156) | 27.3 | 24.5 / 41.3 | 19.6 |
| Barn | 6 | 0 | 3.8 | 5.0 / 6 | 5.3 | 3.8 / 4 | 1 | 106 / 122 | 46 (93) | 13.1 | 9.7 / 17.9 | 7.3 |
| BigStore | 5 | 0 | 7.0 | 11.8 / 14 | 13.8 | 5.2 / 6 | 3 | 309 / 376 | 155 (340) | 53.5 | 26.0 / 30.1 | 9.1 |
| Church | 7 | 0 | 5.7 | 4.7 / 6 | 4.7 | 5.6 / 6 | 3 | 381 / 443 | 79 (179) | 37.2 | 28.6 / 38.1 | 11.8 |
| Civic | 12 | 0 | 12.6 | 11.0 / 16 | 12.2 | 9.3 / 13 | 8 | 589 / 1312 | 230 (510) | 107.0 | 62.9 / 131.2 | 21.1 |
| Clinic | 2 | 0 | 14.5 | 13.0 / 14 | 13.5 | 9.5 / 10 | 1 | 585 / 627 | 286 (673) | 87.8 | 64.0 / 64.2 | 9.0 |
| Convenience | 2 | 0 | 5.5 | 8.5 / 10 | 9.5 | 5.0 / 5 | 0 | 220 / 229 | 109 (241) | 35.1 | 38.1 / 59.6 | 20.6 |
| Cottage | 2 | 0 | 3.0 | 2.0 / 2 | 3.0 | 4.0 / 4 | 0 | 102 / 115 | 52 (110) | 15.2 | 13.6 / 19.6 | 8.1 |
| FireStation | 2 | 0 | 5.0 | 9.5 / 11 | 10.0 | 6.5 / 7 | 0 | 256 / 300 | 118 (247) | 43.8 | 26.2 / 31.0 | 5.6 |
| Funeral | 1 | 0 | 4.0 | 4.0 / 4 | 4.0 | 5.0 / 5 | 0 | 231 / 231 | 78 (177) | 36.2 | 19.6 / 19.6 | 5.2 |
| Garage | 3 | 0 | 1.0 | 1.3 / 2 | 1.3 | 1.0 / 1 | 0 | 37 / 43 | 17 (34) | 4.1 | 2.8 / 3.1 | 1.9 |
| Hardware | 4 | 0 | 4.0 | 8.5 / 12 | 9.5 | 4.3 / 5 | 4 | 180 / 205 | 86 (188) | 28.3 | 14.5 / 18.3 | 6.1 |
| House | 104 | 0 | 6.3 | 4.3 / 6 | 4.4 | 10.7 / 12 | 41 | 236 / 329 | 126 (261) | 37.4 | 27.3 / 68.7 | 21.6 |
| Library | 3 | 0 | 6.7 | 9.3 / 11 | 10.3 | 5.3 / 6 | 1 | 290 / 333 | 141 (306) | 54.6 | 32.6 / 34.7 | 11.9 |
| Motel | 2 | 0 | 17.0 | 15.5 / 18 | 15.5 | 14.5 / 19 | 0 | 554 / 582 | 300 (640) | 82.3 | 96.8 / 109.6 | 33.0 |
| Office | 19 | 0 | 11.3 | 8.4 / 16 | 9.3 | 8.1 / 11 | 8 | 422 / 758 | 238 (536) | 70.5 | 51.8 / 85.6 | 26.0 |
| Pharmacy | 5 | 0 | 5.2 | 13.2 / 14 | 14.8 | 5.8 / 6 | 2 | 305 / 387 | 157 (354) | 47.5 | 32.9 / 47.6 | 16.4 |
| Police | 3 | 0 | 10.7 | 9.7 / 12 | 10.3 | 7.7 / 11 | 0 | 404 / 581 | 214 (479) | 68.4 | 44.5 / 70.3 | 7.6 |
| PostOffice | 1 | 0 | 7.0 | 13.0 / 13 | 14.0 | 7.0 / 7 | 1 | 339 / 339 | 154 (311) | 86.5 | 39.7 / 39.7 | 16.6 |
| Restaurant | 11 | 0 | 4.4 | 5.6 / 9 | 8.5 | 9.4 / 12 | 3 | 356 / 542 | 139 (247) | 56.2 | 32.9 / 50.4 | 21.6 |
| Salon | 10 | 0 | 4.3 | 4.9 / 7 | 7.6 | 4.3 / 6 | 8 | 152 / 218 | 74 (164) | 23.4 | 14.9 / 39.9 | 7.4 |
| School | 5 | 0 | 13.2 | 9.8 / 13 | 10.8 | 10.0 / 12 | 1 | 641 / 894 | 243 (555) | 122.0 | 71.6 / 103.8 | 19.9 |
| Shed | 2 | 0 | 1.0 | 1.0 / 1 | 1.0 | 1.0 / 1 | 0 | 32 / 32 | 15 (28) | 3.2 | 2.6 / 2.9 | 2.0 |
| Store | 47 | 0 | 4.1 | 7.6 / 14 | 8.6 | 4.6 / 6 | 24 | 177 / 309 | 86 (189) | 28.1 | 16.8 / 62.0 | 23.0 |
| Warehouse | 18 | 0 | 5.8 | 8.8 / 15 | 10.0 | 4.9 / 5 | 4 | 193 / 274 | 94 (200) | 34.1 | 31.2 / 236.7 | 98.7 |
| Workshop | 14 | 0 | 5.0 | 7.7 / 14 | 8.7 | 4.6 / 5 | 3 | 164 / 211 | 84 (178) | 26.8 | 17.5 / 35.0 | 7.0 |

304 buildings furnished, 2058 loot containers in them.

Pieces kept out of the rooms' merged furniture meshes, by what keeps them apart: RoomLight×1854, RaidStoreSwivelSeat×437, BasinFixture×381, CivicToggle×372, SleepSpot×235, CookingStation×200, CivicFridge×167, KitchenFridge×156, RaidHomeBath×141, CivicLook×55, RaidStoreVending×51, RaidStoreSafe×44, RadioFixture×32, RaidStoreWaterCooler×32, CivicSink×30, RaidHomeWasher×30, ….

<details><summary>Room furnishing time by role (ms)</summary>

| role | rooms | mean | median | p95 | max |
|---|--:|--:|--:|--:|--:|
| Armory | 3 | 3.0 | 2.2 | 5.2 | 5.2 |
| BackOffice | 58 | 3.0 | 3.0 | 4.7 | 9.3 |
| Bar | 2 | 3.4 | 4.1 | 4.1 | 4.1 |
| Barn | 10 | 1.6 | 1.6 | 3.5 | 3.5 |
| Bathroom | 144 | 2.2 | 1.9 | 4.2 | 19.4 |
| Bays | 6 | 4.9 | 5.0 | 9.7 | 9.7 |
| Bedroom | 223 | 2.3 | 2.2 | 4.3 | 9.6 |
| BreakRoom | 44 | 4.4 | 4.0 | 11.6 | 22.7 |
| Bunks | 2 | 5.0 | 5.6 | 5.6 | 5.6 |
| Cells | 3 | 2.8 | 2.3 | 4.0 | 4.0 |
| Chapel | 1 | 5.2 | 5.2 | 5.2 | 5.2 |
| Classroom | 43 | 2.9 | 2.7 | 6.0 | 7.3 |
| Closet | 1 | 0.4 | 0.4 | 0.4 | 0.4 |
| CommercialKitchen | 12 | 10.1 | 9.7 | 19.8 | 19.8 |
| DayRoom | 2 | 3.0 | 4.1 | 4.1 | 4.1 |
| Den | 6 | 2.1 | 2.4 | 2.8 | 2.8 |
| Dining | 31 | 2.8 | 2.5 | 4.1 | 9.8 |
| Dispensary | 7 | 4.9 | 3.6 | 12.8 | 12.8 |
| Eatery | 18 | 6.9 | 5.5 | 21.6 | 21.6 |
| Exam | 14 | 3.7 | 3.0 | 7.9 | 7.9 |
| Guest | 20 | 2.7 | 2.2 | 15.1 | 15.1 |
| Hall | 125 | 0.2 | 0.0 | 0.5 | 1.5 |
| HomeGarage | 16 | 2.5 | 1.6 | 11.9 | 11.9 |
| Kitchen | 98 | 6.3 | 6.0 | 12.1 | 21.6 |
| Laundry | 30 | 1.5 | 1.1 | 4.5 | 5.9 |
| Living | 136 | 3.4 | 2.8 | 6.5 | 14.0 |
| Lobby | 45 | 2.7 | 2.6 | 4.7 | 8.5 |
| Meeting | 42 | 3.2 | 2.0 | 10.9 | 21.1 |
| Narthex | 1 | 1.3 | 1.3 | 1.3 | 1.3 |
| Nave | 7 | 8.2 | 8.3 | 10.2 | 10.2 |
| Office | 231 | 3.7 | 2.7 | 8.5 | 98.7 |
| OpenOffice | 33 | 7.3 | 6.0 | 20.7 | 26.0 |
| Records | 54 | 2.0 | 1.7 | 3.5 | 8.2 |
| Restroom | 178 | 1.8 | 1.8 | 4.7 | 12.3 |
| Sacristy | 12 | 3.8 | 3.2 | 11.8 | 11.8 |
| Sales | 76 | 5.1 | 4.1 | 14.9 | 23.0 |
| Shed | 2 | 1.7 | 2.0 | 2.0 | 2.0 |
| Sorting | 1 | 16.6 | 16.6 | 16.6 | 16.6 |
| Squad | 3 | 7.1 | 7.4 | 7.6 | 7.6 |
| Stacks | 3 | 9.3 | 8.4 | 11.5 | 11.5 |
| Stock | 180 | 1.8 | 1.4 | 4.9 | 13.6 |
| Vault | 6 | 2.3 | 1.8 | 5.2 | 5.2 |
| Waiting | 2 | 2.8 | 2.8 | 2.8 | 2.8 |
| Warehouse | 33 | 2.4 | 1.9 | 4.2 | 13.9 |
| Workshop | 45 | 2.6 | 2.3 | 5.8 | 10.6 |

</details>

## Pictures

Left: from straight above in the building's own axes (its front door's wall at the bottom), roofs, upper storeys and ceilings off, plain overcast daylight without shadows. Right: the game's isometric camera (widened to fit the building), the red capsule standing in for the survivor in its biggest room, the cutaway run (its roof and the walls turned to the camera lifted), ceilings hidden as in the game; interior partitions aren't cut here (the game's IsoCutaway does that in play).

| file | place | building | footprint | rooms | containers (allotted) | lights | objects |
|---|---|---|---|---|--:|--:|--:|
| `01_house_2622504514.png` | Bardstown | House (home; house) 2622504514 | 219 m², turned 15°, 6 corners, fill 0.86 | Bathroom, Bedroom×3, Den, Dining, Hall, Kitchen, Laundry, Living | 5 (5) | 11 | 329 |
| `02_cottage_2622506312.png` | Bardstown | Cottage (outbuilding; house) 2622506312 | 57 m², turned 13°, 4 corners, fill 0.99 | Bathroom, Bedroom, Living | 2 (3) | 4 | 89 |
| `03_motel_1915619802.png` | Bardstown | Motel (hotel; commercial) 1915619802 | 823 m², turned 15°, 9 corners, fill 0.89 | BreakRoom, Guest×12, Hall, Laundry, Lobby | 18 (18) | 19 | 582 |
| `04_store_2622504474.png` | Bardstown | Store (clothing; commercial) 2622504474 | 727 m², turned 14°, 12 corners, fill 0.88 | BackOffice, Restroom, Sales, Stock×4 | 14 (15) | 4 | 309 |
| `05_bigstore_2363563962.png` | Bardstown | BigStore (grocery; commercial / shop:pastry) 2363563962 | 432 m², turned 13°, 10 corners, fill 0.92 | BackOffice, Office×2, Restroom×2, Sales, Stock | 8 (10) | 5 | 278 |
| `06_convenience_2622504504.png` | Bardstown | Convenience (convenience; commercial) 2622504504 | 304 m², turned 14°, 10 corners, fill 0.67 | BackOffice, Office, Restroom, Sales, Stock×2 | 7 (8) | 5 | 211 |
| `07_pharmacy_281586026.png` | Bardstown | Pharmacy (pharmacy; commercial / pharmacy) 281586026 | 994 m², turned 15°, 6 corners, fill 0.71 | BackOffice, Dispensary, Restroom×2, Sales, Stock | 14 (16) | 6 | 310 |
| `08_hardware_2622505338.png` | Bardstown | Hardware (commercial; commercial) 2622505338 | 336 m², turned 14°, 8 corners, fill 0.81 | BackOffice, Office, Restroom, Sales, Stock | 8 (9) | 5 | 203 |
| `09_autoshop_2363563990.png` | Bardstown | AutoShop (commercial; commercial) 2363563990 | 254 m², turned 14°, 5 corners, fill 0.95 | Bays | 3 (3) | 2 | 58 |
| `10_salon_2363563974.png` (+ `10_salon_2363563974_night.png`) | Bardstown | Salon (salon; commercial) 2363563974 | 511 m², turned 12°, 6 corners, fill 0.91 | BackOffice, Restroom, Sales, Stock×2 | 7 (11) | 4, 2 lit | 188 |
| `11_restaurant_2622504478.png` (+ `11_restaurant_2622504478_night.png`) | Bardstown | Restaurant (restaurant; commercial) 2622504478 | 379 m², turned 14°, 10 corners, fill 0.91 | CommercialKitchen, Eatery×3, Restroom×2 | 8 (10) | 12, 1 lit | 514 |
| `12_bar_2363563996.png` (+ `12_bar_2363563996_night.png`) | Bardstown | Bar (bar; house) 2363563996 | 209 m², turned 13°, 5 corners, fill 0.99 | Bar, Restroom, Stock | 5 (6) | 3, 1 lit | 99 |
| `13_office_2622504446.png` | Bardstown | Office (commercial; commercial) 2622504446 | 388 m², turned 14°, 6 corners, fill 0.88 | BreakRoom, Hall, Lobby, Office×5, OpenOffice×2, Records, Stock | 10 (10) | 10 | 463 |
| `14_bank_2622504490.png` | Bardstown | Bank (bank; commercial) 2622504490 | 584 m², turned 14°, 8 corners, fill 0.93 | BreakRoom, Lobby, Office×3, Records, Restroom, Vault | 9 (10) | 8 | 353 |
| `15_civic_2622504470.png` | Bardstown | Civic (courthouse; commercial) 2622504470 | 554 m², turned 14°, 5 corners, fill 0.99 | BreakRoom, Hall, Lobby, Meeting×2, Office×5, Records | 11 (12) | 9 | 459 |
| `16_clinic_2363564004.png` | Bardstown | Clinic (clinic; commercial) 2363564004 | 543 m², turned 14°, 4 corners, fill 0.98 | Dispensary, Exam×6, Hall, Office×4, Records, Waiting | 12 (12) | 9 | 543 |
| `17_house_2238555434.png` (+ `17_house_2238555434_night.png`) | Princeton | House (home; house) 2238555434 | 248 m², turned 30°, 6 corners, fill 0.96 | Bathroom, Bedroom×4, Dining, Hall, Kitchen, Living | 5 (5) | 12, 1 lit | 322 |
| `18_apartments_2229833810.png` | Princeton | Apartments (apartments; apartments) 2229833810 | 1,310 m², turned 27°, 6 corners, fill 0.72 | Bathroom×11, Bedroom×11, BreakRoom, Hall, Laundry, Living×11, Lobby | 24 (24) | 39 | 1450 |
| `19_library_1474017444.png` | Princeton | Library (library; commercial / library) 1474017444 | 613 m², turned 27°, 8 corners, fill 0.74 | Office×2, Records, Restroom, Stacks, Stock×2 | 10 (11) | 5, 1 lit | 304 |
| `20_police_2229427062.png` | Princeton | Police (police; civic / police) 2229427062 | 578 m², turned 29°, 4 corners, fill 0.98 | Armory, BreakRoom, Cells, Hall, Lobby, Office×5, Records×3, Restroom, Squad, Stock | 12 (12) | 11 | 581 |
| `21_firestation_2229427060.png` | Princeton | FireStation (fire_station; civic / fire_station) 2229427060 | 401 m², turned 27°, 9 corners, fill 0.96 | Bays, Bunks, DayRoom, Office | 8 (8) | 6 | 211 |
| `22_church_2229567554.png` | Princeton | Church (church; church) 2229567554 | 663 m², turned 28°, 14 corners, fill 0.77 | Meeting×4, Nave, Sacristy | 5 (5) | 5 | 394 |
| `23_barn_2229549568.png` | Princeton | Barn (commercial; commercial) 2229549568 | 272 m², turned 29°, 6 corners, fill 0.86 | Barn, Stock×3 | 5 (5) | 4 | 110 |
| `24_garage_2238550564.png` | Princeton | Garage (outbuilding; house) 2238550564 | 32 m², turned 25°, 4 corners, fill 1.00 | HomeGarage | 1 (1) | 1 | 34 |
| `25_school_1729270046.png` | Pikeville | School (school; school) 1729270046 | 801 m², turned 18°, 10 corners, fill 0.69 | BreakRoom, Classroom×5, Hall, Lobby, Office×2 | 8 (9) | 8 | 503 |
| `26_warehouse_2638698918.png` | Pikeville | Warehouse (commercial; commercial) 2638698918 | 662 m², turned 29°, 15 corners, fill 0.82 | Office, Restroom, Warehouse | 6 (7) | 5 | 128 |
| `27_workshop_2638698932.png` | Pikeville | Workshop (commercial; commercial) 2638698932 | 450 m², turned 30°, 6 corners, fill 0.84 | Office, Restroom, Stock×2, Workshop×2 | 8 (9) | 5 | 171 |
| `28_postoffice_2589948132.png` | Middlesboro | PostOffice (post_office; commercial / post_office) 2589948132 | 744 m², turned 7°, 6 corners, fill 0.91 | BreakRoom, Lobby, Office, Restroom, Sorting, Stock×2 | 13 (14) | 7, 1 lit | 339 |
| `29_funeral_1807972230.png` | Middlesboro | Funeral (funeral; commercial / shop:funeral_directors) 1807972230 | 550 m², turned 6°, 6 corners, fill 0.77 | Chapel, Office, Restroom, Stock | 4 (4) | 5 | 231 |

<details><summary>What the pictured buildings are made of (bodies by name; lights by the body holding them)</summary>

- `01_house_2622504514.png`: pieces 81: Wall×18, DoorHead×8, RoomLight×5, Furniture_Dresser×4, Bed×3, Furniture Bedroom×3, Furniture_Chair×3, Wardrobe×3, Desk×2, Door×2, Floors×2, Fridge×2, Furniture Dining Room×2, Furniture Living Room×2, …; vertices 52,303: Wall×7710, DoorHead×6944, Stove×4565, Fridge×3598, Furniture×3179, Wardrobe×2140, Furniture_Basin×2093, Furniture Living Room×2090, Furniture_Fridge×1799, Furniture Dining Room×1550, …; renderers 177 (392 material slots): Wall×41, DoorHead×24, Stove×14, Fridge×10, RoomLight×10, Bed×9, Furniture_Nightstand×8, Bath×7, Desk×7, Wardrobe×7, …; lights: RoomLight×5, Fridge×2, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1
- `02_cottage_2622506312.png`: pieces 24: Wall×4, RoomLight×3, DoorHead×2, Floors×2, Furniture Living Room×2, Bath×1, Coffee table×1, Colliders×1, Desk×1, Door×1, Furniture×1, Furniture_Armchair×1, Furniture_Bookshelf×1, Sofa×1, …; vertices 11,424: Furniture×2337, Furniture Living Room×2114, DoorHead×1736, Bath×1280, Wall×1032, Wardrobe×974, Door×572, TV×409, Floors×386, RoomLight×376, …; renderers 40 (94 material slots): Bath×7, Wall×7, DoorHead×6, RoomLight×6, TV×3, Wardrobe×3, Door×2, Floors×2, Furniture Living Room×2, Furniture×1, …; lights: RoomLight×3, TV×1
- `03_motel_1915619802.png`: pieces 154: Wall×31, Chair×17, Furniture Room×17, DoorHead×14, Bed×12, Furniture_Dresser×12, Wardrobe×12, Furniture_Tv×9, RoomLight×5, Door×2, Floors×2, Furniture_Nightstand×2, Shelves×2, Table×2, …; vertices 85,497: DoorHead×12152, Wardrobe×10800, Wall×8958, Furniture Room×8424, Furniture_Tv×6489, Bed×5964, Furniture×5378, Floors×4595, Stove×4565, Furniture Common Room×2464, …; renderers 295 (621 material slots): Wall×54, DoorHead×42, Bed×36, Wardrobe×36, Furniture_Tv×27, Furniture Room×17, Stove×14, Furniture_Dresser×11, RoomLight×10, Furniture_Nightstand×8, …; lights: Furniture_Tv×9, RoomLight×5, Furniture_Nightstand×2, Furniture_Fridge×1, Stove×1, TV×1
- `04_store_2622504474.png`: pieces 101: Store shelves×32, Wall×13, Lockers×11, Shelves×11, Wardrobe×9, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Checkout counter×1, Colliders×1, …; vertices 56,142: Store shelves×9248, Lockers×7342, Furniture×6977, Wardrobe×6300, DoorHead×5208, Shelves×5168, Furniture_Basin×4186, Wall×3192, Floors×2696, Filing cabinet×1814, …; renderers 151 (319 material slots): Store shelves×32, Wardrobe×21, DoorHead×18, Wall×17, Furniture_Basin×12, Lockers×11, Shelves×11, RoomLight×8, Filing cabinet×5, Door×4, …; lights: RoomLight×4
- `05_bigstore_2363563962.png`: pieces 81: Shelves×17, Wall×12, Store shelves×11, DoorHead×6, Furniture_Basin×4, RoomLight×4, Bookcase×3, Drinks cooler×3, Filing cabinet×3, Chair×2, Door×2, Floors×2, Furniture Office×2, Office chair×2, …; vertices 49,725: Furniture_Basin×8372, Shelves×8060, DoorHead×5208, Furniture×3748, Office desk×3530, Store shelves×3184, Wall×3164, Filing cabinet×3086, Floors×2562, Furniture Office×2424, …; renderers 149 (325 material slots): Furniture_Basin×24, DoorHead×18, Wall×18, Shelves×17, Office desk×16, Store shelves×11, Filing cabinet×9, RoomLight×8, Drinks cooler×6, Door×4, …; lights: RoomLight×4, Office desk×1
- `06_convenience_2622504504.png`: pieces 63: Shelves×18, Wall×10, Store shelves×6, DoorHead×5, RoomLight×4, Bookcase×2, Crate×2, Door×2, Filing cabinet×2, Floors×2, Chair×1, Checkout counter×1, Colliders×1, Drinks cooler×1, …; vertices 35,284: Shelves×8656, DoorHead×4340, Furniture×3862, Wall×2780, Filing cabinet×2246, Furniture_Basin×2093, Office desk×1936, Floors×1838, Store shelves×1736, Checkout counter×1092, …; renderers 107 (236 material slots): Shelves×18, Wall×17, DoorHead×15, Office desk×9, RoomLight×8, Filing cabinet×7, Furniture_Basin×6, Store shelves×6, Crate×4, Door×4, …; lights: RoomLight×4, Office desk×1
- `07_pharmacy_281586026.png`: pieces 89: Store shelves×30, Shelves×10, Wall×10, Medicine cabinet×7, DoorHead×5, stall×5, RoomLight×4, Checkout counter×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Chair×1, Colliders×1, …; vertices 50,425: Store shelves×8736, Medicine cabinet×5796, Furniture×5179, Shelves×5032, DoorHead×4340, Furniture_Basin×4186, Floors×3744, Wall×2636, Checkout counter×2184, Office desk×1962, …; renderers 161 (369 material slots): Medicine cabinet×35, Store shelves×30, DoorHead×15, Wall×13, Furniture_Basin×12, Shelves×10, Office desk×9, RoomLight×8, Medical fridge×5, stall×5, …; lights: RoomLight×4, Medical fridge×1, Office desk×1
- `08_hardware_2622505338.png`: pieces 60: Shelves×17, Wall×8, Store shelves×6, DoorHead×4, RoomLight×4, Bookcase×2, Door×2, Filing cabinet×2, Floors×2, Office chair×2, Office desk×2, Chair×1, Checkout counter×1, Colliders×1, …; vertices 34,207: Shelves×7992, DoorHead×3472, Office desk×3452, Floors×2812, Furniture×2650, Wall×2156, Furniture_Basin×2093, Store shelves×1740, Furniture Back Office×1344, Filing cabinet×1224, …; renderers 102 (221 material slots): Shelves×17, Office desk×16, Wall×14, DoorHead×12, RoomLight×8, Furniture_Basin×6, Store shelves×6, Door×4, Filing cabinet×4, Bookcase×2, …; lights: RoomLight×4, Office desk×1
- `09_autoshop_2363563990.png`: pieces 15: Door×2, Floors×2, RoomLight×2, Car×1, Colliders×1, Filing cabinet×1, Furniture×1, Jerry can×1, Light switch plate×1, Shelves×1, Stack of tyres×1, Tool bench×1; vertices 8,948: Filing cabinet×1910, Tool bench×1536, Car×1518, Floors×1211, Door×1144, Shelves×560, Furniture×405, RoomLight×224, Stack of tyres×200, Jerry can×120, …; renderers 28 (58 material slots): Filing cabinet×5, Door×4, RoomLight×4, Tool bench×4, Car×3, Floors×2, Light switch plate×2, Furniture×1, Jerry can×1, Shelves×1, …; lights: RoomLight×2
- `10_salon_2363563974.png`: pieces 58: Shelves×17, Wall×8, DoorHead×4, Office chair×4, RoomLight×4, Waiting chair×3, salon counter×3, Door×2, Floors×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Back Office×1, …; vertices 30,861: Shelves×7884, Furniture×4246, DoorHead×3472, Floors×2162, Wall×2128, Furniture_Basin×2093, Furniture Salon×1734, Office desk×1600, Furniture Back Office×1200, Door×860, …; renderers 87 (197 material slots): Shelves×17, Wall×13, DoorHead×12, RoomLight×8, Office desk×7, Furniture_Basin×6, Medicine cabinet×5, Door×4, salon counter×3, Filing cabinet×2, …; lights: RoomLight×4
- `11_restaurant_2622504478.png`: pieces 104: Table×17, Stool×16, Booth×14, Wall×10, stall×6, DoorHead×5, Fridge×4, Furniture Dining Room×4, Furniture_Basin×4, RoomLight×4, Stove×4, Back counter×2, Checkout counter×2, Door×2, …; vertices 80,515: Furniture Dining Room×24146, Stove×18260, Furniture_Basin×8372, Fridge×7196, Wall×4474, DoorHead×4340, Back counter×3940, Checkout counter×2208, Steel counter×1878, Floors×1550, …; renderers 226 (392 material slots): Stove×56, Back counter×33, Wall×26, Furniture_Basin×24, Fridge×20, Steel counter×16, DoorHead×15, RoomLight×8, Vending machine×6, stall×6, …; lights: Fridge×4, RoomLight×4, Stove×4
- `12_bar_2363563996.png`: pieces 33: Stool×8, Table×4, Wall×4, RoomLight×3, Door×2, DoorHead×2, Floors×2, stall×2, Colliders×1, Crate×1, Furniture×1, Furniture Bar×1, Light switch plate×1, Shelves×1; vertices 10,910: Furniture Bar×3076, DoorHead×1736, Furniture×1443, Floors×1415, Wall×976, Door×860, Shelves×492, Crate×408, RoomLight×336, Light switch plate×120, …; renderers 32 (73 material slots): DoorHead×6, RoomLight×6, Wall×5, Door×4, Crate×2, Floors×2, Light switch plate×2, stall×2, Furniture×1, Furniture Bar×1, …; lights: RoomLight×3
- `13_office_2622504446.png`: pieces 120: Wall×23, Filing cabinet×12, Chair×11, DoorHead×11, Office chair×10, Bookcase×6, Office desk×6, RoomLight×6, Furniture Office×5, Desk×4, Furniture_Chair×4, Door×2, Floors×2, Records shelf×2, …; vertices 75,658: Office desk×9970, DoorHead×9548, Filing cabinet×7300, Wall×6816, Desk×6204, Furniture Office×5688, Furniture×5102, Stove×4565, Office chair×3620, Floors×3282, …; renderers 256 (565 material slots): Office desk×46, Wall×42, DoorHead×33, Desk×28, Filing cabinet×22, Stove×14, RoomLight×12, Office chair×10, Bookcase×6, Chair×6, …; lights: RoomLight×6, Office desk×2, Furniture_Fridge×1, Stove×1
- `14_bank_2622504490.png`: pieces 91: Wall×14, Chair×12, DoorHead×7, Filing cabinet×7, Desk×5, Safe×5, Furniture_Chair×4, RoomLight×4, Bookcase×3, Furniture Office×3, Office chair×3, Office desk×3, Door×2, Floors×2, …; vertices 60,783: DoorHead×6076, Furniture Banking Hall×5922, Office desk×5552, Filing cabinet×4790, Stove×4565, Wall×4304, Furniture×4279, Furniture_Basin×4186, Furniture Office×3504, Floors×2768, …; renderers 171 (382 material slots): Office desk×25, Wall×24, DoorHead×21, Filing cabinet×14, Stove×14, Furniture_Basin×12, Safe×10, RoomLight×8, Furniture_Fridge×5, Desk×4, …; lights: RoomLight×4, Office desk×2, Furniture_Fridge×1, Stove×1
- `15_civic_2622504470.png`: pieces 122: Wall×21, Chairs×20, Chair×14, Filing cabinet×11, DoorHead×10, Office chair×6, Bookcase×5, Furniture Office×5, Office desk×5, RoomLight×5, Table×3, Door×2, Floors×2, Furniture Meeting Room×2, …; vertices 79,140: Furniture Meeting Room×11952, DoorHead×8680, Office desk×8622, Filing cabinet×7108, Wall×6522, Furniture Office×6384, Floors×5311, Stove×4565, Furniture×3902, Office chair×2172, …; renderers 203 (448 material slots): Office desk×39, Wall×39, DoorHead×30, Filing cabinet×21, Stove×14, RoomLight×10, Front desk×7, Office chair×6, Bookcase×5, Furniture Office×5, …; lights: RoomLight×5, Office desk×2, Furniture_Fridge×1, Stove×1
- `16_clinic_2363564004.png`: pieces 150: Wall×26, DoorHead×12, Furniture Exam Room×12, Filing cabinet×11, Office chair×10, Waiting-room chair×10, Medicine cabinet×7, Counter×6, Desk×6, Examination couch×6, RoomLight×6, Stool×6, Shelves×5, Bookcase×4, …; vertices 81,602: DoorHead×10416, Furniture Exam Room×9830, Counter×7434, Office desk×7156, Wall×7000, Filing cabinet×6278, Medicine cabinet×4836, Furniture Office×4752, Floors×4350, Office chair×3620, …; renderers 263 (622 material slots): Wall×44, Counter×36, DoorHead×36, Office desk×32, Medicine cabinet×23, Filing cabinet×19, Furniture Exam Room×12, RoomLight×12, Office chair×10, Examination couch×6, …; lights: RoomLight×6, Office desk×2, Medical fridge×1
- `17_house_2238555434.png`: pieces 78: Wall×16, DoorHead×7, Furniture_Dresser×5, RoomLight×5, Bed×4, Furniture Bedroom×4, Furniture_Chair×4, Wardrobe×4, Door×2, Floors×2, Fridge×2, Furniture Dining Room×2, Furniture Kitchen×2, Furniture Living Room×2, …; vertices 52,843: Wall×8190, DoorHead×6076, Furniture Kitchen×4586, Stove×4565, Fridge×3598, Furniture×3409, Wardrobe×2476, Floors×2118, Furniture_Basin×2093, Furniture Living Room×2048, …; renderers 168 (347 material slots): Wall×41, DoorHead×21, Stove×14, Bed×12, Fridge×10, RoomLight×10, Wardrobe×8, Bath×7, Furniture_Basin×6, Furniture_Fridge×5, …; lights: RoomLight×5, Fridge×2, Furniture_FloorLamp×1, Furniture_Fridge×1, Furniture_Nightstand×1, Stove×1, TV×1
- `18_apartments_2229833810.png`: pieces 305: Wall×62, DoorHead×33, Furniture Living Room×22, Furniture_Fridge×12, Sofa×12, Stove×12, Bath×11, Bed×11, Coffee table×11, Furniture Bedroom×11, Furniture_Armchair×11, Furniture_Basin×11, Furniture_Bookshelf×11, Furniture_Dresser×11, …; vertices 280,754: Stove×54780, Wall×31958, Furniture×31207, DoorHead×28644, Furniture_Basin×23023, Furniture Living Room×21978, Furniture_Fridge×21588, Bath×13984, Wardrobe×9970, Floors×7090, …; renderers 813 (1684 material slots): Stove×168, Wall×150, DoorHead×99, Bath×77, Furniture_Basin×66, Furniture_Fridge×60, Bed×33, Wardrobe×33, TV×24, Furniture Living Room×22, …; lights: Furniture_Fridge×12, Stove×12, TV×8, RoomLight×5, Furniture_Nightstand×2
- `19_library_1474017444.png`: pieces 94: Stacks×18, Filing cabinet×16, Wall×12, DoorHead×6, Shelves×6, Shelving×6, Bookcase×4, Reading table×4, RoomLight×4, Door×2, Floors×2, Furniture Office×2, Furniture_Basin×2, Office chair×2, …; vertices 58,917: Filing cabinet×9004, Stacks×8640, DoorHead×5208, Furniture×4776, Furniture_Basin×4186, Furniture Library×4002, Floors×3784, Office desk×3586, Wall×3288, Shelves×3272, …; renderers 146 (314 material slots): Filing cabinet×26, DoorHead×18, Stacks×18, Wall×17, Office desk×16, Furniture_Basin×12, RoomLight×8, Shelves×6, Shelving×6, Bookcase×4, …; lights: RoomLight×4, Office desk×1
- `20_police_2229427062.png`: pieces 159: Wall×30, Filing cabinet×21, Chair×16, DoorHead×14, Office chair×9, RoomLight×7, Desk×6, Records shelf×6, Bookcase×5, Cell bars×5, Furniture Chief's Office×5, Office desk×5, Furniture_Chair×4, Ammunition crate×2, …; vertices 103,234: Filing cabinet×13252, DoorHead×12152, Wall×9046, Desk×9030, Office desk×8806, Furniture×7106, Furniture Chief's Office×5688, Floors×5076, Stove×4565, Furniture_Basin×4186, …; renderers 314 (695 material slots): Wall×49, DoorHead×42, Desk×40, Office desk×39, Filing cabinet×38, RoomLight×14, Stove×14, Furniture_Basin×12, Office chair×9, Chair×6, …; lights: RoomLight×7, Office desk×2, Furniture_Fridge×1, Stove×1
- `21_firestation_2229427060.png`: pieces 50: Wall×6, Bunk×4, Footlocker×4, Locker×4, RoomLight×4, DoorHead×3, Table×3, Door×2, Floors×2, Furniture Bunk Room×2, Gear locker×2, Bookcase×1, Colliders×1, Filing cabinet×1, …; vertices 31,986: Locker×4509, Furniture Day Room×3814, Gear locker×3087, Floors×2719, DoorHead×2604, Wall×2356, Bunk×2088, Office desk×2034, Furniture Bunk Room×1752, Tool bench×1518, …; renderers 90 (188 material slots): Locker×13, Wall×13, DoorHead×9, Office desk×9, Gear locker×8, RoomLight×8, Bunk×4, Door×4, Tool bench×4, TV×3, …; lights: RoomLight×4, Office desk×1, TV×1
- `22_church_2229567554.png`: pieces 82: Pew×35, Wall×10, Table×8, DoorHead×5, RoomLight×4, Pantry shelves×3, Door×2, Floors×2, Furniture Fellowship Hall×2, Bookcase×1, Chair×1, Colliders×1, Desk×1, Filing cabinet×1, …; vertices 38,458: Furniture Sanctuary×8568, Furniture Fellowship Hall×8306, DoorHead×4340, Floors×3524, Wall×2604, Desk×2030, Pantry shelves×1532, Door×1144, Furniture×1128, Furniture Pastor's Study×1056, …; renderers 70 (160 material slots): DoorHead×15, Wall×14, Desk×9, RoomLight×8, Door×4, Pantry shelves×3, Wardrobe×3, Filing cabinet×2, Floors×2, Furniture Fellowship Hall×2, …; lights: RoomLight×4, Desk×1
- `23_barn_2229549568.png`: pieces 28: Wall×6, Shelves×5, Crate×4, RoomLight×4, DoorHead×3, Door×2, Floors×2, Colliders×1, Furniture×1; vertices 13,271: Furniture×4209, DoorHead×2460, Shelves×2392, Crate×1680, Door×1144, Wall×568, RoomLight×448, Floors×370, Colliders×0; renderers 46 (93 material slots): DoorHead×9, Wall×9, Crate×8, RoomLight×8, Shelves×5, Door×4, Floors×2, Furniture×1; lights: RoomLight×4
- `24_garage_2238550564.png`: pieces 10: Floors×2, Colliders×1, Door×1, Furniture×1, Jerry can×1, Light switch plate×1, RoomLight×1, Stack of tyres×1, Tool bench×1; vertices 3,384: Tool bench×1594, Door×572, Furniture×396, Floors×270, Stack of tyres×200, Jerry can×120, Light switch plate×120, RoomLight×112, Colliders×0; renderers 15 (29 material slots): Tool bench×4, Door×2, Floors×2, Light switch plate×2, RoomLight×2, Furniture×1, Jerry can×1, Stack of tyres×1; lights: RoomLight×1
- `25_school_1729270046.png`: pieces 95: Wall×17, Chair×14, Furniture Classroom×10, DoorHead×8, Bookcase×7, RoomLight×5, Teacher's desk×5, Desk×4, Filing cabinet×3, Table×3, Door×2, Floors×2, Office chair×2, Office desk×2, …; vertices 92,740: Furniture×38056, Furniture Classroom×8122, Wall×8100, Floors×7534, DoorHead×6944, Teacher's desk×4296, Furniture Break Room×3996, Office desk×3844, Furniture Lobby×1944, Filing cabinet×1694, …; renderers 170 (397 material slots): Wall×50, DoorHead×24, Teacher's desk×20, Office desk×18, Furniture Classroom×10, RoomLight×10, Bookcase×7, Filing cabinet×5, Door×4, Locker×4, …; lights: RoomLight×5, Office desk×2, TV×1
- `26_warehouse_2638698918.png`: pieces 34: Racking×10, RoomLight×4, Wall×4, Door×2, DoorHead×2, Floors×2, Bookcase×1, Chair×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, Furniture_Basin×1, Office chair×1, …; vertices 21,504: Racking×4820, Furniture×4187, Furniture_Basin×2093, Office desk×1938, Floors×1775, DoorHead×1712, Door×1144, Furniture Office×1128, Wall×908, Filing cabinet×552, …; renderers 60 (131 material slots): Racking×10, Office desk×9, RoomLight×8, Wall×7, DoorHead×6, Furniture_Basin×6, Door×4, Filing cabinet×2, Floors×2, Bookcase×1, …; lights: RoomLight×4, Office desk×1
- `27_workshop_2638698932.png`: pieces 39: Wall×9, DoorHead×4, RoomLight×4, Tool bench×4, Shelves×3, Crate×2, Door×2, Floors×2, Furniture_Basin×2, Bookcase×1, Colliders×1, Filing cabinet×1, Furniture×1, Furniture Office×1, …; vertices 28,043: Tool bench×6458, Furniture_Basin×4186, DoorHead×3352, Furniture×2945, Floors×2196, Office desk×1908, Wall×1240, Door×1144, Furniture Office×1104, Shelves×1028, …; renderers 88 (188 material slots): Tool bench×16, DoorHead×12, Furniture_Basin×12, Wall×12, Office desk×9, RoomLight×8, Crate×4, Door×4, Shelves×3, Filing cabinet×2, …; lights: RoomLight×4, Office desk×1
- `28_postoffice_2589948132.png`: pieces 96: Sorting case×25, Wall×12, Chair×11, Crate×6, DoorHead×6, RoomLight×4, stall×3, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Lockers×2, Shelves×2, Bookcase×1, …; vertices 86,292: Sorting case×28728, Furniture×10740, DoorHead×5208, Stove×4565, Wall×4384, Floors×4252, Furniture_Basin×4186, post boxes×3864, Furniture Lobby×2160, Office desk×2038, …; renderers 153 (308 material slots): Sorting case×25, Wall×21, DoorHead×18, Stove×14, Furniture_Basin×12, Office desk×9, RoomLight×8, Crate×6, Furniture_Fridge×5, Door×4, …; lights: RoomLight×4, Furniture_Fridge×1, Office desk×1, Stove×1
- `29_funeral_1807972230.png`: pieces 68: Chairs×28, Wall×6, RoomLight×4, Shelves×4, DoorHead×3, Filing cabinet×3, stall×3, Desk×2, Door×2, Floors×2, Furniture Stock Room×2, Furniture_Basin×2, Bookcase×1, Colliders×1, …; vertices 35,895: Furniture Chapel×10368, Furniture_Basin×4186, Furniture×3592, Floors×2922, DoorHead×2604, Office desk×1892, Shelves×1724, Wall×1624, Furniture Stock Room×1446, Furniture Office×1224, …; renderers 77 (174 material slots): Furniture_Basin×12, Wall×11, DoorHead×9, Office desk×9, RoomLight×8, Desk×4, Door×4, Filing cabinet×4, Shelves×4, stall×3, …; lights: RoomLight×4, Office desk×1

</details>

## Streamed: the statewide map's lazy interiors

Piece 11913_9092 of Bardstown (Full) built in 67 ms: 0 interiors in it as built (none, as it should be), 8 buildings' plans left with the streamer; the plans took 15.3 ms over 6 steps (slowest 4.6 ms).

Furnished round a survivor at the piece's middle: 6 buildings in 103 frames (1.7 s at 60 fps), 6 standing, 0 failed; the slowest frame's furnishing took 10.2 ms (budget 3 ms, plus the step that crosses it). 6 interiors furnished (0 failed, 0 taken down), 6 standing, 8 known.

Loot memory: building 1915619802 (Motel, 18 containers): the first carried off, the second emptied; walked 2 km away (taken down) and back (furnished again): 18 containers, the carried-off one gone, the emptied one still empty, 16 of 16 others as they were. **OK**.

Determinism: the piece built again and building 1915619802 furnished again: identical (objects, names, places, meshes). **OK**.

Plan steps in downtown Louisville's pieces (a step runs until the site's slice is spent, then finishes the plan or the wall under way):

| piece | plans ms in all | steps | slowest step ms | slowest single piece of work |
|---|--:|--:|--:|---|
| 11711_9477 | 20.4 | 6 | 6.6 | 5.0 ms: the plan of Civic 662565482 (6244 m², 43 rooms, 28 corners, 127 windows) |
| 11712_9477 | 8.8 | 3 | 4.0 | 3.9 ms: the plan of Bank 662565810 (1425 m², 8 rooms, 8 corners, 22 windows) |
| 11713_9477 | 5.7 | 2 | 4.5 | 4.4 ms: the plan of Bank 9295913 (4278 m², 9 rooms, 9 corners, 83 windows) |
| 11711_9478 | 20.9 | 4 | 10.5 | 9.1 ms: the plan of Civic 662565944 (1947 m², 25 rooms, 31 corners, 55 windows) |
| 11712_9478 | 26.9 | 7 | 6.7 | 6.6 ms: the plan of Civic 662565612 (2224 m², 29 rooms, 46 corners, 69 windows) |
| 11713_9478 | 29.7 | 10 | 7.7 | 7.5 ms: the plan of Warehouse 662566094 (4917 m², 6 rooms, 10 corners, 49 windows) |
| 11711_9479 | 8.7 | 3 | 5.6 | 4.3 ms: the plan of Motel 1291039792 (1680 m², 44 rooms, 6 corners, 38 windows) |
| 11712_9479 | 13.9 | 6 | 3.2 | 2.6 ms: the plan of Warehouse 662564942 (621 m², 7 rooms, 20 corners, 28 windows) |
| 11713_9479 | 18.7 | 5 | 7.2 | 7.0 ms: the plan of Warehouse 662565798 (4744 m², 6 rooms, 15 corners, 52 windows) |

Slowest plan step in these pieces: 10.5 ms.


## Log

0 errors, 0 warnings logged during the run.

## Stages

- setup: ok, 32 ms
- square: Bardstown: ok, 18,005 ms
- discard: Bardstown: ok, 244 ms
- square: Princeton: ok, 5,888 ms
- discard: Princeton: ok, 195 ms
- square: Pikeville: ok, 3,762 ms
- discard: Pikeville: ok, 150 ms
- square: Middlesboro: ok, 1,881 ms
- discard: Middlesboro: ok, 63 ms
- summary by use: ok, 12 ms
- streamed pieces: ok, 1,595 ms

**Exit 0** after 31.8 s.
