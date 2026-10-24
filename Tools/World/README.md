# CLONE-Z world

CLONE-Z's raid map is being rebuilt from a real place: the state of Kentucky, from OpenStreetMap's streets, buildings
and land use, organised the way survival games like Project Zomboid organise a world (256 m cells, buildings with
rooms on floors, zones). Only the structure is borrowed. The content comes from OpenStreetMap (© OpenStreetMap
contributors, ODbL), and CLONE-Z's own art and rules turn it into the game. Data from other games, including the map
scrape in `Tools/MapScraper`, is never used. The whole state shares one flat grid in metres: Kentucky's official
statewide projection (Kentucky Single Zone, EPSG:3088), so streets and buildings keep their true shapes.

A whole state is a lot for one machine, so the world is built by several (the Spark and the game servers). They
share their work through the **world store** on the NAS.

## The world store

| | |
|---|---|
| Windows (this PC) | `\\192.168.1.170\CLONE-Z` |
| Linux (the Spark, 0htsserver, 1htsserver, hts-llm) | `/mnt/clonez` |

It's a shared folder of its own on the Synology NAS (`/volume2/CLONE-Z`). The Linux machines mount it over NFS 4.1;
the NAS lets in only the machines named in the folder's NFS rules and maps every user on them to its admin, so
files written by any machine can be changed by all the others and from Windows.

| Folder | What's in it | Who writes it |
|---|---|---|
| `world/sources/osm/` | the OpenStreetMap extract for Kentucky | the world builder |
| `world/builds/<id>/` | one build of the world: its `manifest.json` and its regions | the machines building it, each only its own regions |
| `world/current.json` | which build the game and the servers load | whoever publishes a build |
| `machines/<name>.json` | what each machine is doing right now | that machine |
| `scratch/<name>/` | each machine's temporary files | that machine |

Rules for anything that writes there: write under a temporary name and rename it when it's complete (so nobody reads
half a file), and leave other machines' files alone.

## Connecting a machine

1. In DSM: Control Panel → Shared Folder → CLONE-Z → Edit → NFS Permissions → Create: the machine's IP, Read/Write,
   squash "Map all users to admin", security sys, asynchronous on, non-privileged ports off, mounted subfolders on.
