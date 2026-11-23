"""Pyra cut-out rig, version 2 (parts from parts_v2_2.png). Pyra faces the viewer's right.

One description -> Spine 4.2 JSON (spine2/pyra.json + images + atlas) and preview GIFs from a small renderer.
Character space: origin between the feet on the ground, y up, degrees counter-clockwise (Spine's convention).
Every bone has rotation 0 in the setup pose (the parts as drawn: arms and legs hanging straight down);
POSE holds the ready stance and is added to every animation.
Usage: python rig2.py [anim ...]     (no args: everything)
"""
import os, sys, json, math, shutil
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PARTS = os.path.join(HERE, "parts2")
BOX = json.load(open(os.path.join(PARTS, "parts.json")))
SIZES = {n: Image.open(os.path.join(PARTS, n + ".png")).size for n in BOX}

# Legs keep their placement from the parts sheet (half-res sheet px -> world), one offset per leg.
LEG = {"R": dict(cx=114.25, bottom=954.5, x=-38), "L": dict(cx=266.5, bottom=952.5, x=42)}
def leg(side, sx, sy):
    o = LEG[side]; return (sx - o["cx"] + o["x"], o["bottom"] - sy)

# ---- bones: name -> (parent, world x, world y[, inherit]) in the setup pose -----------------------------------
J = {
 "root": (None, 0, 0),
 "hip": ("root", 0, 320),
 "thigh_R": ("hip", *leg("R", 114, 642)), "knee_R": ("thigh_R", *leg("R", 112, 776)),
 "thigh_L": ("hip", *leg("L", 266, 648)), "knee_L": ("thigh_L", *leg("L", 262, 782)),
 "skirt": ("hip", 0, 375),
 "torso": ("hip", 0, 345),
 "cape": ("torso", -5, 490),
 "head": ("torso", -6, 500),
 "hair": ("head", -10, 672),
 "shoulder_R": ("torso", -72, 452), "elbow_R": ("shoulder_R", -72, 389), "wrist_R": ("elbow_R", -71, 247),
 "flame": ("wrist_R", -21, 275, "onlyTranslation"),
 "shoulder_L": ("torso", 48, 452), "elbow_L": ("shoulder_L", 48, 389), "wrist_L": ("elbow_L", 49, 247),
 "staff": ("wrist_L", 122, 247),
 "fireball": ("root", 0, 0),
}

# ---- slots, back to front: (slot, bone, image, pin in image px (y down) or None for sheet legs, pin in world, visible)
A = [
 ("cape",        "cape",       "cape",        (96, 10),  (-5, 490),   True),
 ("hair_back",   "hair",       "hair_back",   (110, 8),  (-12, 672),  True),
 ("upper_arm_L", "shoulder_L", "upper_arm_L", (34, 22),  (48, 452),   True),
 ("forearm_L",   "elbow_L",    "forearm_L",   (36, 30),  (48, 389),   True),
 ("staff",       "staff",      "staff",       (33, 150), (122, 247),  True),
 ("hand_L",      "wrist_L",    "hand_grip",   (12, 30),  (49, 247),   True),
 ("thigh_L",     "thigh_L",    "thigh_L",     None, "L",              True),
 ("boot_L",      "knee_L",     "boot_L",      None, "L",              True),
 ("thigh_R",     "thigh_R",    "thigh_R",     None, "R",              True),
 ("boot_R",      "knee_R",     "boot_R",      None, "R",              True),
 ("skirt",       "skirt",      "skirt",       (110, 10), (0, 372),    True),
 ("torso",       "torso",      "torso",       (95, 175), (0, 345),    True),
 ("head",        "head",       "head",        (115, 205), (-6, 500),  True),
 ("upper_arm_R", "shoulder_R", "upper_arm_R", (34, 22),  (-72, 452),  True),
 ("forearm_R",   "elbow_R",    "forearm_R",   (36, 30),  (-72, 389),  True),
 ("hand_R",      "wrist_R",    "hand_open",   (15, 62),  (-71, 247),  True),
 ("flame",       "flame",      "flame",       (39, 128), (-21, 275),  True),
 ("fireball",    "fireball",   "fireball",    (95, 51),  (0, 0),      False),
]

