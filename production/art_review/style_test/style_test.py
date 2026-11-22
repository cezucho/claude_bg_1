import sys, os, io, json, concurrent.futures as cf
sys.path.insert(0, r"C:\Godot\claude_bg_1\tools\asset_gen")
import gen_sprites as g
from PIL import Image
g.SPRITE_H = 1024
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "styletest")
COMMON = ("Full body, one character only, three-quarter front view from slightly above, standing in a neutral ready pose "
          "with arms held a little away from the body and legs slightly apart (so the figure can later be split into parts for "
          "skeletal animation), feet visible, centred, filling about 85% of the image height. No ground, no cast shadow, no text, "
          "no frame, no scenery, no pixel art, no painterly texture.")
STYLES = {
 "A_heroic_chibi": "Game character illustration in a heroic chibi style for a stylish tactics game aimed at adults: about 3 heads tall, "
   "head larger than realistic but a real torso, broad shoulders and a confident stance; sharp narrow anime eyes and a serious or "
   "cocky expression, never babyish or cute; clean smooth vector line art with crisp dark outlines of even weight, flat cel shading "
   "with one hard-edged shadow tone and one small highlight, saturated but controlled colours. Looks like a premium mobile game "
   "hero, like a collectible board-game miniature drawn as flat vector art.",
 "B_anime_mobile": "Anime mobile-game character art, slightly super-deformed: about 3.5 heads tall, slim athletic proportions, "
   "sharp angular anime face with intense eyes, dynamic hair; thin clean lineart, crisp flat cel shading with two hard shadow "
   "tones, rim light in the element colour, smooth vector-like finish. Cool and adult in tone, like a gacha game hero, not cute.",
 "C_toy_figure": "Stylised designer-toy figure drawn as flat vector illustration: chunky but tough proportions, about 2.5 heads "
   "tall, big hands and boots, a strong jaw or sharp features, narrow determined eyes, heavy confident silhouette; bold uniform "
   "dark outlines, flat colour blocks with one hard shadow tone, no gradients. Tough and cool like a board-game hero miniature, "
   "not childish.",
}
SUBJ = {
 "fang": ("magenta", "Fang, a wolf-folk ranger: a lean, muscular young man with a human face, sharp amber eyes, grey wolf ears and a "
   "shaggy grey mane, a faint scar on one cheek; a wolf-skull shoulder guard and a wolf-pelt cloak over dark leather armour with "
   "stone amulets; clawed gauntlets tipped with sharpened stone; a short wolf tail. Colours: brown, grey and moss green."),
 "pyra": ("green", "Pyra, a fire sorceress in her twenties: a confident smirk, sharp golden eyes, long red hair whose tips curl up "
   "like flames; a fitted red and black battle-mage coat with gold trim and a short flared cape, practical boots; a dark staff "
   "topped with a glowing coal in one hand, a flame hovering above the other open palm. Colours: red, black, orange and gold."),
}
BG = {"magenta": ("magenta", "#FF00FF"), "green": ("pure green", "#00FF00")}
key = g.api_key()
model = sys.argv[1] if len(sys.argv) > 1 else "gemini-3.1-flash-image"
def job(sk, ck):
    bgk, subj = SUBJ[ck]; name, hx = BG[bgk]
    p = " ".join([STYLES[sk], COMMON, "Subject: " + subj,
        f"The background is one perfectly flat, uniform solid {name} colour ({hx}) with no gradient, no shading and no other objects."])
    data = g.generate(p, model, key)
    base = os.path.join(OUT, f"{ck}_{sk}")
    Image.open(io.BytesIO(data)).save(base + "_raw.png")
    g.cut_out(base + "_raw.png", hx, base + ".png")
    return base, p
with cf.ThreadPoolExecutor(6) as ex:
    fs = {ex.submit(job, s, c): (s, c) for s in STYLES for c in SUBJ}
    for f in cf.as_completed(fs):
        try:
            b, p = f.result(); print("ok", b); open(b + "_prompt.txt", "w", encoding="utf-8").write(p)
        except Exception as e: print("FAIL", fs[f], str(e)[:300])