2. On this PC: `.\World.ps1 connect -Machines user@ip`. It copies [connect-nas.sh](connect-nas.sh) over and runs it
   with sudo (asking for the machine's password once). The script installs the NFS client if needed, mounts the
   store at `/mnt/clonez` on first use and adds it to `/etc/fstab`, so it comes back after a reboot.

`.\World.ps1 status` shows which machines have the store mounted, the builds in the store and what each machine is
doing.

## Building the world

1. **The source.** `world/sources/osm/kentucky-latest.osm.pbf` is OpenStreetMap's extract of Kentucky from
   [Geofabrik](https://download.geofabrik.de/north-america/us/kentucky.html), about 150 MB, with its `.md5` beside it.
   To refresh it, download both again over the old ones.
2. **Build.** `.\World.ps1 build` starts [worldbuild.py](worldbuild.py) on the game servers (`-Builders`), each writing
   its own share of the regions (`extract --part K/N`) at low priority. Each machine reads the whole extract, so a build
   takes about two minutes. It writes `world/builds/<id>/`; logs go to `scratch/<machine>/`. The servers run it with
   [uv](https://docs.astral.sh/uv/), installed in the user's home if it's missing (no sudo), which fetches Python 3.12 and
   pyosmium.
3. **Publish.** When `.\World.ps1 status` lists all of the build's parts, `.\World.ps1 publish -Id <id>` checks that
   every part is there and was made from the same extract and tool, writes the build's `manifest.json`, and points
   `world/current.json` at it. The game and the servers load the build named there.

Each build is a set of region files under Kentucky's official flat grid (EPSG:3088, Kentucky Single Zone: metres east
and north, a Lambert conformal conic that keeps shapes true across the state). A region is 8,192 m square (32 × 32
cells of 256 m); `regions/<rx>_<rz>.json.gz` holds its streets, lines (rivers, streams, railways), buildings, land-use
areas, points of interest, barriers (fences, walls, hedges, ...), power lines and point features (trees, street lamps,
signs, hydrants, ...), in decimetres from its south-west corner. Streets, lines, barriers, power lines and areas are cut
at the region's edge (overlapping their neighbours by 16 m); a building belongs to the region its middle is in, a point
to the region it's in. `places.json` lists the named places (cities, towns, villages, largest first) by grid position.
Run the builder's tests with `python -m unittest discover -s Tools/World/tests` (no extract needed; run them with
`uv run --python 3.12 --with osmium python -m unittest discover -s Tools/World/tests` and they also extract and publish a
tiny made-up map).

The first build, `ky-20261019-2305`, holds 1,956 regions: about 932,000 buildings, 726,000 street pieces and 112,000
areas. The October 2026 extract built in format 2 gives 2,071 regions (127 MB): the same buildings, streets, lines and
points of interest, 115,000 areas, 18,000 barrier pieces, 8,000 power line pieces and 99,000 point features (27,000
crossings, 22,000 trees, 16,000 gates, 5,000 street lamps, 5,000 stop signs, ...). One machine builds the whole state
in about three minutes and 3 GB of memory (`extract` then `publish`, with `CLONEZ_WORLD` pointing at a store of its
own).

### What a region holds

Every list is in every region file (empty if there's nothing); coordinates (`x`, `z` and `pts`, which is
`[x0, z0, x1, z1, ...]`) are decimetres east and north of the region's south-west corner. The data is in **format 2**,
which only adds to format 1 (fields and lists marked *2* below, and the new area kinds), so readers take both; in a
format-1 file the format-2 fields are missing, which means "unknown".

| List | Each holds | |
|---|---|---|
| `streets` | `id kind name width lanes oneway bridge tunnel layer pts`, *2:* `surface sidewalk lit route` | `kind` is OSM's highway value; `name` is the street's name, or its route number if it has none |
| `lines` | `id kind name width pts`, *2:* `bridge tunnel layer` | rivers, canals, streams, ditches, drains and railways (`rail`, `abandoned`, `disused`) |
| `buildings` | `id kind name address levels height use pts`, *2:* `roof material colour roofColour roofLevels year` | `pts` is the outline |
| `areas` | `id kind name pts` | land use and cover (kinds below) |
| `pois` | `id kind name x z` | amenities and shops |
| `barriers` *2* | `id kind name width pts` | fences, walls, hedges, guard rails, retaining walls, tree rows |
| `power` *2* | `id kind name width pts` | power lines |
| `nodes` *2* | `id kind name x z dir` | point features |

* **Streets** (*2*): `surface` is `asphalt` (OSM's asphalt, paved, chipseal), `concrete`, `brick` (paving stones, setts,
  cobbles, bricks), `gravel` (unpaved, compacted, gravel, pebbles) or `dirt` (dirt, earth, ground, grass, mud, sand), or
  "" when OSM doesn't say. `sidewalk` is `both`, `left`, `right`, `no` or "" (unknown); a sidewalk mapped as a footway of
  its own (`separate`) counts as `no`, since the footway comes as a street of its own. `lit` is 1 (lit), 2 (not) or 0
  (unknown). `route` is the road's route number(s), OSM's `ref` (`US 31E;US 150`).
* **Lines** (*2*): `bridge`, `tunnel` (a culvert too) and `layer`, as for streets.
* **Buildings** (*2*): `roof` from `roof:shape` (`flat`, `gabled`, `hipped`, `pyramidal`, `skillion`, `gambrel`,
  `mansard`, `half-hipped`, `dome` or `round`; similar shapes fold in: saltbox is gabled, cone pyramidal, onion dome);
  `material` from `building:material` (`brick`, `wood`, `vinyl` (OSM's plastic), `stone`, `concrete`, `metal`, `glass` or
  `stucco` (plaster)); `colour` and `roofColour` from `building:colour` and `roof:colour` as `#rrggbb` (CSS colour names
  and `#rgb` are converted); `roofLevels` from `roof:levels`; `year` built, from `start_date` ("1880s" is 1880, "C19"
  1850). Each is "" or 0 when OSM doesn't say, which in Kentucky is nearly always (about 1,100 roof shapes and 700 dates
  in 932,000 buildings).
* **Barriers** (*2*): `fence`, `wall` (city and flood walls too), `hedge`, `guard_rail`, `retaining_wall` and `tree_row`,
  cut at the region's edge like lines. `width` is the thickness in metres when it's tagged, otherwise 0.1 for a fence,
  0.4 a wall, 1.2 a hedge, 0.3 a guard rail, 0.5 a retaining wall and 6 a tree row.
* **Power** (*2*): `line` (transmission, on towers or H-frames) and `minor_line` (distribution, on wooden poles). Every
  point of the line as mapped is a support; cutting at the region's edge adds points exactly on the cut, so put supports
  only at points strictly inside your own square. `width` is the spread of the wires: 2.4 m for a minor line, and for a
  line by its voltage, 4 m under 100 kV, 9 m under 200 kV (or untagged), 16 m under 400 kV, 24 m above.
* **Nodes** (*2*): `tree`, `street_lamp`, `traffic_signals`, `stop`, `give_way`, `crossing`, `level_crossing`,
  `bus_stop`, `hydrant`, `bench`, `waste_basket`, `post_box`, `telephone`, `vending`, `flagpole`, `water_tower`, `mast`
  (communication towers too), `memorial` (monuments too), `gate` and `bollard`. Water towers, masts, flagpoles and
  memorials mapped as an outline rather than a point (and not as a building) are a point in its middle, with the
  outline's id (the kind of id buildings and areas have). `dir` is the bearing the feature faces in degrees clockwise
  from the grid's north (the game's +z), from its `direction` tag (degrees from true north, which leans up to 2.3° off
  the grid's at Kentucky's ends, turned onto the grid; compass points like `NE`; a range faces its middle), or -1 when
  unknown. Stop and give-way signs and traffic signals tagged `forward` or `backward` along their street face the
  traffic they're for.