def attachment_offsets():
    out = {}
    for slot, bone, img, pin, at, vis in A:
        w, h = SIZES[img]
        if pin is None:
            x0, y0, x1, y1 = BOX[img]; cx, cy = leg(at, (x0 + x1) / 2, (y0 + y1) / 2)
        else:
            cx = at[0] + (w / 2 - pin[0]); cy = at[1] - (h / 2 - pin[1])
        out[slot] = dict(bone=bone, image=img, x=round(cx - J[bone][1], 2), y=round(cy - J[bone][2], 2), w=w, h=h, vis=vis)
    return out

# ---- animations ---------------------------------------------------------------------------------------------
POSE = {
 "shoulder_R": 30, "elbow_R": 60, "wrist_R": -85,       # near arm: forearm level, palm up in front
 "shoulder_L": 10, "elbow_L": 25, "wrist_L": -35,       # far arm: fist at the hip, staff upright
 "thigh_R": 8, "knee_R": -6, "thigh_L": -6, "knee_L": 4,
}

def wave(period, amp, phase=0.0, steps=8, base=0.0):
    return [(round(period * i / steps, 4), base + amp * math.sin(2 * math.pi * (i / steps + phase))) for i in range(steps + 1)]
def wave2(period, ax, ay, phase=0.0, steps=8, bx=0.0, by=0.0):
    return [(round(period * i / steps, 4), (bx + ax * math.sin(2 * math.pi * (i / steps + phase)),
                                           by + ay * math.sin(2 * math.pi * (i / steps + phase)))) for i in range(steps + 1)]

IDLE = 2.0
RUN = 0.6
def run_leg(shift):
    """Thigh and knee keys for one leg; shift 0 or 0.5 of the cycle. Contact (forward) -> push -> toe-off -> passing."""
    th = [35, 0, -30, 12]; kn = [-8, -4, -35, -80]
    n = len(th); out_t, out_k = [], []
    for i in range(n + 1):
        j = (i + int(shift * n)) % n
        out_t.append((round(RUN * i / n, 4), th[j] - POSE.get("x", 0))); out_k.append((round(RUN * i / n, 4), kn[j]))
    return out_t, out_k
tR, kR = run_leg(0); tL, kL = run_leg(0.5)

