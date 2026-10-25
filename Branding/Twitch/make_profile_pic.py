#!/usr/bin/env python3
"""Renders the WithTheDead (WTD) Twitch profile picture.

    python make_profile_pic.py [--out FILE] [--preview DIR]

The scene is drawn at 3x and downsampled to 800x800, Twitch's recommended upload size (256x256 is the minimum and
10 MB the limit). Twitch crops profile pictures to a circle and shows them as small as ~28 px, so everything that
matters sits well inside the circle and reads as three shapes at a glance: the pale moon, the dead hand, the red WTD.
--preview also writes circle-cropped previews at the sizes Twitch uses, on its dark and light themes.

Needs Pillow and numpy, plus the Stencil and Bahnschrift fonts (Windows/Office).
"""
from __future__ import annotations

import argparse
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FONT_DIR = Path("C:/Windows/Fonts")
OUT = 800
SS = 3
S = OUT * SS

YY, XX = np.mgrid[0:S, 0:S].astype(np.float32)

# Layout, in 800-px space.
MOON_C = (400.0, 255.0)
MOON_R = 160.0
GROUND_Y = 600.0      # the horizon (behind the letters)
TREE_Y = 530.0        # where the trees and poles read as standing (their feet are hidden)
LETTERS_TOP = 468.0
LETTERS_W = 560.0
DRIFT_Y = 634.0


def p(v: float) -> float:
    return v * SS


def col(h: str) -> np.ndarray:
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def box_blur(a: np.ndarray, r: int) -> np.ndarray:
    if r < 1:
        return a
    k = 2 * r + 1
    a = np.pad(a, ((0, 0), (r + 1, r)), mode="edge")
    c = np.cumsum(a, axis=1, dtype=np.float64)
    a = (c[:, k:] - c[:, :-k]) / k
    a = np.pad(a, ((r + 1, r), (0, 0)), mode="edge")
    c = np.cumsum(a, axis=0, dtype=np.float64)
    return ((c[k:, :] - c[:-k, :]) / k).astype(np.float32)


def blur(a: np.ndarray, sigma: float) -> np.ndarray:
    """Near-Gaussian blur (three box passes); sigma in working pixels."""
    r = max(1, int(round(sigma)))
    for _ in range(3):
        a = box_blur(a, r)
    return a


def smoothstep(e0: float, e1: float, x: np.ndarray) -> np.ndarray:
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def noise(shape: tuple[int, int], cell: float, seed: int, octaves: int = 4, persistence: float = 0.5) -> np.ndarray:
    """Fractal value noise, stretched to 0..1."""
    h, w = shape
    rng = np.random.default_rng(seed)
    total = np.zeros(shape, np.float32)
    amp = 1.0
    for _ in range(octaves):
        gh, gw = max(2, int(h / cell) + 2), max(2, int(w / cell) + 2)
        grid = rng.random((gh, gw), dtype=np.float32)
        total += np.asarray(Image.fromarray(grid).resize((w, h), Image.BICUBIC), np.float32) * amp
        amp *= persistence
        cell = max(2.0, cell / 2)
    return (total - total.min()) / (total.max() - total.min() + 1e-6)


def over(C: np.ndarray, color: np.ndarray, alpha: np.ndarray) -> None:
    a = np.clip(alpha, 0.0, 1.0)[..., None]
    C *= 1.0 - a
    C += a * color


def screen(C: np.ndarray, color: np.ndarray, alpha: np.ndarray) -> None:
    a = np.clip(alpha, 0.0, 1.0)[..., None] * color
    C[...] = 1.0 - (1.0 - C) * (1.0 - a)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    img = Image.new("L", (S, S), 0)
    return img, ImageDraw.Draw(img)


def arr(img: Image.Image) -> np.ndarray:
    return np.asarray(img, np.float32) / 255.0


def shift(a: np.ndarray, dx: int = 0, dy: int = 0) -> np.ndarray:
    """Moves an array by whole pixels (positive = right / down), filling with zeros."""
    out = np.zeros_like(a)
    h, w = a.shape[:2]
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = a[ys, xs]
    return out


def font(name: str, fallback: str) -> Path:
    f = FONT_DIR / name
    return f if f.exists() else FONT_DIR / fallback


