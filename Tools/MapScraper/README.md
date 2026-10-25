# WithTheDead map scraper (zomboidmap.com)

Mirrors the Project Zomboid **Build 42** map data served by [zomboidmap.com](https://zomboidmap.com)
for a region (Louisville by default). It then builds exports from that data that the site
only computes in the browser. The result is an offline copy of the map for reference while
building WithTheDead.

```
tiles.zomboidmap.com ──► out/raw/            exact mirror: data files, room interiors, every floor's tiles
                              │
                              ├─► out/regions/<region>/   streets, buildings, rooms, zones, fixtures, POIs,
                              │                           loot, labels, heatmaps (GeoJSON / JSON / CSV / PNG)
                              ├─► .../mosaic/             stitched isometric images per floor + all floors stacked
                              └─► .../topdown/            north-up top-down maps, one PNG per 256x256 cell
```

## One-click app

Double-click **`MapScraper.bat`** for a menu, or run `.\MapScraper.ps1 <action>`:

| Action    | What it does |
|-----------|--------------|
| `plan`    | Shows what a rip downloads for the region: file counts per zoom level, what you already have, the estimated size and the time at the current rate. |
| `start`   | Starts or resumes the rip as a hidden background process. Order: data files, room interiors, region exports, tiles (coarsest level first), then mosaics and top-down maps. Add `-DataOnly` for just the data and exports (a few minutes). |
| `status`  | Shows whether it is running, progress per phase, speed, ETA and the latest log lines. |
| `watch`   | Shows the live log. Press Q or Esc to stop watching; the rip keeps going. |
| `stop`    | Stops gracefully: requests already in flight finish, then it exits. `start` resumes. `stop -Force` kills it. |
| `export`  | Rebuilds the region exports from the downloaded data (offline). |
| `stitch`  | Rebuilds the stitched mosaics (`-Level 5`, `-Floors 0-3`, ...). |
| `topdown` | Rebuilds the top-down maps (`-PxPerSquare 8` uses L3 tiles, `-Floors 0,1`, ...). |
| `open`    | Opens the output folder. |

The rip runs on this PC, detached from the window that started it, so you can close the menu or
VS Code. While it runs it keeps Windows from going to sleep (the display can still switch off).
Everything already on disk is skipped, so `start` always resumes. That includes resuming after a
reboot, a crash or `stop -Force`.

### Overnight run

```
.\MapScraper.ps1 plan       # check the numbers first
.\MapScraper.ps1 start      # Louisville, every floor, every zoom level, then mosaics and top-down maps
.\MapScraper.ps1 status     # next morning
```

Measured on October 17, 2026 (Build 42.20 data). Rates are at the default 8 requests/s:

| Region | Tiles | Download | Time |
|--------|-------|----------|------|
| `louisville` (default) | ~130,000 | ~35 GB | ~4.5 h |
| `all` (whole map) | ~1,500,000 | ~575 GB | ~52 h (spread it over several nights; it resumes) |

Most of the size is L0, the native detail level. `-Levels 1-11` cuts Louisville to about 6 GB, and
`-Levels 2-11` to about 2 GB.

### Options

| Option | Default | Meaning |
|--------|---------|---------|
| `-Region` | `louisville` | `louisville` or `all`. For a custom area, use `-Extra '--bbox','X0','Y0','X1','Y1'` (squares) or `-Extra '--cells','CX0','CY0','CX1','CY1'` (cells), plus `'--name','myarea'`. |
| `-Levels` | `all` | Zoom levels to download: `all`, `3-11`, `0,1`... L0 is native detail; each level up halves the resolution. |
| `-Floors` | `all` | `all`, `0`, `0-5`, `ground`, `above`, `basements`, or `-17--1`. Floors run from -17 to 29. |
| `-Workers` | `6` | Parallel downloads. |
| `-Rate` | `8` | Maximum requests per second across all workers. Keep it modest; this is a free fan site. |
| `-DataOnly` | off | Data files, rooms and exports only; no tiles. |
| `-Out` | `out\` | Output folder. You can also set it with `$env:CLONEZ_MAP_OUT`. A big rip can go to another drive, e.g. `-Out S:\zomboidmap`. |
| `-Extra` | | Extra `mapscraper.py` flags, e.g. `'--retry-missing'`, `'--no-build'`, `'--refresh'`, `'--min-free-gb','50'`. |

## Output

```
out/
  raw/                              1:1 mirror of tiles.zomboidmap.com
    map_info.json, manifest.json    tile pyramid description, and which tiles exist
    overlays/*.json|geojson|png     whole-map data (see below), plus i18n/ and loot/names.<lang>.json translations
    overlays/rooms/<cx>_<cy>.json   room interiors per cell
    layer<z>/L<level>/<x>_<y>.webp  per-floor isometric tiles (1024 px, transparent background)
    composite/L<level>/<x>_<y>.webp all floors flattened (L3 and coarser)
  global/loot/                      decoded loot tables: rooms.json, items.json, containers.json, room_items.csv
  regions/<region>/
    summary.json                    counts at a glance
    buildings.geojson|csv           footprint, class, floors, room types with positions, zombie/danger values
    rooms.json|csv                  every room: type, floor, rectangles, area, owning building id
    streets.geojson|csv, street_names.json
    zones.geojson|csv, zones/<Type>.geojson   town, forest, farm, parking stalls (vehicle spawns), zombie types...
    fixtures.json|csv               wells, gas pumps, freezers, ovens, workbenches... (38 types)
    pois.json|csv                   gas, police, guns, food, medical... derived from room types
    labels.json|csv, spawnpoints.json
    loot_here.json|csv              loot table of every room type found in the region
    heatmaps/                       zpop, danger and water crops, forage_zones.png, heatmaps.json (scales/origins)
    site_format/                    the site's search.json and building_classes.json cut to the region
    mosaic/L<level>/                floor_<z>.png, all_floors.png, composite.png, mosaic.json
    topdown/floor_0/                cells/<cx>_<cy>.png, overview.png; topdown.json
  state/                            progress.json, plan.json, missing.txt (404s), failed.txt, run.lock
  run.log
```

### Coordinates

Everything uses Project Zomboid world squares: x grows east, y grows south, and a cell is
256x256 squares. Louisville is cells 45-61 x 3-17 (squares x 11520-15872, y 768-4608).

- **Tiles:** a world position (x, y) on floor z is at pixel
  `px = (x - y - gx_origin) * grid_w / 2^L`, `py = ((x + y - gy_origin) * grid_h - z * layer_px) / 2^L`
  of level L. The tile is `(px // 1024, py // 1024)`. The constants are in `raw/map_info.json`, and
  each `mosaic.json` repeats them with its pixel origin.
- **Top-down cells:** pixel (i, j) of `cells/<cx>_<cy>.png` is square
  `(cx*256 + i/ppsq, cy*256 + j/ppsq)`. Floors and roads are exact. Walls and trees lean towards
  the top-left, because the isometric render draws them above their square.
- **Heatmaps:** `zpop.png` (values 0-10) and `danger.png` (0-255) use 8 squares per pixel; `water.png` uses 2.
- **Loot `score`:** the site's relative odds, 0-100. `rooms_on_map` is how many rooms of that type exist.

## Running the CLI directly

```
python mapscraper.py plan [--region all] [--levels 1-11]
python mapscraper.py run --levels 2-11 --floors=0-5 --workers 6 --rate 8
python mapscraper.py run --no-tiles                    # data + exports only
python mapscraper.py run --cells 50 7 52 9 --name downtown
python mapscraper.py status
python mapscraper.py export | stitch --level 5 | topdown --ppsq 8 --floors 0,1
```

Python 3.9+ with only the standard library. [Pillow](https://pypi.org/project/pillow/) is needed for
heatmap crops, mosaics and top-down maps (`python -m pip install pillow`). Values that start with
`-` need the `=` form, e.g. `--floors=-17--1`.

## Being a good guest

zomboidmap.com is a free fan project by Hunter Pollock. Its robots.txt allows crawling. This tool
identifies itself (`CLONE-Z-MapScraper/1.0`), rate-limits every request, reuses connections, and
backs off (up to a 15-minute cool-down) when requests fail. Please keep the default rate, and keep
the output for personal reference. The map art and data come from Project Zomboid by The Indie Stone.

The game's own map files live in `ref/media/maps/` (Louisville is part of `Muldraugh, KY`). They
are the authoritative source if WithTheDead ever needs every sprite on every square.
