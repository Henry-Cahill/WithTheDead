"""Makes the WithTheDead YouTube live-stream thumbnail: 1280x720, a JPEG under YouTube's 2 MB limit.

It puts the stream's lettering over the painted background (thumbnail-background.png) in the brand's style: the red
stencil and the snow of the Twitch profile picture (../Twitch/make_profile_pic.py), a LIVE badge and a subtitle that
says what the stream is. Make one for another kind of stream with --subtitle:

    python make_thumbnail.py
    python make_thumbnail.py --subtitle "PLAYTEST: CAN I MAKE IT OUT?" --out wtd-playtest-thumbnail.jpg
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
FONTS = Path(r"C:\Windows\Fonts")
W, H = 1280, 720
LEFT = 58


def font(size: int, *names: str, variation: str | None = None) -> ImageFont.FreeTypeFont:
    for name in names:
        if (FONTS / name).exists():
            f = ImageFont.truetype(str(FONTS / name), size)
            if variation:
                try:
                    f.set_variation_by_name(variation)
                except (OSError, ValueError):
                    pass
            return f
    return ImageFont.load_default(size)


def background() -> Image.Image:
    """The painting cut to 16:9, darkened on the left where the lettering goes."""
    img = Image.open(HERE / "thumbnail-background.png").convert("RGB")
    w, h = img.size
    if w * 9 > h * 16:
        cw = h * 16 // 9
        img = img.crop(((w - cw) // 2, 0, (w - cw) // 2 + cw, h))
    else:
        ch = w * 9 // 16
        img = img.crop((0, (h - ch) // 2, w, (h - ch) // 2 + ch))
    img = img.resize((W, H), Image.LANCZOS).convert("RGBA")
    shade = np.zeros((H, W, 4), np.uint8)
    shade[..., :3] = (3, 5, 11)
    shade[..., 3] = (np.clip(1 - np.linspace(0, 1, W) / 0.6, 0, 1) ** 1.6 * 200)[None, :].astype(np.uint8)
    return Image.alpha_composite(img, Image.fromarray(shade, "RGBA"))


def place(text: str, f: ImageFont.FreeTypeFont, x: int, top: int, spacing: int = 0) -> tuple[Image.Image, tuple]:
    """The text as a mask, its glyphs' top edge at `top`, with extra space between letters; and its bounding box."""
    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)
    y = top - d.textbbox((0, 0), text, font=f)[1]
    for ch in text:
        d.text((x, y), ch, font=f, fill=255)
        x += d.textlength(ch, font=f) + spacing
    return mask, mask.getbbox()


def tint(mask: Image.Image, rgb: tuple, alpha: float = 1.0) -> Image.Image:
    layer = Image.new("RGBA", (W, H), rgb + (0,))
    layer.putalpha(mask.point(lambda v: int(v * alpha)))
    return layer


def lettered(img: Image.Image, mask: Image.Image, rgb: tuple, shadow: float = 0.85) -> Image.Image:
    """Plain lettering with a soft dark shadow under it, for reading against the painting."""
    img = Image.alpha_composite(img, tint(ImageChops.offset(mask, 3, 4).filter(ImageFilter.GaussianBlur(6)), (1, 2, 4), shadow))
    return Image.alpha_composite(img, tint(mask, rgb))