def text_mask(text: str, font_file: Path, width: float, top: float, cx: float, tracking: float = 0.0,
              variation: bytes | None = None, size: int = 1000) -> tuple[np.ndarray, int]:
    """Text as a 0..1 mask scaled to `width` working px, its top at `top`, centred on `cx`; returns it and its height."""
    f = ImageFont.truetype(str(font_file), size)
    if variation:
        f.set_variation_by_name(variation)
    advances = [f.getlength(ch) for ch in text]
    img = Image.new("L", (int(sum(advances) + tracking * size * len(text) + size), size * 2), 0)
    d = ImageDraw.Draw(img)
    x = size / 2
    for ch, adv in zip(text, advances):
        d.text((x, size / 2), ch, font=f, fill=255)
        x += adv + tracking * size
    img = img.crop(img.getbbox())
    scale = width / img.width
    img = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    out = Image.new("L", (S, S), 0)
    out.paste(img, (int(round(cx - img.width / 2)), int(round(top))))
    return arr(out), img.height


def snow_caps(mask: np.ndarray, thick: float, seed: int, outer_only: bool = False) -> np.ndarray:
    """Snow lying on the upward-facing surfaces of a 0..1 mask, as an alpha; `thick` in 800-px space. With
    `outer_only`, only on surfaces open to the sky (nothing of the mask above them), not on ledges inside letters."""
    m = mask > 0.5
    above = np.zeros_like(m)
    above[1:] = m[:-1]
    f = blur(mask, p(2.0))
    gy = np.zeros_like(f)
    gy[1:-1] = f[2:] - f[:-2]
    gx = np.zeros_like(f)
    gx[:, 1:-1] = f[:, 2:] - f[:, :-2]
    up = gy / (np.hypot(gx, gy) + 1e-6)
    top = m & ~above & (up > 0.5)
    if outer_only:
        covered = np.zeros_like(m)
        covered[1:] = np.maximum.accumulate(m, axis=0)[:-1]
        top &= ~covered
    lump = noise((1, mask.shape[1]), p(24), seed, octaves=3)[0]
    depth = (p(thick) * (0.45 + 1.1 * lump)).astype(np.int32)
    snow = top.copy()
    cur = top.copy()
    for k in range(1, int(depth.max()) + 1):
        cur = np.roll(cur, -1, axis=0)
        cur[-1] = False
        snow |= cur & (depth >= k)[None, :]
    cur = top.copy()
    for _ in range(int(p(1.5))):
        cur = np.roll(cur, 1, axis=0)
        cur[0] = False
        snow |= cur
    return smoothstep(0.3, 0.6, blur(snow.astype(np.float32), p(1.6)))


def paint_snow(C: np.ndarray, a: np.ndarray, body: str, lit: str, shade: str, strength: float = 1.0) -> None:
    """Snow with a bright upper rim and a cold shadow along its underside."""
    d = max(1, int(p(2.5)))
    over(C, col(body), a * strength)
    over(C, col(lit), np.clip(a - shift(a, 0, d), 0, 1) * 0.85 * strength)
    over(C, col(shade), np.clip(a - shift(a, 0, -d), 0, 1) * 0.55 * strength)


def sky() -> tuple[np.ndarray, np.ndarray]:
    stops = [(0.0, "#03050b"), (0.22, "#070d1a"), (0.45, "#0f1a2f"), (0.60, "#1a2944"), (0.70, "#22344f"), (1.0, "#22344f")]
    y = np.linspace(0.0, 1.0, S)
    cs = np.array([col(c) for _, c in stops])
    g = np.stack([np.interp(y, [s for s, _ in stops], cs[:, i]) for i in range(3)], axis=1).astype(np.float32)
    C = np.repeat(g[:, None, :], S, axis=1)
    dmoon = np.hypot(XX - p(MOON_C[0]), YY - p(MOON_C[1]))
    screen(C, col("#6f8fbf"), 0.55 * np.exp(-(dmoon / p(235)) ** 2) + 0.22 * np.exp(-(dmoon / p(430)) ** 2))
    return C, dmoon


def moon(C: np.ndarray, dmoon: np.ndarray) -> None:
    R = p(MOON_R)
    disc = np.clip((R - dmoon) / 1.5 + 0.5, 0.0, 1.0)
    r = np.clip(dmoon / R, 0.0, 1.0)[..., None]
    maria = smoothstep(0.5, 0.75, noise((S, S), p(70), 11))[..., None]
    craters = smoothstep(0.62, 0.8, noise((S, S), p(16), 12, octaves=2))[..., None]
    surface = col("#eef2f8") * (1.0 - 0.16 * r ** 3) * (1.0 - 0.17 * maria - 0.05 * craters)
    surface = surface * (1.0 - 0.12 * r ** 4) + col("#9fb4d4") * 0.12 * r ** 4
    over(C, surface, disc)
    screen(C, col("#d6e4f7"), 0.42 * blur(disc, p(14)))
    screen(C, col("#9db6dc"), 0.22 * blur(disc, p(45)))


