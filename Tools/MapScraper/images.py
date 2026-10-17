"""Images built from downloaded tiles (needs Pillow).

stitch:  one isometric PNG per floor for a region at one zoom level, all above-ground floors
         stacked into one picture, and the site's flattened "composite" view, plus mosaic.json
         describing how world squares map to pixels.
topdown: the isometric tiles un-skewed into north-up, top-down maps: one PNG per 256x256 cell and
         an overview. Ground, floors and roads land exactly on their squares; tall things (walls,
         trees) lean towards the top-left because they are drawn above their square in the render.
"""
from __future__ import annotations

import json
import logging
import math
import os

from zmap import region_tiles, tile_path

log = logging.getLogger("mapscraper")


def _pillow():
    try:
        from PIL import Image
    except ImportError:
        raise SystemExit("Pillow is needed for images: python -m pip install pillow")
    Image.MAX_IMAGE_PIXELS = None
    return Image


def _tile_file(raw_dir, layer, level, tx, ty, fmt):
    return os.path.join(raw_dir, *tile_path(layer, level, tx, ty, fmt).split("/"))


def _write_json(path, data):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)


def mosaic_frame(info, region, level, floors):
    """Pixel frame (x, y, width, height) at `level` that holds the region on every given floor,
    so the floor images line up when layered."""
    s = 1 << level
    top = max([z for z in floors if z >= 0], default=0)
    bottom = min(min(floors, default=0), 0)
    px0 = (region.x0 - region.y1 - info.gx_origin) * info.grid_w / s
    px1 = (region.x1 - region.y0 - info.gx_origin) * info.grid_w / s
    py0 = ((region.x0 + region.y0 - info.gy_origin) * info.grid_h - top * info.layer_px) / s
    py1 = ((region.x1 + region.y1 - info.gy_origin) * info.grid_h - bottom * info.layer_px) / s
    fx, fy = math.floor(px0), math.floor(py0)
    return fx, fy, math.ceil(px1) - fx, math.ceil(py1) - fy


def auto_mosaic_level(info, region, floors, levels, max_mpx=48):
    """Most detailed level in `levels` whose mosaic stays under max_mpx megapixels."""
    for level in sorted(levels):
        _, _, width, height = mosaic_frame(info, region, level, floors)
        if width * height <= max_mpx * 1e6:
            return level
    return max(levels, default=info.levels - 1)


def stitch(raw_dir, info, manifest, region, level, floors, out_dir, pad=8, max_mpx=300,
           composite=True, should_stop=lambda: False):
    Image = _pillow()
    s, size = 1 << level, info.tile_size
    fx, fy, width, height = mosaic_frame(info, region, level, floors)
    if width * height > max_mpx * 1e6:
        raise SystemExit(f"A level-{level} mosaic of '{region.name}' would be {width}x{height} px "
                         f"({width * height / 1e6:.0f} Mpx). Use a coarser level (a higher number) or raise --max-mpx.")
    folder = os.path.join(out_dir, f"L{level}")
    os.makedirs(folder, exist_ok=True)
    tile_region = region.padded(pad)
    missing = 0

    def paint(layer):
        nonlocal missing
        canvas = None
        for tx, ty in region_tiles(info, manifest, tile_region, layer, level):
            ox, oy = tx * size - fx, ty * size - fy
            if ox >= width or oy >= height or ox + size <= 0 or oy + size <= 0:
                continue
            path = _tile_file(raw_dir, layer, level, tx, ty, info.format)
            if not os.path.exists(path):
                missing += 1
                continue
            if canvas is None:
                canvas = Image.new("RGBA", (width, height))
            with Image.open(path) as im:
                canvas.paste(im.convert("RGBA"), (ox, oy))
        return canvas

    log.info("Stitching '%s' at L%d: %dx%d px per image", region.name, level, width, height)
    made, stacked = [], None
    for layer in sorted(floors):  # basements first, then the ground floor upwards (stacking order)
        if should_stop():
            break
        canvas = paint(layer)
        if canvas is None:
            continue
        name = f"floor_{layer}.png"
        canvas.save(os.path.join(folder, name))
        made.append({"floor": layer, "file": name})
        log.info("  %s", name)
        if layer >= 0:
            if stacked is None:
                stacked = canvas
            else:
                stacked.alpha_composite(canvas)
    if stacked is not None:
        stacked.save(os.path.join(folder, "all_floors.png"))
        log.info("  all_floors.png")
    composite_file = None
    if composite and manifest.has_composite(level) and not should_stop():
        canvas = paint("composite")
        if canvas is not None:
            composite_file = "composite.png"
            canvas.save(os.path.join(folder, composite_file))
            log.info("  composite.png")
    meta = {
        "region": region.to_json(info.cell_size), "level": level, "scale": 1 / s,
        "size": [width, height], "origin_px": [fx, fy],
        "projection": {"grid_w": info.grid_w, "grid_h": info.grid_h, "layer_px": info.layer_px,
                       "gx_origin": info.gx_origin, "gy_origin": info.gy_origin},
        "world_to_image": ["px = (x - y - gx_origin) * grid_w / 2^level - origin_px[0]",
                           "py = ((x + y - gy_origin) * grid_h - floor * layer_px) / 2^level - origin_px[1]"],
        "floors": made, "all_floors": "all_floors.png" if stacked is not None else None,
        "composite": composite_file, "tiles_not_downloaded": missing,
    }
    _write_json(os.path.join(folder, "mosaic.json"), meta)
    if missing:
        log.info("  %d tiles at L%d are not downloaded yet; those areas are transparent.", missing, level)
    return meta


