# CLONE-Z Introduction town harness

Zone `intro`, layout seed 0. `RaidMapBuilder.Build` 2060 ms; NavMesh bake 71 ms.

`Raid map 0: 16 buildings, 461 containers, 1117 fixtures, 118 lights, 5557 objects, 17 ceilings`

Map half-size 70.0 m; spawn (19.0, -16.0) facing 45°.

## Checks

- **FAILED** no path from the spawn to the exit Road Block (path PathPartial, gets to (17.0, -10.7))
- **FAILED** no path from the spawn to the exit Rail Bridge (path PathPartial, gets to (21.7, -11.2))
- **FAILED** no path from the spawn to the exit Army Checkpoint (path PathPartial, gets to (14.8, -19.3))
- **FAILED** no path from the spawn to the exit Boat Dock (path PathPartial, gets to (14.8, -11.5))
- **FAILED** CountyOffice: 3 of its 3 outside doors can't be walked to
- **FAILED** FoodMart: 2 of its 2 outside doors can't be walked to
- **FAILED** Depot: 4 of its 4 outside doors can't be walked to
- **FAILED** NationalGuard: 3 of its 3 outside doors can't be walked to
- **FAILED** QuarantineTent: 1 of its 1 outside doors can't be walked to
- **FAILED** Clinic: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Elm_105: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Elm_109: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Elm_113: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Elm_110: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Elm_114: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak_109: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak_113: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak_106: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak_110: 2 of its 2 outside doors can't be walked to
- **FAILED** House_Oak_114: 2 of its 2 outside doors can't be walked to
- **FAILED** 68 of 69 rooms can't be walked to from the spawn

NavMesh (every door open: 29 carvers off): 3175 triangles, 16317 m² walkable. Outside doors reached 0/35, rooms reached 1/69.

Unreachable:

- door outside CountyOffice at (26.0, 8.9): path PathPartial, gets to (19.8, -10.7)
- door outside CountyOffice at (25.5, 26.1): path PathPartial, gets to (19.8, -10.7)
- door outside CountyOffice at (7.9, 14.5): path PathPartial, gets to (16.3, -10.8)
- room CountyOffice/Open Office: path PathPartial
- room CountyOffice/Lobby: path PathPartial
- room CountyOffice/Break Room: path PathPartial
- room CountyOffice/Manager: path PathPartial
- room CountyOffice/Records: path PathPartial
- room CountyOffice/Utility: path PathPartial
- door outside FoodMart at (13.4, 51.5): path PathPartial, gets to (17.0, -10.7)
- door outside FoodMart at (26.6, 54.0): path PathPartial, gets to (19.8, -10.7)
- room FoodMart/Store: path PathPartial
- room FoodMart/Stock Room: path PathPartial
- room FoodMart/Restroom: path PathPartial
- door outside Depot at (47.0, 40.4): path PathPartial, gets to (21.7, -11.2)
- door outside Depot at (52.5, 40.4): path PathPartial, gets to (21.7, -11.2)
- door outside Depot at (58.5, 40.4): path PathPartial, gets to (21.7, -11.2)
- door outside Depot at (45.0, 59.6): path PathPartial, gets to (21.7, -11.2)
- room Depot/Warehouse: path PathPartial
- room Depot/Garage: path PathPartial
- room Depot/Site Office: path PathPartial
- door outside NationalGuard at (12.4, -15.5): path PathPartial, gets to (14.7, -15.6)
- door outside NationalGuard at (23.6, -29.6): path PathPartial, gets to (22.3, -19.3)
- door outside NationalGuard at (29.6, -13.0): path PathPartial, gets to (22.3, -15.0)
- room NationalGuard/Armory: path PathPartial
- room NationalGuard/Barracks: path PathPartial
- room NationalGuard/Storeroom: path PathPartial
- room NationalGuard/CO Office: path PathPartial
- door outside QuarantineTent at (40.9, -27.5): path PathPartial, gets to (22.3, -19.3)
- room QuarantineTent/Tent: path PathPartial
- door outside Clinic at (-12.0, -7.9): path PathPartial, gets to (14.8, -11.5)
- door outside Clinic at (-23.0, -23.1): path PathPartial, gets to (14.8, -19.3)
- room Clinic/Waiting Room: path PathPartial
- room Clinic/Exam Room: path PathPartial
- room Clinic/Doctor's Office: path PathPartial
- room Clinic/Ward: path PathPartial
- room Clinic/Pharmacy: path PathPartial
- door outside House_Elm_105 at (-20.0, 46.3): path PathPartial, gets to (14.8, -11.5)
- door outside House_Elm_105 at (-9.4, 51.3): path PathPartial, gets to (16.3, -10.8)
- room House_Elm_105/Living Room: path PathPartial
- room House_Elm_105/Kitchen: path PathPartial
- room House_Elm_105/Bedroom: path PathPartial
- room House_Elm_105/Bathroom: path PathPartial
- room House_Elm_105/Den: path PathPartial
- door outside House_Elm_109 at (-36.0, 46.3): path PathPartial, gets to (14.8, -11.5)
- door outside House_Elm_109 at (-27.9, 54.8): path PathPartial, gets to (14.8, -11.5)
- room House_Elm_109/Living Room: path PathPartial
- room House_Elm_109/Bedroom: path PathPartial
- room House_Elm_109/Bathroom: no NavMesh inside
- room House_Elm_109/Laundry: path PathPartial
- door outside House_Elm_113 at (-55.0, 46.3): path PathPartial, gets to (14.8, -11.5)
- door outside House_Elm_113 at (-44.4, 51.3): path PathPartial, gets to (14.8, -11.5)
- room House_Elm_113/Living Room: path PathPartial
- room House_Elm_113/Kitchen: path PathPartial
- room House_Elm_113/Bedroom: path PathPartial
- room House_Elm_113/Bathroom: path PathPartial
- room House_Elm_113/Den: path PathPartial
- door outside House_Elm_110 at (-40.0, 29.8): path PathPartial, gets to (14.8, -11.5)
- door outside House_Elm_110 at (-29.4, 24.6): path PathPartial, gets to (14.8, -11.5)
- room House_Elm_110/Living Room: path PathPartial
- room House_Elm_110/Kitchen: path PathPartial
- room House_Elm_110/Bedroom: path PathPartial
- room House_Elm_110/Bathroom: path PathPartial
- room House_Elm_110/Den: path PathPartial
- door outside House_Elm_114 at (-55.5, 29.8): path PathPartial, gets to (14.8, -11.5)
- door outside House_Elm_114 at (-47.4, 21.1): path PathPartial, gets to (14.8, -11.5)
- room House_Elm_114/Living Room: path PathPartial
- room House_Elm_114/Bedroom: path PathPartial
- room House_Elm_114/Bathroom: no NavMesh inside
- room House_Elm_114/Laundry: path PathPartial
- door outside House_Oak_109 at (-41.0, -29.8): path PathPartial, gets to (14.8, -19.3)
- door outside House_Oak_109 at (-30.4, -24.6): path PathPartial, gets to (14.8, -19.3)
- room House_Oak_109/Living Room: path PathPartial
- room House_Oak_109/Kitchen: path PathPartial
- room House_Oak_109/Bedroom: path PathPartial
- room House_Oak_109/Bathroom: path PathPartial
- room House_Oak_109/Den: path PathPartial
- door outside House_Oak_113 at (-57.0, -29.8): path PathPartial, gets to (14.8, -19.3)
- door outside House_Oak_113 at (-48.9, -21.1): path PathPartial, gets to (14.7, -16.5)
- room House_Oak_113/Living Room: path PathPartial
- room House_Oak_113/Bedroom: path PathPartial
- room House_Oak_113/Bathroom: no NavMesh inside
- room House_Oak_113/Laundry: path PathPartial
- door outside House_Oak_106 at (-18.5, -46.3): path PathPartial, gets to (14.8, -19.3)
- door outside House_Oak_106 at (-10.4, -54.8): path PathPartial, gets to (14.8, -19.3)
- room House_Oak_106/Living Room: path PathPartial
- room House_Oak_106/Bedroom: path PathPartial
- room House_Oak_106/Bathroom: no NavMesh inside
- room House_Oak_106/Laundry: path PathPartial
- door outside House_Oak_110 at (-37.5, -46.3): path PathPartial, gets to (14.8, -19.3)
- door outside House_Oak_110 at (-26.9, -51.3): path PathPartial, gets to (14.8, -19.3)
- room House_Oak_110/Living Room: path PathPartial
- room House_Oak_110/Kitchen: path PathPartial
- room House_Oak_110/Bedroom: path PathPartial
- room House_Oak_110/Bathroom: path PathPartial
- room House_Oak_110/Den: path PathPartial
- door outside House_Oak_114 at (-55.0, -46.3): path PathPartial, gets to (14.8, -19.3)
- door outside House_Oak_114 at (-44.4, -51.3): path PathPartial, gets to (14.8, -19.3)
- room House_Oak_114/Living Room: path PathPartial
- room House_Oak_114/Kitchen: path PathPartial
- room House_Oak_114/Bedroom: path PathPartial
- room House_Oak_114/Bathroom: path PathPartial
- room House_Oak_114/Den: path PathPartial