def red_stencil(img: Image.Image, mask: Image.Image, seed: int = 43) -> Image.Image:
    """The brand's red stencil: a red glow, a dark edge, a top-lit red with grain, and snow lying on every top."""
    img = Image.alpha_composite(img, tint(mask.filter(ImageFilter.GaussianBlur(28)), (255, 51, 36), 0.42))
    img = Image.alpha_composite(img, tint(ImageChops.offset(mask, 7, 11).filter(ImageFilter.GaussianBlur(6)), (1, 2, 4), 0.9))
    img = Image.alpha_composite(img, tint(mask.filter(ImageFilter.MaxFilter(9)), (22, 6, 10), 0.95))

    rng = np.random.default_rng(seed)
    x0, y0, x1, y1 = mask.getbbox()
    t = np.clip((np.arange(H) - y0) / max(1, y1 - y0), 0, 1)[:, None, None]
    red = np.array([230, 68, 58]) * (1 - t) + np.array([147, 27, 20]) * t
    red = np.clip(red + rng.normal(0, 8, (H, W, 1)), 0, 255).astype(np.uint8)
    face = Image.fromarray(np.broadcast_to(red, (H, W, 3)).copy(), "RGB").convert("RGBA")
    face.putalpha(mask)
    img = Image.alpha_composite(img, face)

    # Snow: how far each letter pixel is below the nearest top edge, against a lumpy depth that changes along the row.
    inside = np.array(mask) > 127
    depth = np.zeros((H, W), np.int32)
    run = np.zeros(W, np.int32)
    for y in range(H):
        run = np.where(inside[y], run + 1, 0)
        depth[y] = run
    lumps = np.convolve(rng.normal(0, 1, W + 80), np.ones(40) / 40, mode="same")[40:40 + W]
    cap_depth = 9 + 9 * (lumps - lumps.min()) / max(1e-6, np.ptp(lumps))
    cap = Image.fromarray(((depth > 0) & (depth <= cap_depth[None, :])).astype(np.uint8) * 255, "L")
    cap = cap.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
    under = Image.fromarray(((depth > cap_depth[None, :]) & (depth <= cap_depth[None, :] + 6)).astype(np.uint8) * 255, "L")
    img = Image.alpha_composite(img, tint(under.filter(ImageFilter.GaussianBlur(2)), (40, 6, 6), 0.45))
    img = Image.alpha_composite(img, tint(cap, (214, 226, 240)))
    return Image.alpha_composite(img, tint(ImageChops.offset(cap, 0, -2), (251, 253, 255), 0.9))


def live_badge(img: Image.Image) -> Image.Image:
    f = font(46, "bahnschrift.ttf", "arialbd.ttf", variation="Bold")
    d = ImageDraw.Draw(img)
    x, y, h = LEFT, 42, 68
    w = int(d.textlength("LIVE", font=f)) + 92
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).rounded_rectangle((x + 3, y + 5, x + w + 3, y + h + 5), radius=12, fill=255)
    img = Image.alpha_composite(img, tint(shadow.filter(ImageFilter.GaussianBlur(6)), (0, 0, 0), 0.7))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x, y, x + w, y + h), radius=12, fill=(222, 32, 26, 255))
    d.ellipse((x + 24, y + h // 2 - 11, x + 46, y + h // 2 + 11), fill=(255, 255, 255, 255))
    top = d.textbbox((0, 0), "LIVE", font=f)
    d.text((x + 62, y + (h - (top[3] - top[1])) // 2 - top[1]), "LIVE", font=f, fill=(255, 255, 255, 255))
    return img


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--subtitle", default="BUILDING A ZOMBIE SURVIVAL GAME")
    ap.add_argument("--out", default="wtd-live-thumbnail.jpg")
    args = ap.parse_args()

    img = live_badge(background())
    with_mask, wb = place("WITH THE", font(84, "bahnschrift.ttf", "arialbd.ttf", variation="Bold"), LEFT + 6, 160, 10)
    img = lettered(img, with_mask, (238, 242, 248))

    # DEAD as wide as the left side allows, under WITH THE.
    stencil = ("STENCIL.TTF", "impact.ttf")
    probe = ImageDraw.Draw(Image.new("L", (1, 1)))
    size = int(250 * 640 / (probe.textlength("DEAD", font=font(250, *stencil)) + 18))
    dead, db = place("DEAD", font(size, *stencil), LEFT, wb[3] + 26, 6)
    img = red_stencil(img, dead)

    sf = font(40, "bahnschrift.ttf", "arialbd.ttf", variation="SemiBold")
    sub, sb = place(args.subtitle.upper(), sf, LEFT + 20, db[3] + 44, 3)
    bar = Image.new("L", (W, H), 0)
    ImageDraw.Draw(bar).rounded_rectangle((LEFT, sb[1] - 16, sb[2] + 20, sb[3] + 16), radius=10, fill=255)
    img = Image.alpha_composite(img, tint(bar, (3, 5, 11), 0.72))
    ImageDraw.Draw(img).rectangle((LEFT, sb[1] - 16, LEFT + 6, sb[3] + 16), fill=(222, 32, 26, 255))
    img = lettered(img, sub, (238, 242, 248), 0.6)

    out = HERE / args.out
    img.convert("RGB").save(out, "JPEG", quality=92, optimize=True, progressive=True)
    print("%s: %dx%d, %d KB" % (out.name, W, H, out.stat().st_size // 1024))


if __name__ == "__main__":
    main()
