"""Generates the auto-skirmish sprites with Google's Gemini image models (Nano Banana 2 / 2 Lite) (D-075).

Reads assets/data/art/skirmish_sprites.json. Raw replies go to production/art_review/skirmish/
with a manifest (prompt, model, time); cut-out, trimmed, downscaled PNGs go to
prototypes/auto-skirmish/art/<id>.png. The first character ("anchor") is the style reference: its
raw image is sent with every later character request so the set matches.

The API key is read from GEMINI_API_KEY, else tools/asset_gen/.env.local (GEMINI_API_KEY=...;
git-ignored). It is never printed or saved.

Kinds: character / structure (cut out of a flat key colour), texture (256 px tile), backdrop (16:9, 1600 px JPEG).

Usage (from the project folder):
    python tools/asset_gen/gen_sprites.py                  # every asset not yet made
    python tools/asset_gen/gen_sprites.py --only ember,rime --force
    python tools/asset_gen/gen_sprites.py --cut-only       # redo the cut-outs from the raw images
    python tools/asset_gen/gen_sprites.py --spec pattern_sprites.json --out game/art
Needs Pillow and numpy.
"""
import argparse
import base64
import datetime
import json
import os
import sys
import urllib.error
import urllib.request
from collections import deque

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPEC = os.path.join(ROOT, "assets", "data", "art", "skirmish_sprites.json")
RAW = os.path.join(ROOT, "production", "art_review", "skirmish")
OUT = os.path.join(ROOT, "prototypes", "auto-skirmish", "art")
ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"
SPRITE_H = 256      # cut-out sprites are scaled to this height; the board draws them at ~60 px
TEXTURE = 256


def api_key():
    key = os.environ.get("GEMINI_API_KEY", "").strip()
    if key:
        return key
    local = os.path.join(os.path.dirname(__file__), ".env.local")
    if os.path.exists(local):
        for line in open(local, encoding="utf-8"):
            if line.strip().startswith("GEMINI_API_KEY="):
                return line.split("=", 1)[1].strip()
    sys.exit("No key: set GEMINI_API_KEY, or put GEMINI_API_KEY=... in tools/asset_gen/.env.local")


def generate(prompt, model, key, ref=None, aspect="1:1"):
    parts = []
    if ref:
        parts.append({"inlineData": {"mimeType": "image/png", "data": base64.b64encode(open(ref, "rb").read()).decode()}})
    parts.append({"text": prompt})
    body = {"contents": [{"role": "user", "parts": parts}],
            "generationConfig": {"responseModalities": ["TEXT", "IMAGE"], "imageConfig": {"aspectRatio": aspect}}}
    req = urllib.request.Request(ENDPOINT.format(model=model), data=json.dumps(body).encode(), method="POST",
                                 headers={"Content-Type": "application/json", "x-goog-api-key": key})
    try:
        with urllib.request.urlopen(req, timeout=240) as r:
            resp = json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        raise RuntimeError("HTTP %d %s" % (e.code, e.read().decode("utf-8", "replace")[:600]))
    for c in resp.get("candidates", []):
        for p in c.get("content", {}).get("parts", []):
            d = p.get("inlineData") or p.get("inline_data")
            if d and str(d.get("mimeType", d.get("mime_type", ""))).startswith("image/"):
                return base64.b64decode(d["data"])
    raise RuntimeError("no image in the reply: " + json.dumps(resp)[:400])


def prompt_for(spec, a, with_ref):
    if a["kind"] in ("texture", "backdrop"):
        return a["prompt"]
    name, hexc = spec["backgrounds"][a["bg"]]
    style = spec["styleCharacter"] if a["kind"] == "character" else spec["styleStructure"]
    subject = (spec["referenceNote"] + " " if with_ref else "Subject: ") + a["subject"]
    return " ".join([style, subject, spec["background"].replace("{bgName}", name).replace("{bgHex}", hexc)])


