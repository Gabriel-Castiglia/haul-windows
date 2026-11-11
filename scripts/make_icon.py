"""
Haul -- draws the application icon: an eagle bringing its catch, cut out of a 17th-century engraving.
Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
Date: November 11, 2026
SPDX-License-Identifier: GPL-2.0-only

This program is free software: you can redistribute it and/or modify it under the terms of
the GNU General Public License, version 2, as published by the Free Software Foundation.
It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

The eagle carrying a hare comes from "Eagle bringing a young woman a hare" by the Monogrammist B
(Italy, 17th century; Rijksmuseum, CC0). assets/ keeps the part of the scan with the eagle. An outline
traced by hand on a 1000 px wide view of that part keeps the woman, the hill and the town out; inside
it, the silhouette follows the engraving's own lines (closed and filled), so it is exact. The engraving
is redrawn in cream paper and ink on a flat rounded tile of the accent colour; the left wing, cut by the
edge of the plate in the print, runs off the left edge of the tile.

The same script serves both repositories. In haul-windows it writes:
  icono/Haul.ico                 the program's icon, on the default (orange) tile, 16-256 px
  icono/icono-256.png            the same, 256 px, for the windows and the documents
  icono/icono-1024.png           the same, 1024 px, for the manuals
  icono/accents/haul_<key>.ico   the icon on the tile of each accent colour
In haul (macOS, recognised by fuentes/Haul.swift) the tile is inset on the macOS icon grid and it writes:
  icono/icono-1024.png           the app icon, on the default tile
  icono/accents/<key>.png        the icon on the tile of each accent, 512 px, for the Dock
Both write build/icon_sheet.png, a look at every accent and size.

Run:  python scripts/make_icon.py      (needs numpy, opencv-python, scipy and Pillow)
"""
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
MAC = (ROOT / "fuentes" / "Haul.swift").exists()
SRC = ROOT / "assets" / "monogrammist_b_eagle_hare_rijksmuseum_cc0.jpg"
S = 1024
CREAM = np.array([246, 239, 222], np.float32)
INK = np.array([26, 24, 30], np.float32)

# ------------------------------------------------------------------ the outline
# Points on the 1000 px wide view of the scan in assets/ (multiply by VIEW to reach the scan). It only has
# to keep out what is not the eagle: the silhouette itself comes from the lines.
OUTLINE = [
    (76, 44), (200, 40), (330, 40), (440, 60), (530, 95), (575, 150), (592, 230), (640, 248), (720, 252),
    (800, 262), (860, 270), (905, 292), (935, 322), (930, 350), (905, 372), (870, 368), (800, 372),
    (730, 385), (700, 380), (668, 383), (700, 410), (760, 470), (800, 505), (850, 525), (895, 545),
    (918, 575), (905, 600), (862, 612), (860, 650), (848, 690), (862, 760), (868, 840), (850, 880),
    (835, 940), (822, 1005), (795, 1010), (770, 1000), (742, 940), (730, 880), (700, 860), (640, 870),
    (560, 880), (470, 885), (455, 940), (420, 975), (392, 968), (360, 935), (315, 905), (260, 892),
    (180, 890), (100, 896), (76, 896),
]

rgb = cv2.imread(str(SRC))
g = cv2.cvtColor(rgb, cv2.COLOR_BGR2GRAY)
VIEW = g.shape[1] / 1000
HOLE = 3000                                     # px of the scan: a hole larger than this is paper, not plumage
# Paper tone, then each pixel as a share of it: 1 is bare paper, 0 is full ink.
paper = cv2.GaussianBlur(cv2.dilate(g, np.ones((15, 15), np.uint8)), (0, 0), 20)
norm = np.clip(g.astype(np.float32) / np.maximum(paper.astype(np.float32), 1), 0, 1)

