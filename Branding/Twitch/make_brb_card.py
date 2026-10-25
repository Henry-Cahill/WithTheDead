#!/usr/bin/env python3
"""Renders the WithTheDead "Be right back" card (1920x1080), shown on stream while the Windows screen is hidden.

    python make_brb_card.py [--out FILE]

The profile picture's scene (make_profile_pic.render) sits in a round emblem beside the message; the lettering
gets the same red paint, outline and snow as the WTD on the emblem.
"""
from __future__ import annotations

import argparse
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import make_profile_pic as art

W, H = 1920, 1080
K = 2                      # background and lettering are drawn at 2x
EMBLEM_C, EMBLEM_D = (600, 540), 700
TEXT_X = 1030


def background() -> np.ndarray:
    h, w = H * K, W * K
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    t = (yy / h)[..., None]
    C = art.col("#03050b") * (1 - t) + art.col("#16233b") * t
    d = np.hypot(xx - EMBLEM_C[0] * K, yy - EMBLEM_C[1] * K)
    art.screen(C, art.col("#6f8fbf"), 0.42 * np.exp(-(d / (520 * K)) ** 2))
    img = Image.new("L", (w, h), 0)
    dr = ImageDraw.Draw(img)
    rnd = random.Random(1993)
    for _ in range(900):
        x, y, r = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(0.6, 2.2) * K
        dr.ellipse([x - r, y - r, x + r, y + r], fill=int(255 * rnd.uniform(0.2, 0.7)))
    art.over(C, art.col("#e8f0fa"), np.asarray(img, np.float32) / 255.0)
    return C


def lettering(C: np.ndarray) -> None:
    h, w = C.shape[:2]
    stencil = art.font("STENCIL.TTF", "impact.ttf")
    probe = ImageFont.truetype(str(stencil), 200)
    size = int(200 * 760 * K / probe.getlength("BE RIGHT"))
    f = ImageFont.truetype(str(stencil), size)
    top = 340 * K
    lines = []
    for i, word in enumerate(("BE RIGHT", "BACK")):
        img = Image.new("L", (w, h), 0)
        ImageDraw.Draw(img).text((TEXT_X * K, top + int(size * 0.95) * i), word, font=f, fill=255, anchor="la")
        lines.append(np.asarray(img, np.float32) / 255.0)
    L = np.maximum(*lines)
    rows = np.nonzero(L.max(axis=1) > 0.5)[0]
    y0, y1 = rows[0], rows[-1]

    art.over(C, art.col("#010204"), 0.8 * art.blur(art.shift(L, 4 * K, 7 * K), 5 * K))
    art.over(C, art.col("#16060a"), np.clip((art.blur(L, 2.6 * K) - 0.04) * 5.0, 0, 1))
    yy = np.arange(h, dtype=np.float32)[:, None, None]
    t = np.clip((yy - y0) / (y1 - y0), 0, 1)
    red = art.col("#e6443a") * (1 - t) + art.col("#931b14") * t
    edge = np.clip((1.0 - art.blur(L, 3.5 * K)) * 1.6, 0, 1)
    wear = art.smoothstep(0.64, 0.8, art.noise((h, w), 6 * K, 41, octaves=3))
    over_red = red * (1.0 - 0.38 * edge - 0.2 * wear)[..., None]
    art.over(C, over_red, L)
    snow = np.maximum(*(art.snow_caps(line, 5.0, 43 + i, outer_only=True) for i, line in enumerate(lines)))
    art.paint_snow(C, snow, "#d5e0ec", "#fbfdff", "#8ea2bd")

    sub = ImageFont.truetype(str(art.font("bahnschrift.ttf", "arialbd.ttf")), 34 * K)
    sub.set_variation_by_name(b"SemiBold Condensed")
    simg = Image.new("L", (w, h), 0)
    ImageDraw.Draw(simg).text((TEXT_X * K + 4 * K, y1 + 70 * K), "H O L D   T H E   L I N E     ·     B A C K   B E F O R E   D A W N",
                              font=sub, fill=255, anchor="la")
    art.over(C, art.col("#c9d6e6"), np.asarray(simg, np.float32) / 255.0 * 0.9)


def emblem(card: Image.Image) -> None:
    D, (cx, cy) = EMBLEM_D, EMBLEM_C
    pic = art.render().resize((D, D), Image.LANCZOS)
    big = Image.new("L", (D * 4, D * 4), 0)
    ImageDraw.Draw(big).ellipse([0, 0, D * 4 - 1, D * 4 - 1], fill=255)
    mask = big.resize((D, D), Image.LANCZOS)
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).ellipse([cx - D // 2 + 10, cy - D // 2 + 18, cx + D // 2 + 10, cy + D // 2 + 18], fill=200)
    sh = art.blur(np.asarray(shadow, np.float32) / 255.0, 14)
    f = np.asarray(card, np.float32) / 255.0
    art.over(f, art.col("#010204"), sh * 0.85)
    ring = Image.new("L", (W, H), 0)
    ImageDraw.Draw(ring).ellipse([cx - D // 2 - 7, cy - D // 2 - 7, cx + D // 2 + 7, cy + D // 2 + 7], fill=255)
    art.over(f, art.col("#22324f"), np.asarray(ring, np.float32) / 255.0)
    card.paste(Image.fromarray((np.clip(f, 0, 1) * 255 + 0.5).astype(np.uint8)))
    card.paste(pic, (cx - D // 2, cy - D // 2), mask)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[1])
    ap.add_argument("--out", type=Path, default=art.HERE / "wtd-brb-1920x1080.png")
    args = ap.parse_args()
    C = background()
    lettering(C)
    card = Image.fromarray((np.clip(C, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((W, H), Image.LANCZOS)
    emblem(card)
    f = np.asarray(card, np.float32) / 255.0
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    dc = np.hypot((xx - W / 2) / (W / 2), (yy - H / 2) / (H / 2))
    vignette = (np.clip((dc - 0.75) / 0.6, 0, 1) ** 1.5 * 0.5)[..., None]
    f = f * (1 - vignette) + art.col("#02030a") * vignette
    f += np.random.default_rng(7).normal(0.0, 0.01, (H, W, 1)).astype(np.float32)
    Image.fromarray((np.clip(f, 0, 1) * 255 + 0.5).astype(np.uint8)).save(args.out, optimize=True)
    print("wrote", args.out)


if __name__ == "__main__":
    main()
