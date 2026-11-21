# CLONE-Z Introduction town harness

Zone `intro`, layout seed 0. `RaidMapBuilder.Build` 1787 ms; NavMesh bake 71 ms.

`Raid map 0: 11 buildings, 355 containers, 772 fixtures, 79 lights, 4277 objects, 12 ceilings`

Map half-size 70.0 m; spawn (16.6, 15.0) facing 0°.

## Checks

- **FAILED** no path from the spawn to the exit Road Block (path PathPartial)
- **FAILED** no path from the spawn to the exit Rail Bridge (path PathPartial)
- **FAILED** no path from the spawn to the exit Army Checkpoint (path PathPartial)
- **FAILED** no path from the spawn to the exit Boat Dock (path PathPartial)
- **FAILED** NationalGuard: 3 of its 3 outside doors can't be walked to
- **FAILED** CountyOffice: 3 of its 3 outside doors can't be walked to
- **FAILED** FoodMart: 2 of its 2 outside doors can't be walked to
- **FAILED** Depot: 4 of its 4 outside doors can't be walked to
- **FAILED** Clinic: 2 of its 2 outside doors can't be walked to
- **FAILED** QuarantineTent: 1 of its 1 outside doors can't be walked to
- **FAILED** House_Elm: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak: 2 of its 2 outside doors can't be walked to
- **FAILED** Cottage_North: 2 of its 2 outside doors can't be walked to
- **FAILED** House_East: 2 of its 2 outside doors can't be walked to
- **FAILED** Cottage_West: 2 of its 2 outside doors can't be walked to
- **FAILED** 45 of 46 rooms can't be walked to from the spawn

NavMesh: 3563 triangles, 17523 m² walkable. Outside doors reached 0/25, rooms reached 1/46.

Unreachable:

- door outside NationalGuard at (13.4, -20.5): path PathPartial
- door outside NationalGuard at (24.6, -34.6): path PathPartial
- door outside NationalGuard at (30.6, -18.0): path PathPartial
- room NationalGuard/Operations: path PathPartial
- room NationalGuard/Armory: path PathPartial
- room NationalGuard/Barracks: path PathPartial
- room NationalGuard/Storeroom: path PathPartial
- room NationalGuard/CO Office: path PathPartial
- door outside CountyOffice at (25.5, 7.4): path PathPartial
- door outside CountyOffice at (25.0, 24.6): path PathPartial
- door outside CountyOffice at (7.4, 13.0): path PathPartial
- room CountyOffice/Lobby: path PathPartial
- room CountyOffice/Break Room: path PathPartial
- room CountyOffice/Manager: path PathPartial
- room CountyOffice/Records: path PathPartial
- room CountyOffice/Utility: path PathPartial
- door outside FoodMart at (11.4, 48.5): path PathPartial
- door outside FoodMart at (24.6, 51.0): path PathPartial
- room FoodMart/Store: path PathPartial
- room FoodMart/Stock Room: path PathPartial
- room FoodMart/Restroom: path PathPartial
- door outside Depot at (46.0, 32.4): path PathPartial
- door outside Depot at (51.5, 32.4): path PathPartial
- door outside Depot at (57.5, 32.4): path PathPartial
- door outside Depot at (44.0, 51.6): path PathPartial
- room Depot/Warehouse: path PathPartial
- room Depot/Garage: path PathPartial
- room Depot/Site Office: path PathPartial
- door outside Clinic at (-16.5, -10.4): path PathPartial
- door outside Clinic at (-27.5, -25.6): path PathPartial
- room Clinic/Waiting Room: path PathPartial
- room Clinic/Exam Room: path PathPartial
- room Clinic/Doctor's Office: path PathPartial
- room Clinic/Ward: path PathPartial
- room Clinic/Pharmacy: path PathPartial
- door outside QuarantineTent at (-24.0, -33.9): path PathPartial
- room QuarantineTent/Tent: path PathPartial
- door outside House_Elm at (-23.0, 9.4): path PathPartial
- door outside House_Elm at (-12.4, 14.5): path PathPartial
- room House_Elm/Living Room: path PathPartial
- room House_Elm/Kitchen: path PathPartial
- room House_Elm/Bedroom: path PathPartial
- room House_Elm/Bathroom: path PathPartial
- room House_Elm/Den: path PathPartial
- door outside House_Oak at (-35.0, 9.4): path PathPartial
- door outside House_Oak at (-45.6, 14.5): path PathPartial
- room House_Oak/Living Room: path PathPartial
- room House_Oak/Kitchen: path PathPartial
- room House_Oak/Bedroom: path PathPartial
- room House_Oak/Bathroom: path PathPartial
- room House_Oak/Den: path PathPartial
- door outside Cottage_North at (-12.9, 36.5): path PathPartial
- door outside Cottage_North at (-21.5, 44.6): path PathPartial
- room Cottage_North/Living Room: path PathPartial
- room Cottage_North/Bedroom: path PathPartial
- room Cottage_North/Bathroom: no NavMesh inside
- room Cottage_North/Laundry: path PathPartial
- door outside House_East at (47.0, -9.4): path PathPartial
- door outside House_East at (36.4, -14.5): path PathPartial
- room House_East/Living Room: path PathPartial
- room House_East/Kitchen: path PathPartial
- room House_East/Bedroom: path PathPartial
- room House_East/Bathroom: path PathPartial
- room House_East/Den: path PathPartial
- door outside Cottage_West at (-51.5, -9.9): path PathPartial
- door outside Cottage_West at (-43.4, -18.5): path PathPartial
- room Cottage_West/Living Room: path PathPartial
- room Cottage_West/Bedroom: path PathPartial
- room Cottage_West/Bathroom: no NavMesh inside
- room Cottage_West/Laundry: path PathPartial

