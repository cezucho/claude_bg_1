"""Second parts sheet: stricter prompt (distinct left/right arms, torso without shoulder pads, straight limbs)."""
import sys, os, io, json, base64, urllib.request, concurrent.futures as cf
sys.path.insert(0, r"C:\Godot\claude_bg_1\tools\asset_gen")
import gen_sprites as g
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
REF = r"C:\Godot\claude_bg_1\production\art_review\style_test\pyra_A_heroic_chibi_raw.png"
PROMPT = """You are preparing production art for 2D cut-out skeletal animation (Spine). The attached image is the approved character design: Pyra, a fire sorceress drawn in a heroic chibi style. Redraw her as a MODULAR PARTS SHEET.

STYLE: identical to the reference: same face, same red flame-tipped hair, same red-and-black battle-mage coat with gold trim, same clean dark outlines of even weight, flat cel shading with one hard shadow tone, same colours. Every part at exactly the same scale as in the reference image, as if the character were taken apart. Light comes from the upper left for every part.

LAYOUT: a clean grid with every part fully separated, large empty gaps between parts (at least the width of a finger), nothing touching, nothing overlapping, nothing cut off by the image edge. Each part appears exactly once. No labels, no text, no numbers, no arrows, no guide lines, no ground shadows, no drop shadows.

PARTS (draw each as a complete, closed shape, including the bits normally hidden under neighbouring parts):
1. HEAD with face, front hair and the short neck, three-quarter view turned toward the viewer's right, as in the reference.
2. BACK HAIR: only the long hair mass that hangs behind the head and shoulders.
3. TORSO: chest and waist of the coat with the high collar, the neck opening and the belt with buckle at the bottom. NO shoulder pads, NO sleeves, NO arms. The shoulders end in smooth rounded shoulder caps.
4. COAT SKIRT: the coat tails from the belt down to the hem, belt included at the top, no legs.
5. CAPE: the short flared cape alone, seen from the front, as it hangs behind the body.
6. CHARACTER'S RIGHT UPPER ARM (the arm on the viewer's left): shoulder pad on top, red sleeve, ending in a rounded elbow cap. Drawn hanging straight down, vertical.
7. CHARACTER'S LEFT UPPER ARM (the arm on the viewer's right): its own drawing, not a mirror copy, shoulder pad on top, red sleeve, rounded elbow cap. Drawn hanging straight down, vertical.
8. CHARACTER'S RIGHT FOREARM: black bracer with gold trim, rounded elbow cap at the top, wrist at the bottom. Straight and vertical.
9. CHARACTER'S LEFT FOREARM: its own drawing, black bracer with gold trim, rounded elbow cap at the top, wrist at the bottom. Straight and vertical.
10. CHARACTER'S RIGHT HAND: open hand, palm up, fingers relaxed, as if a flame floats above it, with a rounded wrist end.
11. CHARACTER'S LEFT HAND: a closed fist curled around an EMPTY space as if gripping a pole, with NO staff in it, rounded wrist end.
12. RIGHT THIGH: black trousers from the hip to the knee, rounded hip cap on top and rounded knee cap at the bottom. Straight and vertical.
13. LEFT THIGH: its own drawing, same description.
14. RIGHT SHIN AND BOOT: the brown boot with gold buckle and cuff, rounded knee cap at the top, foot pointing toward the viewer's right.
15. LEFT SHIN AND BOOT: its own drawing, same description.
16. STAFF: the dark twisted wooden staff alone, perfectly vertical, with the glowing orange coal at the top.
17. HOVERING FLAME: a single small flame, upright.
18. FIREBALL PROJECTILE: a round fireball flying toward the viewer's right, with a short fiery tail streaming to the left.

All limbs drawn straight (not bent) so their joints line up with the parts they connect to.

BACKGROUND: one perfectly flat, uniform solid pure green colour (#00FF00) with no gradient, no shading, no ground, no shadows and no other objects. Do not use pure green anywhere on the character."""
key = g.api_key()
def call(i, size):
    body = {"contents": [{"role": "user", "parts": [
        {"inlineData": {"mimeType": "image/png", "data": base64.b64encode(open(REF, "rb").read()).decode()}}, {"text": PROMPT}]}],
        "generationConfig": {"responseModalities": ["TEXT", "IMAGE"], "imageConfig": {"aspectRatio": "1:1", **({"imageSize": size} if size else {})}}}
    req = urllib.request.Request(g.ENDPOINT.format(model="gemini-3.1-flash-image"), data=json.dumps(body).encode(), method="POST",
                                 headers={"Content-Type": "application/json", "x-goog-api-key": key})
    resp = json.loads(urllib.request.urlopen(req, timeout=300).read())
    for c in resp.get("candidates", []):
        for p in c.get("content", {}).get("parts", []):
            d = p.get("inlineData") or p.get("inline_data")
            if d: return base64.b64decode(d["data"])
    raise RuntimeError(json.dumps(resp)[:300])
def job(i):
    try: data = call(i, "2K")
    except Exception as e: print("2K failed, 1K:", str(e)[:200]); data = call(i, None)
    p = os.path.join(HERE, f"parts_v2_{i}.png"); im = Image.open(io.BytesIO(data)); im.save(p); return p, im.size
with cf.ThreadPoolExecutor(4) as ex:
    for f in cf.as_completed([ex.submit(job, i) for i in range(4)]):
        try: print("ok", f.result())
        except Exception as e: print("FAIL", str(e)[:300])
open(os.path.join(HERE, "parts_v2_prompt.txt"), "w", encoding="utf-8").write(PROMPT)