def wisps(C: np.ndarray) -> None:
    n = noise((S, S), p(120), 21, octaves=5)
    warp = (noise((S, S), p(200), 22, octaves=2) - 0.5) * p(46)
    a = np.zeros((S, S), np.float32)
    for cy, h, k in ((190, 13, 0.75), (322, 19, 0.6), (120, 9, 0.45)):
        a += k * np.exp(-(((YY + warp) - p(cy)) / p(h)) ** 2)
    over(C, col("#121c30"), np.clip(a * smoothstep(0.35, 0.75, n), 0, 0.62))


def flakes(C: np.ndarray, count: int, r0: float, r1: float, a0: float, a1: float, seed: int, soft: float = 0.0) -> None:
    img, d = canvas()
    rnd = random.Random(seed)
    for _ in range(count):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), p(rnd.uniform(r0, r1))
        d.ellipse([x - r, y - r, x + r, y + r], fill=int(255 * rnd.uniform(a0, a1)))
    a = arr(img)
    if soft:
        a = blur(a, p(soft)) * 1.6
    over(C, col("#e8f0fa"), a)


def treeline(C: np.ndarray) -> None:
    n = noise((1, S), p(18), 71, octaves=4)[0]
    top = p(462) + (n - 0.5) * p(26)
    over(C, col("#0c1526"), np.clip((YY - top[None, :]) / 1.5 + 0.5, 0, 1))


def town() -> tuple[np.ndarray, np.ndarray]:
    """The town against the moon's glow: (buildings, which hold snow, and trees, poles and wires, which don't)."""
    bimg, b = canvas()
    timg, t = canvas()
    rnd = random.Random(1993)

    def poly(d, pts):
        d.polygon([(p(x), p(y)) for x, y in pts], fill=255)

    def line(d, pts, w):
        d.line([(p(x), p(y)) for x, y in pts], fill=255, width=max(1, int(p(w))), joint="curve")

    for x0, x1, eaves, ridge in ((262, 350, 470, 436), (448, 532, 468, 438), (70, 160, 478, 448), (636, 724, 480, 450)):
        poly(b, [(x0, GROUND_Y), (x0, eaves), ((x0 + x1) / 2, ridge), (x1, eaves), (x1, GROUND_Y)])
    for x, top in ((282, 438), (510, 442)):
        poly(b, [(x, 470), (x, top), (x + 9, top), (x + 9, 470)])

    # The water tower.
    cx, cy = 226.0, 372.0
    leg_top = cy + 16

    def leg_x(xt: float, xb: float, y: float) -> float:
        return cx + xt + (xb - xt) * (y - leg_top) / (GROUND_Y - leg_top)

    for xt, xb in ((-24, -42), (24, 42), (-8, -13), (8, 13)):
        line(t, [(cx + xt, leg_top), (cx + xb, GROUND_Y)], 3.4)
    for y0, y1 in ((400, 440), (440, 486)):
        line(t, [(leg_x(-24, -42, y0), y0), (leg_x(24, 42, y1), y1)], 1.5)
        line(t, [(leg_x(24, 42, y0), y0), (leg_x(-24, -42, y1), y1)], 1.5)
        line(t, [(leg_x(-24, -42, y0), y0), (leg_x(24, 42, y0), y0)], 1.5)
    b.rounded_rectangle([p(cx - 34), p(cy - 22), p(cx + 34), p(cy + 16)], radius=p(9), fill=255)
    b.ellipse([p(cx - 34), p(cy - 2), p(cx + 34), p(cy + 26)], fill=255)
    poly(b, [(cx - 38, cy - 19), (cx, cy - 46), (cx + 38, cy - 19)])
    line(t, [(cx, cy - 46), (cx, cy - 57)], 2.2)
    b.rectangle([p(cx - 40), p(cy + 1), p(cx + 40), p(cy + 3.5)], fill=255)
    for k in range(-40, 41, 8):
        line(t, [(cx + k, cy + 1), (cx + k, cy - 6)], 1.1)
    line(t, [(cx - 40, cy - 6), (cx + 40, cy - 6)], 1.1)

    # The church.
    sx = 610.0
    poly(b, [(sx - 17, GROUND_Y), (sx - 17, 414), (sx + 17, 414), (sx + 17, GROUND_Y)])
    poly(b, [(sx - 14, 416), (sx - 14, 378), (sx + 14, 378), (sx + 14, 416)])
    poly(b, [(sx - 13, 378), (sx, 312), (sx + 13, 378)])
    line(t, [(sx, 314), (sx, 292)], 2.4)
    line(t, [(sx - 6.5, 300), (sx + 6.5, 300)], 2.4)
    poly(b, [(sx + 16, GROUND_Y), (sx + 16, 452), (sx + 52, 426), (sx + 90, 452), (sx + 90, GROUND_Y)])
    b.rectangle([p(sx - 6), p(386), p(sx + 6), p(406)], fill=0)

    def tree(x: float, y: float, length: float, ang: float, w: float, depth: int) -> None:
        if depth == 0 or length < 3:
            return
        x2, y2 = x + math.cos(ang) * length, y + math.sin(ang) * length
        line(t, [(x, y), (x2, y2)], w)
        for _ in range(rnd.choice((2, 2, 3))):
            tree(x2, y2, length * rnd.uniform(0.6, 0.78), ang + rnd.uniform(-0.6, 0.6), max(0.6, w * 0.66), depth - 1)

    up = -math.pi / 2
    for x, length, lean, w, depth in ((128, 64, 0.05, 7.5, 7), (692, 58, -0.08, 7.0, 7), (556, 40, 0.1, 4.5, 6),
                                      (300, 36, -0.12, 4.0, 6)):
        line(t, [(x, GROUND_Y), (x, TREE_Y)], w)
        tree(x, TREE_Y, length, up + lean, w, depth)

    # Utility poles and their sagging wires.
    for x in (40, 770):
        line(t, [(x, GROUND_Y), (x, 396)], 3.2)
        line(t, [(x - 14, 402), (x + 14, 402)], 2.4)
    for sag, x0, x1 in ((22, 28, 758), (27, 52, 782)):
        line(t, [(x0 + (x1 - x0) * i / 60, 402 + sag * (1 - (2 * i / 60 - 1) ** 2)) for i in range(61)], 1.0)
    return arr(bimg), arr(timg)


