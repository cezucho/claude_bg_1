"""Pyra cut-out rig: one description -> Spine 4.2 JSON (spine/pyra.json + spine/images) and a preview renderer.

Coordinates: character space, origin between the feet on the ground, y up, degrees counter-clockwise (Spine's).
All bones have rotation 0 in the setup pose; the setup pose is the parts sheet assembled. Animations carry the pose.
Usage: python rig.py            -> writes spine/, previews/*.gif, previews/setup.png
"""
import os, json, math, shutil
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PARTS = os.path.join(HERE, "parts")
SIZES = {n: Image.open(os.path.join(PARTS, n + ".png")).size for n in json.load(open(os.path.join(PARTS, "parts.json")))}

def sheet(x, y):        # the legs keep their placement from the parts sheet: ground at y=950, centre at x=524
    return (x - 524, 950 - y)

# ---- bones: name -> (parent, world x, world y) in the setup pose ------------------------------------------------
J = {
 "root": (None, 0, 0),
 "hip": ("root", 0, 360),
 "thigh_L": ("hip", *sheet(450, 628)), "knee_L": ("thigh_L", *sheet(447, 752)),
 "thigh_R": ("hip", *sheet(586, 628)), "knee_R": ("thigh_R", *sheet(603, 752)),
 "skirt": ("hip", 0, 385),
 "torso": ("hip", 0, 375),
 "cape": ("torso", 0, 515),
 "head": ("torso", 5, 520),
 "hair": ("head", 5, 650),
 "shoulder_L": ("torso", -66, 496), "elbow_L": ("shoulder_L", -106, 411), "wrist_L": ("elbow_L", -111, 326),
 "shoulder_R": ("torso", 75, 496), "elbow_R": ("shoulder_R", 115, 411), "wrist_R": ("elbow_R", 120, 326),
 "staff": ("wrist_R", 176, 318),
 "flame": ("wrist_L", -170, 380),
}

# ---- attachments: slot -> (bone, image, pin in image px (y down), pin in world, flipX) -------------------------
# Listed back to front: this is the draw order.
A = [
 ("cape",        "cape",      "cape",      (141, 8),   (0, 520),    False),
 ("hair_back",   "hair",      "hair_back", (130, 10),  (5, 712),    False),
 ("thigh_L",     "thigh_L",   "thigh_L",   None, None, False),
 ("thigh_R",     "thigh_R",   "thigh_R",   None, None, False),
 ("boot_L",      "knee_L",    "boot_L",    None, None, False),
 ("boot_R",      "knee_R",    "boot_R",    None, None, False),
 ("skirt",       "skirt",     "skirt",     (116, 4),   (0, 392),    False),
 ("torso",       "torso",     "torso",     (88, 189),  (0, 360),    False),
 ("head",        "head",      "head",      (92, 246),  (5, 512),    False),
 ("upper_arm_R", "shoulder_R","upper_arm", (65, 30),   (75, 496),   True),
 ("forearm_R",   "elbow_R",   "forearm",   (35, 10),   (115, 411),  True),
 ("staff",       "staff",     "staff",     (62, 300),  (176, 318),  False),
 ("hand_R",      "wrist_R",   "hand_grip", (20, 82),   (120, 326),  False),
 ("upper_arm_L", "shoulder_L","upper_arm", (65, 30),   (-66, 496),  False),
 ("forearm_L",   "elbow_L",   "forearm",   (35, 10),   (-106, 411), False),
 ("hand_L",      "wrist_L",   "hand_open", (118, 64),  (-111, 326), False),
 ("flame",       "flame",     "flame",     (51, 150),  (-170, 380), False),
]
BOX = json.load(open(os.path.join(PARTS, "parts.json")))

def attachment_offsets():
    """Attachment centre relative to its bone (bones are unrotated in setup, so this is a plain difference)."""
    out = {}
    for slot, bone, img, pin, at, flip in A:
        w, h = SIZES[img]
        if pin is None:                                   # legs: keep the sheet placement exactly
            x0, y0, x1, y1 = BOX[img]; cx, cy = sheet((x0 + x1) / 2, (y0 + y1) / 2)
        else:
            px = (w - pin[0]) if flip else pin[0]
            cx = at[0] + (w / 2 - px); cy = at[1] - (h / 2 - pin[1])
        bx, by = J[bone][1], J[bone][2]
        out[slot] = dict(bone=bone, image=img, x=round(cx - bx, 2), y=round(cy - by, 2), w=w, h=h, sx=-1 if flip else 1)
    return out

