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

`.\World.ps1 status` shows which machines have the store mounted and what it holds.