def windows(C: np.ndarray) -> None:
    """A couple of lit windows: someone's still holding out in there."""
    img, d = canvas()
    panes = ((88, 500, 101, 515), (699, 506, 712, 521))
    for x0, y0, x1, y1 in panes:
        d.rectangle([p(x0), p(y0), p(x1), p(y1)], fill=255)
    w = arr(img)
    screen(C, col("#ff8f2e"), 0.55 * blur(w, p(9)))
    over(C, col("#ffc56e"), w)
    img, d = canvas()
    for x0, y0, x1, y1 in panes:
        d.line([(p((x0 + x1) / 2), p(y0)), (p((x0 + x1) / 2), p(y1))], fill=255, width=int(p(1.4)))
        d.line([(p(x0), p((y0 + y1) / 2)), (p(x1), p((y0 + y1) / 2))], fill=255, width=int(p(1.4)))
    over(C, col("#060a12"), arr(img))


def ground(C: np.ndarray) -> None:
    over(C, col("#4a5d7a"), 0.35 * np.exp(-((YY - p(GROUND_Y - 8)) / p(16)) ** 2))
    t = np.clip((YY - p(GROUND_Y)) / p(800 - GROUND_Y), 0, 1)[..., None]
    base = col("#5d6f8a") * (1 - t) + col("#b9c7d9") * t
    shade = (0.92 + 0.12 * noise((S, S), p(60), 31, octaves=3))[..., None]
    over(C, base * shade, np.clip((YY - p(GROUND_Y)) / 1.5 + 0.5, 0, 1))


