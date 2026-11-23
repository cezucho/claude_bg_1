"""Cuts named parts out of parts_v2_2.png -> parts2/*.png (half resolution) and parts2/parts.json."""
import os, sys, json, numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "parts2"); os.makedirs(OUT, exist_ok=True)
SRC = os.path.join(HERE, "parts_v2_2.png")
im = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float64)
r, g, b = im[..., 0], im[..., 1], im[..., 2]
gr = g - np.maximum(r, b)
alpha = np.clip((70 - gr) / 35.0, 0, 1)
alpha[(gr > 25) & (g < 140)] = 0
solid = ndi.binary_opening(alpha > 0.5, iterations=2)
lab, n = ndi.label(solid)
sizes = ndi.sum(solid, lab, range(1, n + 1))
comps = [i for i, s in enumerate(sizes, 1) if s >= 6000]            # same order as split_parts.py's c00..
im[..., 1] -= np.clip(gr, 0, None)
lum = im.mean(2)
BOXES = {"head": (52,16,402,461), "hair_back": (421,89,863,573), "torso": (881,105,1188,468), "cape": (1627,119,2016,530),
         "skirt": (1226,128,1615,466), "upper_arm_R": (146,663,290,1061), "upper_arm_L": (475,664,618,1061),
         "forearm_R": (811,678,962,1043), "forearm_L": (1084,678,1235,1043), "hand_grip": (1700,724,1925,979),
         "hand_open": (1379,775,1615,953), "staff": (1378,1155,1530,1949), "flame": (1711,1201,1869,1471),
         "leg_R": (122,1246,329,1911), "leg_L": (448,1259,698,1907), "fireball": (1565,1676,1947,1881)}   # from split_parts.py
def pick(box):
    x0, y0, x1, y1 = box; ids, cnt = np.unique(lab[y0:y1, x0:x1][lab[y0:y1, x0:x1] > 0], return_counts=True)
    return ids[np.argmax(cnt)]
CUTS = json.load(open(os.path.join(HERE, "cuts_v2.json"))) if os.path.exists(os.path.join(HERE, "cuts_v2.json")) else {}
def save(name, m):
    m = ndi.binary_dilation(m, iterations=2)
    ys, xs = np.where(m); x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    a = (alpha * m)[y0:y1, x0:x1]
    rgba = Image.fromarray(np.dstack([im[y0:y1, x0:x1].clip(0, 255), a * 255]).astype(np.uint8))
    w, h = rgba.size; rgba = rgba.resize((round(w / 2), round(h / 2)), Image.LANCZOS)
    rgba.save(os.path.join(OUT, name + ".png")); return [int(x0) / 2, int(y0) / 2, int(x1) / 2, int(y1) / 2]
meta = {}
for name, box in BOXES.items():
    m = lab == pick(box)
    if name.startswith("upper_arm") and name in CUTS:                 # keep pad + sleeve, drop the bracer
        m[int(CUTS[name] * 2):] = False
    if name.startswith("leg") and name in CUTS:
        side = name[-1]; tb, cuff, bt = CUTS[name]                      # thigh bottom, boot cuff top, boot top (half-res sheet px)
        yy = np.arange(m.shape[0])[:, None]
        thigh = m & (yy < tb * 2) & ~((yy > cuff * 2) & ((lum > 75) | (r - b > 35)))
        thigh = ndi.binary_opening(thigh, iterations=2); l2, n2 = ndi.label(thigh)
        if n2 > 1: thigh = l2 == (np.argmax(ndi.sum(thigh, l2, range(1, n2 + 1))) + 1)
        meta["thigh_" + side] = save("thigh_" + side, thigh)
        meta["boot_" + side] = save("boot_" + side, m & (yy >= bt * 2))
        continue
    meta[name] = save(name, m)
json.dump(meta, open(os.path.join(OUT, "parts.json"), "w"), indent=1)
# grid contact sheet: every part on a 20 px grid with its own pixel coordinates, for reading pivots
names = list(meta); W = 1500; sheet = Image.new("RGB", (W, 1400), (70, 75, 85)); d = ImageDraw.Draw(sheet)
x = y = 30; rowh = 0
for nm in names:
    p = Image.open(os.path.join(OUT, nm + ".png"))
    if x + p.width + 30 > W: x = 30; y += rowh + 40; rowh = 0
    for gx in range(0, p.width + 1, 20): d.line([(x + gx, y), (x + gx, y + p.height)], fill=(95, 100, 110) if gx % 100 else (140, 140, 160))
    for gy in range(0, p.height + 1, 20): d.line([(x, y + gy), (x + p.width, y + gy)], fill=(95, 100, 110) if gy % 100 else (140, 140, 160))
    sheet.paste(p, (x, y), p); d.text((x, y - 14), f"{nm} {p.width}x{p.height}", fill=(255, 255, 0))
    x += p.width + 30; rowh = max(rowh, p.height)
sheet.save(os.path.join(HERE, "previews", "_grid_v2.png")); print(meta)
