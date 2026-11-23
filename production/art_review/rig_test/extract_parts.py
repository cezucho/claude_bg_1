"""Cuts named parts out of parts_sheet_0.png -> parts/*.png and parts/parts.json (box in sheet coords)."""
import os, json, numpy as np
from PIL import Image
from scipy import ndimage as ndi
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "parts"); os.makedirs(OUT, exist_ok=True)
im = np.asarray(Image.open(os.path.join(HERE, "parts_sheet_0.png")).convert("RGB")).astype(np.float64)
r, g, b = im[..., 0], im[..., 1], im[..., 2]
gr = g - np.maximum(r, b)
alpha = np.clip((70 - gr) / 35.0, 0, 1)
alpha[(gr > 25) & (g < 140)] = 0                  # dark-green ground shadow under the boots
solid = ndi.binary_opening(alpha > 0.5, iterations=2)
lab, _ = ndi.label(solid)
spill = np.clip(g - np.maximum(r, b), 0, None); im[..., 1] -= spill
lum = im.mean(2)
# name: (component seed point, extra clip (x0,y0,x1,y1) in sheet coords or None, optional mask fn)
P = {
 "head": ((140, 150), None), "hair_back": ((380, 200), None), "torso": ((600, 150), None),
 "skirt": ((860, 220), None), "cape": ((180, 450), None),
 "upper_arm": ((450, 400), (378, 334, 502, 462)), "forearm": ((410, 500), (378, 452, 502, 558)),
 "hand_open": ((110, 670), None), "hand_grip": ((290, 650), None),
 "thigh_L": ((470, 680), (340, 600, 516, 775)), "boot_L": ((420, 870), (340, 748, 516, 955)),
 "thigh_R": ((580, 680), (531, 598, 700, 775)), "boot_R": ((620, 870), (531, 748, 700, 945)),
 "staff": ((890, 750), None), "flame": ((160, 880), None),
}
meta = {}
for name, (seed, clip) in P.items():
    m = lab == lab[seed[1], seed[0]]
    if clip:
        c = np.zeros_like(m); x0, y0, x1, y1 = clip; c[y0:y1, x0:x1] = True; m &= c
        if name.startswith("thigh"):               # drop the brown/gold boot cuff that pokes above the knee cut
            yy = np.arange(m.shape[0])[:, None]
            m &= ~((yy > 742) & ((lum > 75) | (r - b > 35)))
            m = ndi.binary_opening(m, iterations=2); lab2, n2 = ndi.label(m)
            if n2 > 1:
                big = np.argmax(ndi.sum(m, lab2, range(1, n2 + 1))) + 1; m = lab2 == big
    if name == "staff":                            # keep the glow sparks: everything in the staff column
        m = ((alpha > 0.05) & (np.indices(m.shape)[1] > 820) & (np.indices(m.shape)[0] > 470))
    m = ndi.binary_dilation(m, iterations=2)
    ys, xs = np.where(m); x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    a = (alpha * m)[y0:y1, x0:x1]
    rgba = np.dstack([im[y0:y1, x0:x1].clip(0, 255), a * 255]).astype(np.uint8)
    Image.fromarray(rgba).save(os.path.join(OUT, name + ".png"))
    meta[name] = [int(x0), int(y0), int(x1), int(y1)]
json.dump(meta, open(os.path.join(OUT, "parts.json"), "w"), indent=1)
print(meta)
# contact sheet on grey to check edges
W = 1100; sheet = Image.new("RGBA", (W, 1000), (90, 90, 100, 255)); x = y = 10; rowh = 0
for n in P:
    p = Image.open(os.path.join(OUT, n + ".png"))
    if x + p.width > W: x = 10; y += rowh + 10; rowh = 0
    sheet.alpha_composite(p, (x, y)); x += p.width + 10; rowh = max(rowh, p.height)
sheet.save(os.path.join(HERE, "parts_contact.png"))