def hand_mask() -> np.ndarray:
    """A dead hand reaching up out of a torn sleeve: gaunt, clawed, one finger broken at the last joint."""
    img, d = canvas()
    wx, wy = 400.0, 446.0
    tilt = math.radians(-5.0)
    cs, sn = math.cos(tilt), math.sin(tilt)

    def R(x: float, y: float) -> tuple[float, float]:
        dx, dy = x - wx, y - wy
        return p(wx + dx * cs - dy * sn), p(wy + dx * sn + dy * cs)

    def dot(x: float, y: float, r: float) -> None:
        cx, cy = R(x, y)
        d.ellipse([cx - p(r), cy - p(r), cx + p(r), cy + p(r)], fill=255)

    def capsule(a: tuple[float, float], b: tuple[float, float], w0: float, w1: float) -> None:
        (x0, y0), (x1, y1) = R(*a), R(*b)
        ang = math.atan2(y1 - y0, x1 - x0)
        nx, ny = -math.sin(ang), math.cos(ang)
        r0, r1 = p(w0) / 2, p(w1) / 2
        d.polygon([(x0 + nx * r0, y0 + ny * r0), (x1 + nx * r1, y1 + ny * r1),
                   (x1 - nx * r1, y1 - ny * r1), (x0 - nx * r0, y0 - ny * r0)], fill=255)
        dot(*a, w0 / 2)
        dot(*b, w1 / 2)

    d.polygon([R(370, 446), R(430, 446), R(438, 600), R(362, 600)], fill=255)
    d.polygon([R(x, y) for x, y in ((368, 446), (353, 412), (343, 374), (345, 342), (360, 334), (440, 330),
                                     (458, 336), (463, 372), (454, 410), (432, 446))], fill=255)
    rnd = random.Random(7)
    sleeve = [(356, 600), (356, 464)]
    x = 356.0
    while x < 444:
        x = min(444.0, x + rnd.uniform(5, 10))
        sleeve.append((x, rnd.uniform(450, 466)))
    sleeve += [(444, 600)]
    d.polygon([R(x, y) for x, y in sleeve], fill=255)

    tips = []
    for base, segs in (
        ((352, 346), [(-112, 40, 22, 20), (-121, 27, 19, 17), (-129, 19, 16, 14)]),   # little finger
        ((382, 336), [(-100, 52, 24, 21), (-106, 34, 21, 18), (-152, 21, 17, 14)]),   # ring, broken
        ((414, 331), [(-90, 57, 25, 22), (-93, 37, 22, 19), (-98, 27, 18, 15)]),      # middle
        ((446, 337), [(-78, 51, 24, 21), (-70, 33, 21, 18), (-62, 24, 17, 14)]),      # index
        ((444, 418), [(-38, 40, 27, 23), (-60, 32, 22, 19), (-80, 23, 18, 15)]),      # thumb
    ):
        x, y = base
        for i, (ang, length, w0, w1) in enumerate(segs):
            a = math.radians(ang)
            nx, ny = x + math.cos(a) * length, y + math.sin(a) * length
            capsule((x, y), (nx, ny), w0, w1)
            if i < len(segs) - 1:
                dot(nx, ny, w1 / 2 + 1.6)   # bony knuckles
            x, y = nx, ny
        tips.append((x, y, math.radians(segs[-1][0]), segs[-1][3]))

    m = smoothstep(0.35, 0.65, blur(arr(img), p(1.4)))
    cimg, c = canvas()
    for x, y, a, w in tips:
        nx, ny = -math.sin(a), math.cos(a)
        c.polygon([R(x + nx * w * 0.42, y + ny * w * 0.42), R(x + math.cos(a) * 11, y + math.sin(a) * 11),
                   R(x - nx * w * 0.42, y - ny * w * 0.42)], fill=255)
    return np.maximum(m, arr(cimg))


def hand(C: np.ndarray, dmoon: np.ndarray) -> None:
    m = hand_mask()
    rim = np.clip((m - blur(m, p(2.2))) * 3.2, 0, 1) * m
    backlight = np.clip(1.25 - dmoon / p(MOON_R * 1.7), 0.3, 1.0)
    over(C, col("#05070c"), m)
    screen(C, col("#b9cde8"), rim * backlight * 0.85)


def letters(C: np.ndarray) -> None:
    L, h = text_mask("WTD", font("STENCIL.TTF", "impact.ttf"), p(LETTERS_W), p(LETTERS_TOP), p(400), tracking=0.03)
    screen(C, col("#ff3324"), 0.28 * blur(L, p(28)))
    over(C, col("#010204"), 0.8 * blur(shift(L, int(p(4)), int(p(7))), p(5)))
    over(C, col("#16060a"), np.clip((blur(L, p(2.6)) - 0.04) * 5.0, 0, 1))
    t = np.clip((YY - p(LETTERS_TOP)) / h, 0, 1)[..., None]
    red = col("#e6443a") * (1 - t) + col("#931b14") * t
    edge = np.clip((1.0 - blur(L, p(3.5))) * 1.6, 0, 1)
    wear = smoothstep(0.64, 0.8, noise((S, S), p(6), 41, octaves=3))
    specks = smoothstep(0.8, 0.86, noise((S, S), p(2.2), 42, octaves=1))
    over(C, red * (1.0 - 0.38 * edge - 0.2 * wear - 0.3 * specks)[..., None], L)
    paint_snow(C, snow_caps(L, 9.0, 43), "#d5e0ec", "#fbfdff", "#8ea2bd")