* **Areas**: format 1's `water`, `parking`, `cemetery`, `forest`, `farmland`, `farmyard`, `orchard`, `meadow`, `grass`,
  `scrub`, `wetland`, `rock`, `sand`, `quarry`, `residential`, `commercial`, `retail`, `industrial`, `park`, `playground`,
  `pitch` and `golf`, and (*2*) `school` (school, college and university grounds), `hospital`, `churchyard` (church
  grounds), `lot` (brownfield, greenfield, construction site), `plaza` (a pedestrian square), `fuel` (a filling
  station's forecourt), `airfield` and `military`. When an area's tags fit several kinds, format 1's win, so format-1
  areas come out as before. A kind a reader doesn't know is plain ground.
* The manifest's region entries and totals, and each part's listing, count (*2*) `barriers`, `power` and `nodes` too.

## In the game

A zone with a `RealPlace` (in [ZoneCatalog.cs](../../Assets/Scripts/Zone/ZoneCatalog.cs)) is built by
[KentuckyMap](../../Assets/Scripts/World/Map/KentuckyMap.cs) from the current build instead of the procedural town:
the 400 m square around the place's centre, with the streets and buildings where they really are, and past the edge the
town carrying on as scenery. If the store can't be read, the raid falls back to the procedural town and the log says
why.

The map is drawn by a pipeline of passes, each a class of its own in `Assets/Scripts/World/Map/` (the overview is at the
top of `KentuckyMap.cs`):

* **Ground** (`GroundPass*`): snow with drifts, land use (yards, fields, woods floor, cemeteries, ball fields,
  ploughed parking lots with stalls), frozen lakes and rivers with open water on the big ones, banks.
* **Roads** (`RoadPass*`, `MapJunctions`): the street network with real junctions, ploughed winter carriageways, kerbs,
  sidewalks and driveways, faded markings, paths, bridges and railways.
* **Buildings** (`BuildingPass*`): an archetype for every building from its kind, use, footprint and surroundings
  (OpenStreetMap rarely has storeys or roof shapes for Kentucky, so mostly heuristics): downtown brick rows with
  storefronts, frame houses with porches, trailers, churches with steeples, schools, warehouses, barns, silos, gas
  stations; real roofs (gable, hip, gambrel, flat with parapet…) with snow and chimneys.
* **Interiors** (`InteriorPass*`, `InteriorStreamer`): floor plans, furniture, working fixtures, lights and loot by
  what the building was, room by room; built at once on a fixed square, and on the statewide map lazily, only within
  about 45 m of a survivor.
* **Cutaway** (`CutawayPass`, `MapCutaway*`): roofs and upper storeys show from the isometric camera and are lifted off,
  Project Zomboid-style, around the survivor and between them and the camera.
* **Streetscape** (`StreetscapePass*`, `PowerPass`, `SignPass`, `BarrierPass`, `StreetLamp`): utility poles and wires,
  street lights (a small pool of real lights lent to the nearest lamps), span-wire signals, stop signs and street-name
  blades, route shields, hydrants, mailboxes, benches, water towers with the town's name, fences and stone walls.
* **Nature** (`VegetationPass*`, `VehiclePass*`, `SnowPass`): bare winter trees, cedars and pines; 1993 cars in
  driveways, at the kerb and in lot stalls, some lootable; plough piles and drifts.
* **Gameplay** (`MapGameplay*`): loot budgets by building and room (`Allot`, `ContainersFor`), the dead's density from
  where people lived (`DensityAt`), real landmarks, exits named for the roads that leave the map.

The game's one non-tutorial zone, **Kentucky**, is the whole state (`RealStatewide`). The raid starts in Bardstown: its
400 m square is built at once and holds the raid's exits, landing zones and the zone's landmarks, and from there
[KentuckyStreamer](../../Assets/Scripts/World/Map/KentuckyStreamer.cs) builds the rest of Kentucky around the survivor
as they walk or drive, in 128 m pieces (64 to a region), with the same passes:

* Regions are read and sorted by piece off the main thread
  ([MapRegionIndex](../../Assets/Scripts/World/Map/MapRegionIndex.cs)), a few regions ahead of the survivor. The pieces
  around a survivor are built in full detail and the ring past them in a cheaper "Lite" detail, upgraded as the survivor
  comes near; pieces are built within a per-frame time budget, nearest and ahead first, with their meshes packed off the
  main thread, and the pieces more than three away are let go of. A building belongs to the piece that holds the middle
  of its outline and is built whole there.
* The NavMesh is baked around the survivors in the background as they move; the arena square that zombie spawns, the
  A-Life and the map's clamps work in (`CloneZConfig.ArenaCenter`) follows them, and the build's regions are the edge of
  the world. The dead left far behind go quietly and come back around the survivors. In co-op, pieces are built around
  every survivor, and the NavMesh (so the dead and the Zone's people) covers them while they're within about 1.6 km of
  each other; past that it stays with the host's survivor.
* Every random choice comes from hashes of the map's own ids, so a piece comes out the same on every machine and every
  time it's built. A piece's containers roll their own loot from the raid's seed and their place (outside the Central
  Economy's caps, which are tuned for one map); a piece that's let go of and built again keeps what was taken from them
  (for the last thousand or so pieces left behind).
* World coordinates stay metres from Bardstown's centre, so camps, cars and map marks saved in the zone keep their
  places. Floats keep a few millimetres' precision out to about 100 km from Bardstown, which a raid's night of driving
  doesn't reach; the far ends of the state (Paducah, Pikeville) are 250–360 km away, where movement and rendering get
  coarse — re-centring the world as the survivor travels (with the saves converted) would be the next step.

The game reads the store at `\\192.168.1.170\CLONE-Z` on Windows and `/mnt/clonez` elsewhere. Set the `CLONEZ_WORLD`
environment variable to use a copy somewhere else (for a build to share outside the LAN, copy `world/current.json`,
the current build's `manifest.json` and `places.json`, and the regions it needs — for the whole state, all of them).
The map needs a format-2 build for its fences, power lines, trees and signs (a format-1 build still loads, without
them).

To see the map without playing, the editor's batch harness renders it and reports its cost
([MapShots](../../Assets/Scripts/Dev/MapShots.cs)):
`Unity.exe -batchmode -projectPath <project> -executeMethod CloneZ.EditorTools.CloneZBatch.MapShots -clonez-mapshots <outDir> [-clonez-mapshots-place <town>] [-clonez-mapshots-quick] [-clonez-mapshots-lite] -logFile <log>`
with `CLONEZ_WORLD` set. It writes top-down and isometric pictures by day and night, `report.md` and `report.json`.

Places are only as complete as OpenStreetMap: a town where few buildings are mapped looks sparse. The night shift's
`map` focus works on the builder's code (`Assets/Scripts/World/Map/`), not on the data.

## Credit

The map data is © OpenStreetMap contributors and available under the
[Open Database License](https://opendatacommons.org/licenses/odbl/). The game credits it in the zone's description;
keep that credit wherever the map is shown.