Doorways that block:

- CountyOffice door at (26.0, 10.5): NavMesh inside 0.3 m off, in the doorway 0.8 m off, outside 0.2 m off; baked in the doorway: Bldg_CountyOffice/DoorHead (24.4, 10.3)–(27.6, 10.6) y 0.0..2.8; Bldg_CountyOffice/Floors (9.4, 10.4)–(29.4, 24.6) y 0.0..0.3; OutdoorProps (4.0, 4.0)–(34.3, 30.2) y -1.0..8.1;
- CountyOffice door at (25.5, 24.5): NavMesh inside 0.1 m off, in the doorway 0.7 m off, outside 0.2 m off; baked in the doorway: Bldg_CountyOffice/DoorHead (24.7, 24.4)–(26.3, 24.6) y 0.0..2.8; Bldg_CountyOffice/Furniture (9.8, 10.7)–(29.2, 24.4) y 0.0..1.2; Bldg_CountyOffice/Floors (9.4, 10.4)–(29.4, 24.6) y 0.0..0.3; OutdoorProps (4.0, 4.0)–(34.3, 30.2) y -1.0..8.1;
- CountyOffice door at (9.5, 14.5): NavMesh inside 0.1 m off, in the doorway 0.7 m off, outside 0.2 m off; baked in the doorway: Bldg_CountyOffice/DoorHead (9.4, 13.7)–(9.6, 15.3) y 0.0..2.8; Bldg_CountyOffice/Floors (9.4, 10.4)–(29.4, 24.6) y 0.0..0.3; OutdoorProps (4.0, 4.0)–(34.3, 30.2) y -1.0..8.1;
- FoodMart door at (15.0, 51.5): NavMesh inside 0.1 m off, in the doorway 0.7 m off, outside 0.2 m off; baked in the doorway: Bldg_FoodMart/DoorHead (14.8, 50.4)–(15.1, 52.6) y 0.0..2.8; OutdoorProps (1.1, 33.2)–(27.6, 68.4) y -0.2..5.5;

## Exits

| exit | this raid | where | walk from the spawn |
|---|---|---|--:|
| Road Block | open | (0.0, 63.0) | **path PathPartial, gets to (17.0, -10.7)** |
| Rail Bridge | open | (63.0, 0.0) | **path PathPartial, gets to (21.7, -11.2)** |
| Army Checkpoint | open | (0.0, -63.0) | **path PathPartial, gets to (14.8, -19.3)** |
| Boat Dock | closed | (-63.0, 0.0) | **path PathPartial, gets to (14.8, -11.5)** |

## Buildings