## Exits

| exit | this raid | where | walk from the spawn |
|---|---|---|--:|
| Road Block | open | (0.0, 63.0) | **path PathPartial** |
| Rail Bridge | closed | (63.0, 0.0) | **path PathPartial** |
| Army Checkpoint | closed | (0.0, -63.0) | **path PathPartial** |
| Boat Dock | open | (-63.0, 0.0) | **path PathPartial** |

## Buildings

| # | name | kind | footprint | size (m) | rooms | outside doors reached | rooms reached |
|--:|---|---|---|---|--:|--:|--:|
| 1 | NationalGuard | Army | (14.9, -33.1) – (29.1, -14.9) | 14.2 × 18.2 | 5 | 0/3 | 0/5 |
| 2 | CountyOffice | Office | (8.9, 8.9) – (29.1, 23.1) | 20.2 × 14.2 | 6 | 0/3 | 1/6 |
| 3 | FoodMart | Store | (12.9, 38.9) – (23.1, 53.1) | 10.2 × 14.2 | 3 | 0/2 | 0/3 |
| 4 | Depot | Depot | (38.9, 33.9) – (61.1, 50.1) | 22.2 × 16.2 | 3 | 0/4 | 0/3 |
| 5 | Clinic | Clinic | (-31.1, -24.1) – (-12.9, -11.9) | 18.2 × 12.2 | 5 | 0/2 | 0/5 |
| 6 | QuarantineTent | Tent | (-27.1, -44.6) – (-20.9, -35.4) | 6.2 × 9.2 | 1 | 0/1 | 0/1 |
| 7 | House_Elm | House | (-26.1, 10.9) – (-13.9, 21.1) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 8 | House_Oak | House | (-44.1, 10.9) – (-31.9, 21.1) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 9 | Cottage_North | House | (-23.6, 32.9) – (-14.4, 43.1) | 9.2 × 10.2 | 4 | 0/2 | 0/4 |
| 10 | House_East | House | (37.9, -21.1) – (50.1, -10.9) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 11 | Cottage_West | House | (-55.1, -20.6) – (-44.9, -11.4) | 10.2 × 9.2 | 4 | 0/2 | 0/4 |

## What the town is made of

| root | objects | renderers | meshes | vertices | triangles | materials | lights | colliders |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| world | 4237 | 2249 | 2134 | 990817 | 620454 | 103 | 116 | 1499 |
| overhead | 16 | 12 | 12 | 12127 | 9608 | 7 | 0 | 48 |
| scenery | 24 | 22 | 22 | 43446 | 32970 | 7 | 0 | 0 |
| total | 4277 | 2283 | 2168 | 1046390 | 663032 | 105 | 116 | 1547 |

## Log

0 errors, 0 warnings logged during the run.


## Pictures

- plan_day.png
- plan_night.png
- town_day.png
- town_night.png
- grid_nw_day.png
- grid_nw_night.png
- grid_nc_day.png
- grid_nc_night.png
- grid_ne_day.png
- grid_ne_night.png
- grid_mw_day.png
- grid_mw_night.png
- grid_mc_day.png
- grid_mc_night.png
- grid_me_day.png
- grid_me_night.png
- grid_sw_day.png
- grid_sw_night.png
- grid_sc_day.png
- grid_sc_night.png
- grid_se_day.png
- grid_se_night.png
- bldg_01_nationalguard_day.png
- bldg_01_nationalguard_night.png
- bldg_02_countyoffice_day.png
- bldg_02_countyoffice_night.png
- bldg_03_foodmart_day.png
- bldg_03_foodmart_night.png
- bldg_04_depot_day.png
- bldg_04_depot_night.png
- bldg_05_clinic_day.png
- bldg_05_clinic_night.png
- bldg_06_quarantinetent_day.png
- bldg_06_quarantinetent_night.png
- bldg_07_house_elm_day.png
- bldg_07_house_elm_night.png
- bldg_08_house_oak_day.png
- bldg_08_house_oak_night.png
- bldg_09_cottage_north_day.png
- bldg_09_cottage_north_night.png
- bldg_10_house_east_day.png
- bldg_10_house_east_night.png
- bldg_11_cottage_west_day.png
- bldg_11_cottage_west_night.png
- exit_road_block_day.png
- exit_rail_bridge_day.png
- exit_army_checkpoint_day.png
- exit_boat_dock_day.png
- spawn_day.png
- spawn_night.png

## Stages

- setup: ok, 21 ms
- build the town: ok, 1788 ms
- count: ok, 12 ms
- navmesh and paths: ok, 83 ms
- pictures: ok, 12229 ms

**Exit 2** after 14.2 s.