# ---- animations ---------------------------------------------------------------------------------------------
# Each channel: list of (time, value) keys, eased in-out between keys. rotate in degrees; translate (x, y); scale (x, y).
def loop(t, period, amp, phase=0.0):
    return amp * math.sin(2 * math.pi * (t / period + phase))

POSE = {  # the ready pose, added to every key of every animation
 "shoulder_L": -40, "elbow_L": -45, "wrist_L": 80,
 "shoulder_R": 12, "elbow_R": 6, "wrist_R": -18,
 "thigh_L": -3, "knee_L": 3, "thigh_R": 4, "knee_R": -4,
}

def sampled(period, fn, steps=8):
    return [(round(period * i / steps, 4), fn(period * i / steps)) for i in range(steps + 1)]

IDLE_T = 2.0
ANIMS = {
 "idle": {
  "rotate": {
   "torso":      sampled(IDLE_T, lambda t: loop(t, IDLE_T, 1.5)),
   "head":       sampled(IDLE_T, lambda t: loop(t, IDLE_T, -2.0, 0.1)),
   "hair":       sampled(IDLE_T, lambda t: loop(t, IDLE_T, 2.5, 0.25)),
   "cape":       sampled(IDLE_T, lambda t: loop(t, IDLE_T, 3.0, 0.3)),
   "shoulder_L": sampled(IDLE_T, lambda t: loop(t, IDLE_T, 4.0, 0.15)),
   "elbow_L":    sampled(IDLE_T, lambda t: loop(t, IDLE_T, 3.0, 0.2)),
   "wrist_L":    sampled(IDLE_T, lambda t: loop(t, IDLE_T, -6.0, 0.2)),
   "shoulder_R": sampled(IDLE_T, lambda t: loop(t, IDLE_T, -2.0, 0.1)),
   "skirt":      sampled(IDLE_T, lambda t: loop(t, IDLE_T, -1.0, 0.2)),
  },
  "translate": {
   "torso": sampled(IDLE_T, lambda t: (0, -3 + loop(t, IDLE_T, 3, 0.25))),
   "flame": sampled(IDLE_T, lambda t: (0, loop(t, IDLE_T / 2, 6))),
  },
  "scale": {
   "flame": sampled(IDLE_T, lambda t: (1 + loop(t, IDLE_T / 4, 0.05), 1 + loop(t, IDLE_T / 4, 0.09, 0.25)), 16),
  },
 },
 "cast": {   # wind up, throw the flame to screen-left, recover. 1.2 s
  "rotate": {
   "torso":      [(0, 0), (0.30, -7), (0.45, 9), (0.80, 6), (1.2, 0)],
   "head":       [(0, 0), (0.30, 4), (0.45, -5), (0.80, -3), (1.2, 0)],
   "hair":       [(0, 0), (0.30, -4), (0.50, 9), (0.80, 5), (1.2, 0)],
   "cape":       [(0, 0), (0.30, -6), (0.50, 12), (0.85, 6), (1.2, 0)],
   "shoulder_L": [(0, 0), (0.30, 50), (0.45, -30), (0.80, -25), (1.2, 0)],
   "elbow_L":    [(0, 0), (0.30, 40), (0.45, 38), (0.80, 32), (1.2, 0)],
   "wrist_L":    [(0, 0), (0.30, -25), (0.45, -60), (0.80, -50), (1.2, 0)],
   "shoulder_R": [(0, 0), (0.30, 20), (0.45, -8), (1.2, 0)],
   "elbow_R":    [(0, 0), (0.30, 25), (0.45, 0), (1.2, 0)],
   "thigh_L":    [(0, 0), (0.45, -4), (1.2, 0)],
   "thigh_R":    [(0, 0), (0.45, 5), (1.2, 0)],
  },
  "translate": {
   "torso": [(0, (0, 0)), (0.30, (6, -6)), (0.45, (-10, -2)), (0.80, (-6, -2)), (1.2, (0, 0))],
   "flame": [(0, (0, 0)), (0.30, (10, 10)), (0.45, (0, 0)), (0.75, (-330, 30)), (0.76, (0, 0)), (1.0, (0, 0)), (1.2, (0, 0))],
  },
  "scale": {
   "flame": [(0, (1, 1)), (0.30, (1.5, 1.5)), (0.45, (1.6, 1.4)), (0.75, (2.2, 1.6)), (0.76, (0.2, 0.2)), (1.0, (0.2, 0.2)), (1.2, (1, 1))],
  },
  "alpha": {
   "flame": [(0, 1), (0.6, 1), (0.75, 0), (1.0, 0), (1.2, 1)],
  },
 },
 "hit": {   # knocked back from screen-left. 0.5 s
  "rotate": {
   "torso": [(0, 0), (0.08, -12), (0.5, 0)], "head": [(0, 0), (0.08, -8), (0.5, 0)],
   "hair": [(0, 0), (0.10, 10), (0.5, 0)], "cape": [(0, 0), (0.10, 10), (0.5, 0)],
   "shoulder_L": [(0, 0), (0.08, 25), (0.5, 0)], "shoulder_R": [(0, 0), (0.08, 15), (0.5, 0)],
  },
  "translate": {"hip": [(0, (0, 0)), (0.08, (14, -6)), (0.5, (0, 0))]},
  "color": {s: [(0, "ffffffff"), (0.04, "ff9a9aff"), (0.2, "ffffffff")] for s in [a[0] for a in A]},
 },
}