def drift(C: np.ndarray) -> None:
    n1 = noise((1, S), p(140), 51, octaves=3)[0]
    n2 = noise((1, S), p(30), 52, octaves=2)[0]
    top = (p(DRIFT_Y) + (n1 - 0.5) * p(22) + (n2 - 0.5) * p(6))[None, :]
    a = np.clip((YY - top) / 1.5 + 0.5, 0, 1)
    t = np.clip((YY - top) / p(150), 0, 1)[..., None]
    ripples = np.asarray(Image.fromarray(noise((S, S // 6), p(9), 54, octaves=3)).resize((S, S), Image.BICUBIC))
    over(C, (col("#e9f0f8") * (1 - t) + col("#b4c3d6") * t) * (0.95 + 0.08 * ripples)[..., None], a)
    over(C, col("#ffffff"), np.clip(a - shift(a, 0, int(p(4))), 0, 1) * 0.6)


def subtitle(C: np.ndarray) -> None:
    T, _ = text_mask("WITH THE DEAD", font("bahnschrift.ttf", "arialbd.ttf"), p(376), p(674), p(400),
                     tracking=0.34, variation=b"Bold Condensed")
    over(C, col("#0d1626"), T * 0.92)


def render() -> Image.Image:
    C, dmoon = sky()
    moon(C, dmoon)
    wisps(C)
    flakes(C, 380, 0.5, 1.1, 0.25, 0.6, 61)
    treeline(C)
    buildings, rest = town()
    over(C, col("#060a12"), np.maximum(buildings, rest))
    paint_snow(C, snow_caps(buildings, 2.4, 72), "#7d93b1", "#a9bdd6", "#3f5068")
    windows(C)
    ground(C)
    hand(C, dmoon)
    flakes(C, 140, 1.0, 2.2, 0.35, 0.8, 62, soft=0.4)
    letters(C)
    drift(C)
    subtitle(C)
    flakes(C, 26, 3.5, 7.5, 0.12, 0.3, 63, soft=2.2)

    img = Image.fromarray((np.clip(C, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((OUT, OUT), Image.LANCZOS)
    f = np.asarray(img, np.float32) / 255.0
    yy, xx = np.mgrid[0:OUT, 0:OUT].astype(np.float32)
    dc = np.hypot(xx - OUT / 2, yy - OUT / 2) / (OUT / 2)
    vignette = (np.clip((dc - 0.62) / 0.5, 0, 1) ** 1.6 * 0.55)[..., None]
    f = f * (1 - vignette) + col("#02030a") * vignette
    f += np.random.default_rng(5).normal(0.0, 0.012, (OUT, OUT, 1)).astype(np.float32)
    return Image.fromarray((np.clip(f, 0, 1) * 255 + 0.5).astype(np.uint8))


def previews(img: Image.Image, out_dir: Path) -> None:
    """Circle crops at the sizes Twitch shows (channel page down to chat), on its dark and light themes."""
    out_dir.mkdir(parents=True, exist_ok=True)
    sizes, pad = (300, 150, 70, 50, 28), 24
    w, half = sum(sizes) + pad * (len(sizes) + 1), max(sizes) + pad * 2
    sheet = Image.new("RGB", (w, half * 2), (14, 14, 16))
    sheet.paste(Image.new("RGB", (w, half), (247, 247, 248)), (0, half))
    x = pad
    for s in sizes:
        im = img.resize((s, s), Image.LANCZOS)
        mask = Image.new("L", (s * 4, s * 4), 0)
        ImageDraw.Draw(mask).ellipse([0, 0, s * 4 - 1, s * 4 - 1], fill=255)
        mask = mask.resize((s, s), Image.LANCZOS)
        for row in range(2):
            sheet.paste(im, (x, row * half + (half - s) // 2), mask)
        x += s + pad
    sheet.save(out_dir / "preview_sizes.png")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[1])
    ap.add_argument("--out", type=Path, default=HERE / "wtd-twitch-profile-800.png")
    ap.add_argument("--preview", type=Path, help="also write circle-cropped previews into this folder")
    args = ap.parse_args()
    img = render()
    img.save(args.out, optimize=True)
    print("wrote", args.out)
    if args.preview:
        previews(img, args.preview)
        print("previews in", args.preview)


if __name__ == "__main__":
    main()
