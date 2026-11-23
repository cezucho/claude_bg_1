"""Asks Nano Banana for an exploded parts sheet of Pyra (image-to-image from the style-test hero shot)."""
import sys, os, io, concurrent.futures as cf
sys.path.insert(0, r"C:\Godot\claude_bg_1\tools\asset_gen")
import gen_sprites as g
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
REF = r"C:\Godot\claude_bg_1\production\art_review\style_test\pyra_A_heroic_chibi_raw.png"
PROMPT = (
 "Using the attached character as the exact design reference, create an exploded-view modular 2D game sprite parts sheet "
 "for cut-out skeletal animation (Spine). Keep the identical art style, line weight, colours, cel shading and proportions; "
 "every part at the same scale as in the reference. Draw each body part fully separated, floating apart from every other part "
 "with generous empty space between them, nothing touching or overlapping, arranged in a tidy grid. Parts to draw, each exactly once: "
 "1 the head with face and front hair, no neck; 2 the long back hair mass alone, as it hangs behind the head; "
 "3 the torso: upper coat with collar and shoulders, cut off at the waist belt, no arms, no head; "
 "4 the hips and coat skirt from the belt down to the coat hem, no legs; 5 the short flared cape alone, unfolded; "
 "6 left upper arm, 7 right upper arm, 8 left forearm with sleeve cuff, 9 right forearm with sleeve cuff; "
 "10 left hand open palm up, 11 right hand clenched in a fist as if gripping a pole; "
 "12 left thigh, 13 right thigh, 14 left shin with boot and foot, 15 right shin with boot and foot; "
 "16 the staff alone, upright, with the glowing coal on top; 17 the hovering flame alone. "
 "Joint ends must be rounded with extra overlapping circular caps at the shoulders, elbows, wrists, hips and knees, "
 "drawn as complete shapes (the part continues under where it would join), so no gaps appear when the bones rotate. "
 "Flat even lighting, no cast shadows, no ground shadow, no labels, no text, no numbers. "
 "The background is one perfectly flat, uniform solid pure green colour (#00FF00) with no gradient and no other objects.")
key = g.api_key()
def job(i):
    data = g.generate(PROMPT, "gemini-3.1-flash-image", key, ref=REF, aspect="1:1")
    p = os.path.join(HERE, f"parts_sheet_{i}.png"); Image.open(io.BytesIO(data)).save(p); return p
with cf.ThreadPoolExecutor(3) as ex:
    for f in cf.as_completed([ex.submit(job, i) for i in range(3)]):
        try: print("ok", f.result())
        except Exception as e: print("FAIL", str(e)[:300])
open(os.path.join(HERE, "parts_sheet_prompt.txt"), "w", encoding="utf-8").write(PROMPT)