region = np.zeros(g.shape, np.uint8)
cv2.fillPoly(region, [np.array([(x * VIEW, y * VIEW) for x, y in OUTLINE], np.int32)], 1)
ink = (norm < 0.80) & (region > 0)
closed = cv2.morphologyEx(ink.astype(np.uint8), cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
filled = ndimage.binary_fill_holes(closed)
# Only the small holes are the eagle's own (between feathers, highlights); a large one is paper seen between the
# eagle, the hare and its ears, and stays open.
holes, nh = ndimage.label(filled & ~closed.astype(bool))
hsizes = ndimage.sum(np.ones_like(holes), holes, range(1, nh + 1))
filled &= ~np.isin(holes, 1 + np.nonzero(hsizes > HOLE)[0])
print("holes kept open:", sorted(int(v) for v in hsizes[hsizes > HOLE]), " largest filled:", int(max([v for v in hsizes if v <= HOLE] or [0])))
lab, n = ndimage.label(filled)
sizes = ndimage.sum(filled, lab, range(1, n + 1))
sil = np.isin(lab, 1 + np.nonzero(sizes > 400)[0])          # the eagle and the hare, without specks of the paper
sil = cv2.morphologyEx(sil.astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8)) > 0

ys, xs = np.nonzero(sil)
x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
norm_c = norm[y0:y1, x0:x1]
sil_c = sil[y0:y1, x0:x1].astype(np.float32)

# ------------------------------------------------------------------ placement
EW = 1000                                       # eagle width on the 1024 tile: the wing runs off the left edge
sc = EW / norm_c.shape[1]
EH = int(round(norm_c.shape[0] * sc))
d_norm = cv2.resize(norm_c, (EW, EH), interpolation=cv2.INTER_CUBIC)
d_sil = cv2.GaussianBlur(cv2.resize(sil_c, (EW, EH), interpolation=cv2.INTER_LINEAR), (0, 0), 1.0)
EX, EY = -6, (S - EH) // 2 + 24                 # a little low: the wing tips may leave the top, the hare's feet the bottom
top, bottom = max(0, -EY), max(0, EY + EH - S)
d_norm, d_sil = d_norm[top:EH - bottom], d_sil[top:EH - bottom]
EY, EH = EY + top, EH - top - bottom


def compose(bg, detail=1.0):
    """The whole drawing over a plain background of colour bg (float RGB). A lower detail lightens the
    hatching, so that at 16 to 32 px the eagle reads as one cream shape instead of grey noise."""
    img = np.empty((S, S, 3), np.float32)
    img[:] = bg
    sh = np.zeros((S, S), np.float32)
    xa, xb = max(0, EX), min(S, EX + EW)
    sh[EY:EY + EH, xa:xb] = d_sil[:, xa - EX:xb - EX]
    sh = np.pad(sh, ((12, 0), (8, 0)))[:S, :S]             # the shadow falls down and right
    sh = cv2.GaussianBlur(sh, (0, 0), 12) * 0.35
    img *= (1 - sh[..., None])
    reg = img[EY:EY + EH, xa:xb]
    tone = np.clip((d_norm[:, xa - EX:xb - EX] - 0.38) / 0.40, 0, 1)
    tone = 1 - (1 - tone[..., None]) * detail
    col = INK + (CREAM - INK) * tone
    a = d_sil[:, xa - EX:xb - EX][..., None]
    reg[:] = reg * (1 - a) + col * a
    return img


def tile_mask(inset):
    """The rounded tile: full bleed for Windows, on the macOS icon grid (824 of 1024) for the Mac."""
    m = Image.new("L", (S * 4, S * 4), 0)
    a, b = (100 * 4, (S - 100) * 4 - 1) if inset else (0, S * 4 - 1)
    ImageDraw.Draw(m).rounded_rectangle((a, a, b, b), radius=(185 if inset else 200) * 4, fill=255)
    return np.array(m.resize((S, S), Image.LANCZOS), np.float32) / 255


def layers(detail):
    """The drawing (premultiplied) and how much of the tile shows through it, from drawing it on black and on white."""
    on0, on1 = compose(np.zeros(3, np.float32), detail), compose(np.full(3, 255, np.float32), detail)
    return np.clip(on0, 0, 255), np.clip((on1 - on0).mean(axis=2) / 255, 0, 1)


