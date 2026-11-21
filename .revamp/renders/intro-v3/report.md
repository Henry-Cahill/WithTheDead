# CLONE-Z Introduction town harness

Zone `intro`, layout seed 0. `RaidMapBuilder.Build` 2059 ms; NavMesh bake 73 ms.

`Raid map 0: 16 buildings, 461 containers, 1117 fixtures, 118 lights, 5557 objects, 17 ceilings`

Map half-size 70.0 m; spawn (19.0, -16.0) facing 45°.

## Checks

- **FAILED** 2 of 69 rooms can't be walked to from the spawn

NavMesh (every door open: 29 carvers off): 3901 triangles, 16432 m² walkable. Outside doors reached 35/35, rooms reached 67/69.

Unreachable:

- room House_Oak_110/Bedroom: path PathPartial
- room House_Oak_110/Bathroom: path PathPartial

## Exits

| exit | this raid | where | walk from the spawn |
|---|---|---|--:|
| Road Block | open | (0.0, 63.0) | 85 m |
| Rail Bridge | open | (63.0, 0.0) | 47 m |
| Army Checkpoint | open | (0.0, -63.0) | 55 m |
| Boat Dock | closed | (-63.0, 0.0) | 84 m |

## Buildings

| # | name | kind | footprint | size (m) | rooms | outside doors reached | rooms reached |
|--:|---|---|---|---|--:|--:|--:|
| 1 | CountyOffice | Office | (9.4, 10.4) – (29.6, 24.6) | 20.2 × 14.2 | 6 | 3/3 | 6/6 |
| 2 | FoodMart | Store | (14.9, 41.9) – (25.1, 56.1) | 10.2 × 14.2 | 3 | 2/2 | 3/3 |
| 3 | Depot | Depot | (39.9, 41.9) – (62.1, 58.1) | 22.2 × 16.2 | 3 | 4/4 | 3/3 |
| 4 | NationalGuard | Army | (13.9, -28.1) – (28.1, -9.9) | 14.2 × 18.2 | 5 | 3/3 | 5/5 |
| 5 | QuarantineTent | Tent | (42.4, -30.6) – (51.6, -24.4) | 9.2 × 6.2 | 1 | 1/1 | 1/1 |
| 6 | Clinic | Clinic | (-26.6, -21.6) – (-8.4, -9.4) | 18.2 × 12.2 | 5 | 2/2 | 5/5 |
| 7 | House_Elm_105 | House | (-23.1, 47.8) – (-10.9, 57.9) | 12.2 × 10.2 | 5 | 2/2 | 5/5 |
| 8 | House_Elm_109 | House | (-39.6, 47.8) – (-29.4, 56.9) | 10.2 × 9.2 | 4 | 2/2 | 4/4 |
| 9 | House_Elm_113 | House | (-58.1, 47.8) – (-45.9, 57.9) | 12.2 × 10.2 | 5 | 2/2 | 5/5 |
| 10 | House_Elm_110 | House | (-43.1, 18.0) – (-30.9, 28.3) | 12.2 × 10.2 | 5 | 2/2 | 5/5 |
| 11 | House_Elm_114 | House | (-59.1, 19.0) – (-48.9, 28.3) | 10.2 × 9.2 | 4 | 2/2 | 4/4 |
| 12 | House_Oak_109 | House | (-44.1, -28.3) – (-31.9, -18.0) | 12.2 × 10.2 | 5 | 2/2 | 5/5 |
| 13 | House_Oak_113 | House | (-60.6, -28.3) – (-50.4, -19.0) | 10.2 × 9.2 | 4 | 2/2 | 4/4 |
| 14 | House_Oak_106 | House | (-22.1, -56.9) – (-11.9, -47.8) | 10.2 × 9.2 | 4 | 2/2 | 4/4 |
| 15 | House_Oak_110 | House | (-40.6, -57.9) – (-28.4, -47.8) | 12.2 × 10.2 | 5 | 2/2 | 3/5 |
| 16 | House_Oak_114 | House | (-58.1, -57.9) – (-45.9, -47.8) | 12.2 × 10.2 | 5 | 2/2 | 5/5 |

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
- grid_nw_day.png
- grid_nc_day.png
- grid_ne_day.png
- grid_mw_day.png
- grid_mc_day.png
- grid_me_day.png
- grid_sw_day.png
- grid_sc_day.png
- grid_se_day.png
- bldg_01_countyoffice_day.png
- bldg_02_foodmart_day.png
- bldg_03_depot_day.png
- bldg_04_nationalguard_day.png
- bldg_05_quarantinetent_day.png
- bldg_06_clinic_day.png
- bldg_07_house_elm_105_day.png
- bldg_08_house_elm_109_day.png
- bldg_09_house_elm_113_day.png
- bldg_10_house_elm_110_day.png
- bldg_11_house_elm_114_day.png
- bldg_12_house_oak_109_day.png
- bldg_13_house_oak_113_day.png
- bldg_14_house_oak_106_day.png
- bldg_15_house_oak_110_day.png
- bldg_16_house_oak_114_day.png
- exit_road_block_day.png
- exit_rail_bridge_day.png
- exit_army_checkpoint_day.png
- exit_boat_dock_day.png
- spawn_day.png
- spawn_night.png

## Stages

- setup: ok, 20 ms
- build the town: ok, 2060 ms
- count: ok, 17 ms
- navmesh and paths: ok, 110 ms
- pictures: ok, 6577 ms

**Exit 2** after 8.8 s.