ANIMS = {
 "idle": {
  "rotate": {
   "torso": wave(IDLE, 1.5), "head": wave(IDLE, -2.0, 0.1), "hair": wave(IDLE, -2.5, 0.25), "cape": wave(IDLE, -3.0, 0.3),
   "skirt": wave(IDLE, -1.0, 0.2),
   "shoulder_R": wave(IDLE, 3.0, 0.15), "elbow_R": wave(IDLE, 4.0, 0.2), "wrist_R": wave(IDLE, -6.0, 0.25),
   "shoulder_L": wave(IDLE, -2.0, 0.1),
  },
  "translate": {"torso": wave2(IDLE, 0, 3, 0.25, by=-3), "flame": wave2(IDLE, 0, 6, 0, 16)},
  "scale": {"flame": [(t, (1 + 0.05 * math.sin(8 * math.pi * t / IDLE), 1 + 0.09 * math.sin(8 * math.pi * t / IDLE + 1.5)))
                      for t in [round(IDLE * i / 32, 4) for i in range(33)]]},
 },
 "run": {   # in place; the game moves the node. Facing right.
  "rotate": {
   "thigh_R": [(t, v - POSE["thigh_R"]) for t, v in tR], "knee_R": [(t, v - POSE["knee_R"]) for t, v in kR],
   "thigh_L": [(t, v - POSE["thigh_L"]) for t, v in tL], "knee_L": [(t, v - POSE["knee_L"]) for t, v in kL],
   "torso": wave(RUN / 2, 1.5, 0, 4, base=-9), "head": wave(RUN / 2, -1.5, 0.1, 4, base=6),
   "hair": wave(RUN / 2, -4, 0.3, 4, base=-10), "cape": wave(RUN / 2, -5, 0.35, 4, base=-16),
   "skirt": wave(RUN / 2, 3, 0.25, 4, base=-4),
   "shoulder_R": wave(RUN, 14, 0.5, 8), "elbow_R": wave(RUN, 8, 0.5, 8),
   "shoulder_L": wave(RUN, 18, 0, 8),
  },
  "translate": {"hip": wave2(RUN / 2, 0, 9, 0.25, 4, by=-4), "flame": wave2(RUN / 2, -3, 4, 0.4, 4)},
 },
 "cast": {   # charge overhead, throw forward (to the right), fireball flies right. 1.1 s
  "rotate": {
   "shoulder_R": [(0, 0), (0.35, -90), (0.50, 70), (0.65, 62), (1.1, 0)],    # swing back low, then thrust forward
   "elbow_R":    [(0, 0), (0.35, -30), (0.50, -55), (0.65, -50), (1.1, 0)],
   "wrist_R":    [(0, 0), (0.35, 20), (0.50, 80), (0.65, 75), (1.1, 0)],     # palm turns to face forward: a push
   "torso":      [(0, 0), (0.35, 7), (0.50, -10), (0.65, -8), (1.1, 0)],
   "head":       [(0, 0), (0.35, -3), (0.50, 5), (0.65, 4), (1.1, 0)],
   "hair":       [(0, 0), (0.35, 4), (0.55, -10), (0.80, -4), (1.1, 0)],
   "cape":       [(0, 0), (0.35, 6), (0.55, -12), (0.85, -5), (1.1, 0)],
   "shoulder_L": [(0, 0), (0.35, -10), (0.50, 14), (1.1, 0)],
   "thigh_R":    [(0, 0), (0.50, 6), (1.1, 0)], "thigh_L": [(0, 0), (0.50, -4), (1.1, 0)],
  },
  "translate": {"torso": [(0, (0, 0)), (0.35, (-6, -2)), (0.50, (10, -4)), (0.65, (8, -4)), (1.1, (0, 0))]},
  "scale": {"flame": [(0, (1, 1)), (0.35, (1.5, 1.5)), (0.49, (1.6, 1.6)), (0.50, (0.01, 0.01)), (0.75, (0.01, 0.01)), (1.1, (1, 1))]},
  "attach": {"fireball": [(0, None), (0.50, "fireball"), (0.90, None)]},
  "fireball": (0.50, 0.90, 700),     # release time, gone time, distance flown to the right (filled in below)
 },
 "hit": {   # struck from the front (right), knocked back to the left. 0.5 s
  "rotate": {
   "torso": [(0, 0), (0.08, 12), (0.5, 0)], "head": [(0, 0), (0.08, 8), (0.5, 0)],
   "hair": [(0, 0), (0.10, -10), (0.5, 0)], "cape": [(0, 0), (0.10, -10), (0.5, 0)],
   "shoulder_R": [(0, 0), (0.08, -20), (0.5, 0)], "shoulder_L": [(0, 0), (0.08, -15), (0.5, 0)],
  },
  "translate": {"hip": [(0, (0, 0)), (0.08, (-14, -6)), (0.5, (0, 0))]},
  "color": {s: [(0, "ffffffff"), (0.04, "ff8a8aff"), (0.2, "ffffffff")] for s in [a[0] for a in A]},
 },
}