def cut_out(raw_path, bg_hex, out_path):
    """Removes the flat key colour by flood fill from the image border, softens the edge, trims, scales."""
    im = np.asarray(Image.open(raw_path).convert("RGB")).astype(np.int32)
    h, w, _ = im.shape
    key = np.array([int(bg_hex[i:i + 2], 16) for i in (1, 3, 5)])
    border = np.concatenate([im[0], im[-1], im[:, 0], im[:, -1]])          # the model's "flat" colour drifts a little
    near = np.abs(border - key).sum(1) < 200
    if near.any():
        key = np.median(border[near], axis=0)
    d = np.sqrt(((im - key) ** 2).sum(2))
    bgish = d < 110
    seen = np.zeros((h, w), bool)
    q = deque((y, x) for y in range(h) for x in (0, w - 1) if bgish[y, x])
    q.extend((y, x) for x in range(w) for y in (0, h - 1) if bgish[y, x])
    for y, x in q:
        seen[y, x] = True
    while q:
        y, x = q.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and not seen[ny, nx] and bgish[ny, nx]:
                seen[ny, nx] = True
                q.append((ny, nx))
    alpha = np.where(seen, 0, 255).astype(np.uint8)
    # enclosed pockets of pure key colour (between an arm and the body) also go
    alpha[d < 60] = 0
    # edge pixels: partial alpha by distance, and pull the key colour out of them (despill)
    edge = (~seen) & (d < 170)
    alpha[edge] = np.clip((d[edge] - 60) / 110 * 255, 0, 255).astype(np.uint8)
    rgb = im.copy()
    if bg_hex.upper() == "#FF00FF":
        g = rgb[..., 1]
        rgb[..., 0] = np.minimum(rgb[..., 0], g + (255 - g) // 2)
        rgb[..., 2] = np.minimum(rgb[..., 2], g + (255 - g) // 2)
    else:
        lim = (rgb[..., 0] + rgb[..., 2]) // 2 + 30
        rgb[..., 1] = np.where(edge, np.minimum(rgb[..., 1], lim), rgb[..., 1])
    out = Image.fromarray(np.dstack([np.clip(rgb, 0, 255).astype(np.uint8), alpha]), "RGBA")
    box = out.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox()
    out = out.crop(box)
    scale = SPRITE_H / out.height
    out = out.resize((max(1, round(out.width * scale)), SPRITE_H), Image.LANCZOS)
    out.save(out_path, optimize=True)


def make_texture(raw_path, out_path):
    Image.open(raw_path).convert("RGB").resize((TEXTURE, TEXTURE), Image.LANCZOS).save(out_path, optimize=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", help="comma-separated asset ids")
    ap.add_argument("--force", action="store_true", help="regenerate even if a raw image exists")
    ap.add_argument("--cut-only", action="store_true")
    ap.add_argument("--spec", help="another brief file in assets/data/art/ (default skirmish_sprites.json)")
    ap.add_argument("--out", help="output folder relative to the project (default prototypes/auto-skirmish/art)")
    a = ap.parse_args()
    global OUT
    if a.out:
        OUT = os.path.join(ROOT, a.out)
    spec = json.load(open(os.path.join(os.path.dirname(SPEC), a.spec) if a.spec else SPEC, encoding="utf-8"))
    os.makedirs(RAW, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    man_path = os.path.join(RAW, "manifest.json")
    manifest = json.load(open(man_path, encoding="utf-8")) if os.path.exists(man_path) else []
    only = set(a.only.split(",")) if a.only else None
    key = None if a.cut_only else api_key()
    ref = os.path.join(RAW, "anchor.png")
    for asset in spec["assets"]:
        if only and asset["id"] not in only:
            continue
        raw = os.path.join(RAW, asset["id"] + ".png")
        if not a.cut_only and (a.force or not os.path.exists(raw)):
            use_ref = asset["kind"] == "character" and asset["id"] != "anchor" and os.path.exists(ref)
            prompt = prompt_for(spec, asset, use_ref)
            try:
                data = generate(prompt, asset["model"], key, ref if use_ref else None, "16:9" if asset["kind"] == "backdrop" else "1:1")
            except Exception as e:
                print("FAILED", asset["id"], str(e)[:400], flush=True)
                continue
            Image.open(__import__("io").BytesIO(data)).save(raw)
            manifest = [m for m in manifest if m["id"] != asset["id"]]
            manifest.append({"id": asset["id"], "file": asset["id"] + ".png", "prompt": prompt, "model": asset["model"],
                             "styleReference": "anchor.png" if use_ref else None,
                             "made": datetime.datetime.now().isoformat(timespec="seconds")})
            json.dump(manifest, open(man_path, "w", encoding="utf-8"), indent=1)
            print("generated", asset["id"], "with", asset["model"], flush=True)
        if not os.path.exists(raw):
            continue
        out = os.path.join(OUT, asset["id"] + (".jpg" if asset["kind"] in ("texture", "backdrop") else ".png"))
        if asset["kind"] == "texture":
            make_texture(raw, out)
        elif asset["kind"] == "backdrop":   # a wide painting behind the interface, kept large
            im = Image.open(raw).convert("RGB"); im.resize((1600, round(1600 * im.height / im.width)), Image.LANCZOS).save(out, quality=84, optimize=True)
        else:
            cut_out(raw, spec["backgrounds"][asset["bg"]][1], out)
        print("  ->", os.path.relpath(out, ROOT), flush=True)


if __name__ == "__main__":
    main()