fg, k = layers(1.0)
fg_small, k_small = layers(0.45)


def icon(rgb, size=S, inset=False):
    """The icon on a tile of colour rgb, as an RGBA image of the given size."""
    f, kk = (fg, k) if size > 32 else (fg_small, k_small)
    pre = (f + np.array(rgb, np.float32) * kk[..., None])
    if inset:                                   # the drawing shrinks onto the inset tile
        pre = cv2.resize(pre, (824, 824), interpolation=cv2.INTER_AREA)
        pre = np.pad(pre, ((100, 100), (100, 100), (0, 0)))
    t = tile_mask(inset)
    pre = pre * t[..., None]
    alpha = t * 255
    if inset:                                   # the soft shadow of macOS icons under the tile
        shadow = cv2.GaussianBlur(np.pad(t, ((10, 0), (0, 0)))[:S], (0, 0), 14) * 0.28 * 255
        alpha = alpha + shadow * (1 - t)
    rgba = np.dstack([pre, alpha])
    if size != S:
        rgba = cv2.resize(rgba, (size, size), interpolation=cv2.INTER_AREA)
    a = rgba[..., 3:4]
    rgb_ = np.where(a > 0, rgba[..., :3] * 255 / np.maximum(a, 1e-6), 0)
    return Image.fromarray(np.clip(np.dstack([rgb_, a]), 0, 255).round().astype(np.uint8), "RGBA")


# The tile colours of the twelve accents: the light tone of each one, the default first (orange, which no other
# icon of the family uses: Gryphmark is gold, Wyrmhold black, Citadel green and Muelle blue).
TILES = {"ember": "D9652F", "gold": "9A6F00", "purple": "5B2A86", "sky": "1A8CD8", "red": "C8102E", "yellow": "B88A00",
         "green": "1E8A43", "teal": "00796B", "blue": "1F5FD1", "pink": "C2185B", "black": "1D1D1F", "white": "8E8E93"}
TILES = {key: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) for key, v in TILES.items()}
(ROOT / "build").mkdir(exist_ok=True)
(ROOT / "icono" / "accents").mkdir(parents=True, exist_ok=True)
sizes = [256, 64, 48, 40, 32, 24, 20, 16]

if MAC:
    icon(TILES["ember"], 1024, inset=True).save(ROOT / "icono" / "icono-1024.png")
    for key, rgb_ in TILES.items():
        icon(rgb_, 512, inset=True).save(ROOT / "icono" / "accents" / f"{key}.png")
else:
    def write_ico(path, rgb_):
        imgs = [icon(rgb_, z) for z in sizes]
        imgs[0].save(path, sizes=[(z, z) for z in sizes], append_images=imgs[1:])
    for key, rgb_ in TILES.items():
        write_ico(ROOT / "icono" / "accents" / f"haul_{key}.ico", rgb_)
    write_ico(ROOT / "icono" / "Haul.ico", TILES["ember"])
    icon(TILES["ember"], 256).save(ROOT / "icono" / "icono-256.png")
    icon(TILES["ember"], 1024).save(ROOT / "icono" / "icono-1024.png")

sheet = Image.new("RGB", (6 * 170 + 20, 2 * 250 + 20), (246, 246, 248))
for i, t in enumerate(TILES.values()):
    x, y = 20 + (i % 6) * 170, 20 + (i // 6) * 250
    big = icon(t, 150, inset=MAC)
    sheet.paste(big, (x, y), big)
    sx = x
    for z in (48, 32, 16):
        im = icon(t, z, inset=MAC)
        sheet.paste(im, (sx, y + 170 + (48 - z) // 2), im)
        sx += z + 10
sheet.save(ROOT / "build" / "icon_sheet.png")
icon(TILES["ember"], 1024, inset=MAC).save(ROOT / "build" / "icon_1024.png")
print("icon written for", "macOS" if MAC else "Windows")