def ease(u): return u * u * (3 - 2 * u)
def sample(keys, t):
    if t <= keys[0][0]: return keys[0][1]
    for (t0, v0), (t1, v1) in zip(keys, keys[1:]):
        if t <= t1:
            u = 0 if t1 == t0 else ease((t - t0) / (t1 - t0))
            if isinstance(v0, tuple): return tuple(a + (b - a) * u for a, b in zip(v0, v1))
            if isinstance(v0, str): return v0 if u < 1 else v1
            return v0 + (v1 - v0) * u
    return keys[-1][1]
def step(keys, t):
    v = keys[0][1]
    for kt, kv in keys:
        if t >= kt: v = kv
    return v
def duration(anim):
    return max(k[-1][0] for name, ch in anim.items() if isinstance(ch, dict) for k in ch.values())

# ---- pose evaluation (shared by the renderer and the fireball set-up) ----------------------------------------
def mat(x, y, r, sx=1, sy=1):
    c, s = math.cos(math.radians(r)), math.sin(math.radians(r))
    return np.array([[c * sx, -s * sy, x], [s * sx, c * sy, y], [0, 0, 1]])
def pose_at(anim, t):
    ch = ANIMS[anim] if anim else {}
    world = {}
    for name, (parent, x, y, *rest) in J.items():
        lx, ly = (x - J[parent][1], y - J[parent][2]) if parent else (x, y)
        r = POSE.get(name, 0) if anim else 0; sx = sy = 1
        if name in ch.get("rotate", {}): r += sample(ch["rotate"][name], t)
        if name in ch.get("translate", {}): dx, dy = sample(ch["translate"][name], t); lx += dx; ly += dy
        if name in ch.get("scale", {}): sx, sy = sample(ch["scale"][name], t)
        if not parent: world[name] = mat(lx, ly, r, sx, sy)
        elif rest and rest[0] == "onlyTranslation":
            p = world[parent] @ np.array([lx, ly, 1]); world[name] = mat(p[0], p[1], r, sx, sy)
        else: world[name] = world[parent] @ mat(lx, ly, r, sx, sy)
    return world

def _fireball():
    """The fireball starts where the hand flame is at release and flies straight right in world space."""
    c = ANIMS["cast"]; t0, t1, dist = c.pop("fireball")
    p = pose_at("cast", t0 - 0.01)["flame"] @ np.array([0, 40, 1])
    x, y = float(p[0]), float(p[1])
    c["translate"]["fireball"] = [(0, (x, y)), (t0, (x, y)), (t1, (x + dist, y - 10))]
    c["scale"]["fireball"] = [(0, (0.7, 0.7)), (t0, (0.7, 0.7)), (t0 + 0.1, (1.0, 1.0)), (t1, (1.1, 1.0))]
_fireball()

# ---- Spine JSON ---------------------------------------------------------------------------------------------
def bezier(t0, v0, t1, v1):
    d = (t1 - t0) / 3; return [round(t0 + d, 4), v0, round(t1 - d, 4), v1]
def spine_json():
    att = attachment_offsets(); bones = []
    for name, (parent, x, y, *rest) in J.items():
        b = {"name": name}
        if parent: b.update(parent=parent, x=round(x - J[parent][1], 2), y=round(y - J[parent][2], 2))
        if rest: b["inherit"] = rest[0]
        bones.append(b)
    slots = []
    for s, *_ in A:
        d = {"name": s, "bone": att[s]["bone"]}
        if att[s]["vis"]: d["attachment"] = s
        slots.append(d)
    skin = {}
    for s, *_ in A:
        a = att[s]; d = {"x": a["x"], "y": a["y"], "width": a["w"], "height": a["h"]}
        if a["image"] != s: d["path"] = a["image"]
        skin[s] = {s: d}
    anims = {}
    for an, ch in ANIMS.items():
        out = {"bones": {}, "slots": {}}
        names = set(ch.get("rotate", {})) | set(ch.get("translate", {})) | set(ch.get("scale", {})) | set(POSE)
        for bn in names:
            tl = {}; base = POSE.get(bn, 0)
            if bn in ch.get("rotate", {}) or base:
                rk = ch.get("rotate", {}).get(bn, [(0, 0)]); keys = []
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
        for sn, ks in ch.get("color", {}).items():
            out["slots"].setdefault(sn, {})["rgba"] = [{"time": t, "color": c} for t, c in ks]
        for sn, ks in ch.get("attach", {}).items():
            out["slots"].setdefault(sn, {})["attachment"] = [{"time": t, "name": v} for t, v in ks]
        if not out["slots"]: del out["slots"]
        anims[an] = out
    return {"skeleton": {"spine": "4.2.00", "images": "./images/", "width": 640, "height": 760},
            "bones": bones, "slots": slots, "skins": [{"name": "default", "attachments": skin}], "animations": anims}