| # | name | kind | footprint | size (m) | rooms | outside doors reached | rooms reached |
|--:|---|---|---|---|--:|--:|--:|
| 1 | CountyOffice | Office | (9.4, 10.4) – (29.6, 24.6) | 20.2 × 14.2 | 6 | 0/3 | 0/6 |
| 2 | FoodMart | Store | (14.9, 41.9) – (25.1, 56.1) | 10.2 × 14.2 | 3 | 0/2 | 0/3 |
| 3 | Depot | Depot | (39.9, 41.9) – (62.1, 58.1) | 22.2 × 16.2 | 3 | 0/4 | 0/3 |
| 4 | NationalGuard | Army | (13.9, -28.1) – (28.1, -9.9) | 14.2 × 18.2 | 5 | 0/3 | 1/5 |
| 5 | QuarantineTent | Tent | (42.4, -30.6) – (51.6, -24.4) | 9.2 × 6.2 | 1 | 0/1 | 0/1 |
| 6 | Clinic | Clinic | (-26.6, -21.6) – (-8.4, -9.4) | 18.2 × 12.2 | 5 | 0/2 | 0/5 |
| 7 | House_Elm_105 | House | (-23.1, 47.8) – (-10.9, 57.9) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 8 | House_Elm_109 | House | (-39.6, 47.8) – (-29.4, 56.9) | 10.2 × 9.2 | 4 | 0/2 | 0/4 |
| 9 | House_Elm_113 | House | (-58.1, 47.8) – (-45.9, 57.9) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 10 | House_Elm_110 | House | (-43.1, 18.0) – (-30.9, 28.3) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 11 | House_Elm_114 | House | (-59.1, 19.0) – (-48.9, 28.3) | 10.2 × 9.2 | 4 | 0/2 | 0/4 |
| 12 | House_Oak_109 | House | (-44.1, -28.3) – (-31.9, -18.0) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 13 | House_Oak_113 | House | (-60.6, -28.3) – (-50.4, -19.0) | 10.2 × 9.2 | 4 | 0/2 | 0/4 |
| 14 | House_Oak_106 | House | (-22.1, -56.9) – (-11.9, -47.8) | 10.2 × 9.2 | 4 | 0/2 | 0/4 |
| 15 | House_Oak_110 | House | (-40.6, -57.9) – (-28.4, -47.8) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |
| 16 | House_Oak_114 | House | (-58.1, -57.9) – (-45.9, -47.8) | 12.2 × 10.2 | 5 | 0/2 | 0/5 |

## What the town is made of

| root | objects | renderers | meshes | vertices | triangles | materials | lights | colliders |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| world | 5510 | 3366 | 3179 | 1200268 | 764102 | 112 | 188 | 1530 |
| overhead | 21 | 17 | 17 | 15240 | 12764 | 7 | 0 | 71 |
| scenery | 26 | 24 | 24 | 44002 | 32992 | 7 | 0 | 0 |
| total | 5557 | 3407 | 3220 | 1259510 | 809858 | 114 | 188 | 1601 |

## Log

0 errors, 1 warnings logged during the run.

- warning: `[CloneZ] Raid map layout notes (5): no room for a street light on Main St at -18 | no room for a street light on Main St at 50 | no room for a street light on Depot Rd at 60 | no room for a street light on Oak St at -45 | no room for a street light on Oak St at -29 Raid map 0: 16 buildings, 461 containers, 1117 fixtures, 118 lights, 5557 objects, 17 ceilings`

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
- bldg_01_countyoffice_day.png
- bldg_01_countyoffice_night.png
- bldg_02_foodmart_day.png
- bldg_02_foodmart_night.png
- bldg_03_depot_day.png
- bldg_03_depot_night.png
- bldg_04_nationalguard_day.png
- bldg_04_nationalguard_night.png
- bldg_05_quarantinetent_day.png
- bldg_05_quarantinetent_night.png
- bldg_06_clinic_day.png
- bldg_06_clinic_night.png
- bldg_07_house_elm_105_day.png
- bldg_07_house_elm_105_night.png
- bldg_08_house_elm_109_day.png
- bldg_08_house_elm_109_night.png
- bldg_09_house_elm_113_day.png
- bldg_09_house_elm_113_night.png
- bldg_10_house_elm_110_day.png
- bldg_10_house_elm_110_night.png
- bldg_11_house_elm_114_day.png
- bldg_11_house_elm_114_night.png
- bldg_12_house_oak_109_day.png
- bldg_12_house_oak_109_night.png
- bldg_13_house_oak_113_day.png
- bldg_13_house_oak_113_night.png
- bldg_14_house_oak_106_day.png
- bldg_14_house_oak_106_night.png
- bldg_15_house_oak_110_day.png
- bldg_15_house_oak_110_night.png
- bldg_16_house_oak_114_day.png
- bldg_16_house_oak_114_night.png
- exit_road_block_day.png
- exit_rail_bridge_day.png
- exit_army_checkpoint_day.png
- exit_boat_dock_day.png
- spawn_day.png
- spawn_night.png

## Stages

- setup: ok, 20 ms
- build the town: ok, 2062 ms
- count: ok, 13 ms
- navmesh and paths: ok, 94 ms
- pictures: ok, 9756 ms

**Exit 2** after 12.0 s.