def ease(u):
    return u * u * (3 - 2 * u)

def sample(keys, t):
    if t <= keys[0][0]: return keys[0][1]
    for (t0, v0), (t1, v1) in zip(keys, keys[1:]):
        if t <= t1:
            u = 0 if t1 == t0 else ease((t - t0) / (t1 - t0))
            if isinstance(v0, tuple): return tuple(a + (b - a) * u for a, b in zip(v0, v1))
            if isinstance(v0, str): return v0 if u < 1 else v1
            return v0 + (v1 - v0) * u
    return keys[-1][1]

def duration(anim):
    return max(k[-1][0] for ch in anim.values() for k in ch.values())

# ---- Spine JSON ---------------------------------------------------------------------------------------------
def bezier(t0, v0, t1, v1):
    d = (t1 - t0) / 3
    return [round(t0 + d, 4), v0, round(t1 - d, 4), v1]

def spine_json():
    att = attachment_offsets()
    bones = []
    for name, (parent, x, y) in J.items():
        b = {"name": name}
        if parent:
            b["parent"] = parent; b["x"] = round(x - J[parent][1], 2); b["y"] = round(y - J[parent][2], 2)
        bones.append(b)
    slots = [{"name": s, "bone": att[s]["bone"], "attachment": s} for s, *_ in A]
    skin = {}
    for s, *_ in A:
        a = att[s]; d = {"x": a["x"], "y": a["y"], "width": a["w"], "height": a["h"]}
        if a["image"] != s: d["path"] = a["image"]
        if a["sx"] == -1: d["scaleX"] = -1
        skin[s] = {s: d}
    anims = {}
    for an, ch in ANIMS.items():
        out = {"bones": {}, "slots": {}}
        bone_names = set(ch.get("rotate", {})) | set(ch.get("translate", {})) | set(ch.get("scale", {})) | set(POSE)
        for bn in bone_names:
            tl = {}
            rk = ch.get("rotate", {}).get(bn, [(0, 0)])
            base = POSE.get(bn, 0)
            if bn in ch.get("rotate", {}) or base:
                keys = []
                for i, (t, v) in enumerate(rk):
                    k = {"time": t, "value": round(v + base, 3)}
                    if i + 1 < len(rk): k["curve"] = bezier(t, round(v + base, 3), rk[i + 1][0], round(rk[i + 1][1] + base, 3))
                    keys.append(k)
                tl["rotate"] = keys
            for kind in ("translate", "scale"):
                if bn in ch.get(kind, {}):
                    ks = ch[kind][bn]; keys = []
                    for i, (t, (x, y)) in enumerate(ks):
                        k = {"time": t, "x": round(x, 3), "y": round(y, 3)}
                        if i + 1 < len(ks):
                            t1, (x1, y1) = ks[i + 1]
                            k["curve"] = bezier(t, round(x, 3), t1, round(x1, 3)) + bezier(t, round(y, 3), t1, round(y1, 3))
                        keys.append(k)
                    tl[kind] = keys
            out["bones"][bn] = tl
        for sn, ks in ch.get("alpha", {}).items():
            out["slots"][sn] = {"rgba": [{"time": t, "color": "ffffff%02x" % int(round(v * 255))} for t, v in ks]}
        for sn, ks in ch.get("color", {}).items():
            out["slots"][sn] = {"rgba": [{"time": t, "color": c} for t, c in ks]}
        if not out["slots"]: del out["slots"]
        anims[an] = out
    return {"skeleton": {"spine": "4.2.00", "images": "./images/", "width": 640, "height": 820},
            "bones": bones, "slots": slots, "skins": [{"name": "default", "attachments": skin}], "animations": anims}