# ---- preview renderer ---------------------------------------------------------------------------------------
IMGS = {n: Image.open(os.path.join(PARTS, n + ".png")).convert("RGBA") for n in SIZES}
def render(anim, t, scale=0.5, size=(640, 440), origin=(250, 415)):
    W, H = size; ss = 2
    canvas = Image.new("RGBA", (W * ss, H * ss), (0, 0, 0, 0))
    att = attachment_offsets(); world = pose_at(anim, t); ch = ANIMS[anim] if anim else {}
    k = scale * ss; to_canvas = np.array([[k, 0, origin[0] * ss], [0, -k, origin[1] * ss], [0, 0, 1]])
    for s, *_ in A:
        a = att[s]
        visible = step(ch["attach"][s], t) is not None if s in ch.get("attach", {}) else a["vis"]
        if not visible: continue
        img = IMGS[a["image"]]; w, h = img.size
        m = to_canvas @ world[a["bone"]] @ mat(a["x"], a["y"], 0) @ np.array([[1, 0, -w / 2], [0, -1, h / 2], [0, 0, 1]])
        layer = img.transform(canvas.size, Image.AFFINE, tuple(np.linalg.inv(m)[:2].flatten()), resample=Image.BICUBIC)
        if s in ch.get("color", {}):
            c = sample(ch["color"][s], t); tint = [int(c[i:i + 2], 16) / 255 for i in (0, 2, 4)]
            arr = np.asarray(layer).astype(np.float32); arr[..., :3] *= tint; layer = Image.fromarray(arr.astype(np.uint8))
        canvas.alpha_composite(layer)
    return canvas.resize((W, H), Image.LANCZOS)
def on_bg(im, bg=(46, 52, 64)):
    out = Image.new("RGBA", im.size, bg + (255,)); out.alpha_composite(im); return out.convert("RGB")

if __name__ == "__main__":
    sp = os.path.join(HERE, "spine2"); os.makedirs(os.path.join(sp, "images"), exist_ok=True)
    for n in SIZES: shutil.copy(os.path.join(PARTS, n + ".png"), os.path.join(sp, "images", n + ".png"))
    json.dump(spine_json(), open(os.path.join(sp, "pyra.json"), "w"), indent=1)
    pv = os.path.join(HERE, "previews2"); os.makedirs(pv, exist_ok=True)
    on_bg(render(None, 0)).save(os.path.join(pv, "setup.png")); on_bg(render("idle", 0)).save(os.path.join(pv, "pose.png"))
    fps = 30
    for an in (sys.argv[1:] or ANIMS):
        d = duration(ANIMS[an]); n = int(round(d * fps))
        frames = [on_bg(render(an, i / fps)) for i in range(n)]
        if an == "run": frames = frames * 3
        if an in ("hit", "cast"): frames += [frames[0]] * 12
        frames[0].save(os.path.join(pv, an + ".gif"), save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0)
        strip = Image.new("RGB", (640 * 6, 440))
        for i in range(6): strip.paste(frames[int(i * (n - 1) / 5)], (640 * i, 0))
        strip.resize((640 * 3, 220)).save(os.path.join(pv, "_strip_" + an + ".png"))
        print(an, n, "frames")