def topdown_level(info, ppsq):
    """Tile level whose detail matches `ppsq` output pixels per square."""
    return max(0, min(info.levels - 1, round(math.log2(64 / ppsq))))


def _unskew(Image, raw_dir, info, level, z, bx, by, block, ppsq):
    """Top-down image of squares [bx, bx+block) x [by, by+block) on floor z, from `level` tiles."""
    s, size = 1 << level, info.tile_size
    px0 = (bx - by - block - info.gx_origin) * info.grid_w / s - 1
    px1 = (bx + block - by - info.gx_origin) * info.grid_w / s + 1
    py0 = ((bx + by - info.gy_origin) * info.grid_h - z * info.layer_px) / s - 1
    py1 = ((bx + by + 2 * block - info.gy_origin) * info.grid_h - z * info.layer_px) / s + 1
    tx0, tx1 = math.floor(px0 / size), math.floor(px1 / size)
    ty0, ty1 = math.floor(py0 / size), math.floor(py1 / size)
    src = None
    for ty in range(ty0, ty1 + 1):
        for tx in range(tx0, tx1 + 1):
            path = _tile_file(raw_dir, z, level, tx, ty, info.format)
            if not os.path.exists(path):
                continue
            if src is None:
                src = Image.new("RGBA", ((tx1 - tx0 + 1) * size, (ty1 - ty0 + 1) * size))
            with Image.open(path) as im:
                src.paste(im.convert("RGBA"), ((tx - tx0) * size, (ty - ty0) * size))
    if src is None:
        return None
    # Output pixel (i, j) is world (bx + i/ppsq, by + j/ppsq); map it into the source canvas.
    k = 1.0 / (ppsq * s)
    coeffs = (info.grid_w * k, -info.grid_w * k, (bx - by - info.gx_origin) * info.grid_w / s - tx0 * size,
              info.grid_h * k, info.grid_h * k,
              ((bx + by - info.gy_origin) * info.grid_h - z * info.layer_px) / s - ty0 * size)
    affine = getattr(Image, "Transform", Image).AFFINE
    bilinear = getattr(Image, "Resampling", Image).BILINEAR
    return src.transform((block * ppsq, block * ppsq), affine, coeffs, resample=bilinear)


def topdown(raw_dir, info, region, ppsq, floors, out_dir, overview_ppsq=1, should_stop=lambda: False):
    Image = _pillow()
    ppsq = max(1, min(32, int(ppsq)))
    level = topdown_level(info, ppsq)
    cs = info.cell_size
    block = min(cs, max(1, 1024 // ppsq))  # squares per transform; keeps each source canvas small
    cx0, cy0, cx1, cy1 = region.cell_range(cs)
    box = getattr(Image, "Resampling", Image).BOX
    # Keep overview.png under ~100 megapixels (the whole map at 1 px per square would be 322).
    area = (region.x1 - region.x0) * (region.y1 - region.y0)
    while overview_ppsq > 1 / 64 and area * overview_ppsq ** 2 > 100e6:
        overview_ppsq /= 2
    report = {"region": region.to_json(cs), "px_per_square": ppsq, "source_level": level,
              "cell_image": "floor_<z>/cells/<cx>_<cy>.png covers squares [cx*256, cx*256+256) x [cy*256, cy*256+256)",
              "pixel_to_world": "x = cx*256 + px / px_per_square, y = cy*256 + py / px_per_square",
              "overview_px_per_square": overview_ppsq, "floors": {}}
    log.info("Top-down maps for '%s': %d px per square from L%d tiles", region.name, ppsq, level)
    for z in floors:
        folder = os.path.join(out_dir, f"floor_{z}")
        cells_dir = os.path.join(folder, "cells")
        os.makedirs(cells_dir, exist_ok=True)
        overview = Image.new("RGBA", (math.ceil((region.x1 - region.x0) * overview_ppsq),
                                      math.ceil((region.y1 - region.y0) * overview_ppsq)))
        written = 0
        for cy in range(cy0, cy1 + 1):
            for cx in range(cx0, cx1 + 1):
                if should_stop():
                    break
                cell_img = None
                for by in range(cy * cs, (cy + 1) * cs, block):
                    for bx in range(cx * cs, (cx + 1) * cs, block):
                        part = _unskew(Image, raw_dir, info, level, z, bx, by, block, ppsq)
                        if part is None:
                            continue
                        if cell_img is None:
                            cell_img = Image.new("RGBA", (cs * ppsq, cs * ppsq))
                        cell_img.paste(part, ((bx - cx * cs) * ppsq, (by - cy * cs) * ppsq))
                if cell_img is None or cell_img.getbbox() is None:
                    continue
                cell_img.save(os.path.join(cells_dir, f"{cx}_{cy}.png"))
                written += 1
                side = max(1, round(cs * overview_ppsq))
                overview.paste(cell_img.resize((side, side), box),
                               (round((cx * cs - region.x0) * overview_ppsq), round((cy * cs - region.y0) * overview_ppsq)))
        if written:
            overview.save(os.path.join(folder, "overview.png"))
        report["floors"][str(z)] = {"cells": written}
        log.info("  floor %d: %d cell images%s", z, written, " + overview.png" if written else
                 f" (no L{level} tiles downloaded for this floor yet)")
        if should_stop():
            break
    _write_json(os.path.join(out_dir, "topdown.json"), report)
    return report
