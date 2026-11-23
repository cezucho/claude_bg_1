"""Packs parts/*.png into spine/pyra.png + spine/pyra.atlas (Spine 4.x atlas format) for runtimes."""
import os, json
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); import sys; P = os.path.join(HERE, sys.argv[1] if len(sys.argv) > 1 else "parts"); S = os.path.join(HERE, sys.argv[2] if len(sys.argv) > 2 else "spine")
names = sorted(json.load(open(os.path.join(P, "parts.json"))), key=lambda n: -Image.open(os.path.join(P, n + ".png")).size[1])
W, pad = 1024, 2; x = y = pad; rowh = 0; place = {}
for n in names:
    w, h = Image.open(os.path.join(P, n + ".png")).size
    if x + w + pad > W: x = pad; y += rowh + pad; rowh = 0
    place[n] = (x, y, w, h); x += w + pad; rowh = max(rowh, h)
H = 1 << (y + rowh + pad - 1).bit_length()
page = Image.new("RGBA", (W, H))
for n, (x, y, w, h) in place.items(): page.paste(Image.open(os.path.join(P, n + ".png")), (x, y))
page.save(os.path.join(S, "pyra.png"))
lines = ["pyra.png", f"size:{W},{H}", "filter:Linear,Linear"] + sum([[n, f"bounds:{x},{y},{w},{h}"] for n, (x, y, w, h) in place.items()], [])
open(os.path.join(S, "pyra.atlas"), "w").write("\n".join(lines) + "\n")
print(W, H)