# ---- preview renderer ---------------------------------------------------------------------------------------
IMGS = {n: Image.open(os.path.join(PARTS, n + ".png")).convert("RGBA") for n in SIZES}

def mat(x, y, r, sx=1, sy=1):
    c, s = math.cos(math.radians(r)), math.sin(math.radians(r))
    return np.array([[c * sx, -s * sy, x], [s * sx, c * sy, y], [0, 0, 1]])

def pose_at(anim, t):
    ch = ANIMS[anim] if anim else {}
    world = {}
    for name, (parent, x, y) in J.items():
        lx, ly = (x - J[parent][1], y - J[parent][2]) if parent else (x, y)
        r = POSE.get(name, 0) if anim else 0
        sx = sy = 1
        if name in ch.get("rotate", {}): r += sample(ch["rotate"][name], t)
        if name in ch.get("translate", {}): dx, dy = sample(ch["translate"][name], t); lx += dx; ly += dy
        if name in ch.get("scale", {}): sx, sy = sample(ch["scale"][name], t)
        m = mat(lx, ly, r, sx, sy)
        world[name] = world[parent] @ m if parent else m
    return world

def render(anim, t, scale=0.5, size=(560, 480), origin=(300, 450)):
    W, H = size; ss = 2
    canvas = Image.new("RGBA", (W * ss, H * ss), (0, 0, 0, 0))
    att = attachment_offsets(); world = pose_at(anim, t)
    ch = ANIMS[anim] if anim else {}
    k = scale * ss
    to_canvas = np.array([[k, 0, origin[0] * ss], [0, -k, origin[1] * ss], [0, 0, 1]])
    for s, *_ in A:
        a = att[s]; img = IMGS[a["image"]]; w, h = img.size
        m = to_canvas @ world[a["bone"]] @ mat(a["x"], a["y"], 0, a["sx"], 1) @ np.array([[1, 0, -w / 2], [0, -1, h / 2], [0, 0, 1]])
        inv = np.linalg.inv(m)
        layer = img.transform(canvas.size, Image.AFFINE, tuple(inv[:2].flatten()), resample=Image.BICUBIC)
        alpha = sample(ch["alpha"][s], t) if s in ch.get("alpha", {}) else 1.0
        if s in ch.get("color", {}):
            c = sample(ch["color"][s], t); tint = [int(c[i:i + 2], 16) / 255 for i in (0, 2, 4)]
            arr = np.asarray(layer).astype(np.float32); arr[..., :3] *= tint; layer = Image.fromarray(arr.astype(np.uint8))
        if alpha < 1:
            arr = np.asarray(layer).copy(); arr[..., 3] = (arr[..., 3] * alpha).astype(np.uint8); layer = Image.fromarray(arr)
        canvas.alpha_composite(layer)
    return canvas.resize((W, H), Image.LANCZOS)

def on_bg(im, bg=(46, 52, 64)):
    out = Image.new("RGBA", im.size, bg + (255,)); out.alpha_composite(im); return out.convert("RGB")

def _flight_to_local():
    """The cast's flame flight is authored in world space; the flame bone's parent (wrist_L) rotates, so convert."""
    ks = ANIMS["cast"]["translate"]["flame"]; out = []
    for t, (x, y) in ks:
        m = pose_at("cast", t)["wrist_L"]
        inv = np.linalg.inv(m[:2, :2]); lx, ly = inv @ np.array([x, y])
        out.append((t, (float(lx), float(ly))))
    ANIMS["cast"]["translate"]["flame"] = out
_flight_to_local()

if __name__ == "__main__":
    sp = os.path.join(HERE, "spine"); os.makedirs(os.path.join(sp, "images"), exist_ok=True)
    for n in SIZES: shutil.copy(os.path.join(PARTS, n + ".png"), os.path.join(sp, "images", n + ".png"))
    json.dump(spine_json(), open(os.path.join(sp, "pyra.json"), "w"), indent=1)
    pv = os.path.join(HERE, "previews"); os.makedirs(pv, exist_ok=True)
    on_bg(render(None, 0)).save(os.path.join(pv, "setup.png"))
    on_bg(render("idle", 0)).save(os.path.join(pv, "pose.png"))
    fps = 30
    for an in ANIMS:
        d = duration(ANIMS[an]); n = int(round(d * fps))
        frames = [on_bg(render(an, i / fps)) for i in range(n)]
        if an == "hit": frames += [frames[0]] * 15
        frames[0].save(os.path.join(pv, an + ".gif"), save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0)
        print(an, n, "frames")
