"""Keys out the green, finds connected parts on a parts sheet, writes each as a transparent PNG and a labelled overview."""
import sys, os, numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
src = sys.argv[1]; out = sys.argv[2]; os.makedirs(out, exist_ok=True)
im = np.asarray(Image.open(src).convert("RGB")).astype(np.int32)
r, g, b = im[..., 0], im[..., 1], im[..., 2]
greenness = g - np.maximum(r, b)                  # background and its darker ground shadow are strongly green
alpha = np.clip((90 - greenness) / 50.0, 0, 1)    # 40 -> opaque, 90 -> transparent: soft edge
solid = alpha > 0.5
solid = ndi.binary_opening(solid, iterations=2)
lab, n = ndi.label(solid)
sizes = ndi.sum(solid, lab, range(1, n + 1))
rgb = im.astype(np.float64)
# despill: pull green down to max(r,b) on edge pixels
spill = np.clip(rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]), 0, None)
rgb[..., 1] -= spill * (alpha < 0.999)
ov = Image.open(src).convert("RGB"); d = ImageDraw.Draw(ov)
k = 0
for i, s in enumerate(sizes, 1):
    if s < 6000: continue
    ys, xs = np.where(ndi.binary_dilation(lab == i, iterations=3))
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    m = ndi.binary_dilation(lab == i, iterations=3)[y0:y1, x0:x1]
    a = (alpha[y0:y1, x0:x1] * m * 255).astype(np.uint8)
    rgba = np.dstack([rgb[y0:y1, x0:x1].clip(0, 255).astype(np.uint8), a])
    Image.fromarray(rgba).save(os.path.join(out, f"c{k:02d}.png"))
    d.rectangle([x0, y0, x1, y1], outline=(255, 255, 255), width=2); d.text((x0 + 4, y0 + 2), f"c{k:02d}", fill=(255, 255, 0))
    print(f"c{k:02d} box=({x0},{y0},{x1},{y1}) px={int(s)}"); k += 1
ov.save(os.path.join(out, "_overview.png"))
