# Champion style test (2026-11-22)

Owner's direction: chibi but adult, anime-leaning, smooth vector look, no pixel art
(inspirations: Rogue Legend, Marvel United). Three directions were tried on Fang and Pyra:
A heroic chibi, B anime mobile, C toy figure (see `comparison.png`).

**Chosen: A, heroic chibi** (about 3 heads tall, sharp anime eyes, adult expressions, clean even
outlines, flat cel shading with one hard shadow tone). Parked by the owner; resume later.

- `*_A_heroic_chibi_raw.png`: Nano Banana output (gemini-3.1-flash-image), full resolution
- `*_A_heroic_chibi.png`: cut-out, about 1024 px tall (not downscaled to 256 px)
- `*_prompt.txt`: the exact prompt
- `style_test.py`: the script used; it writes to a `styletest/` folder next to itself

Next when resumed: refine A on these two, then use an approved image as the style reference for
the other champions, update `assets/data/art/*_sprites.json` briefs, write the art bible, and try
one champion as a cut-out rig (Skeleton2D or Spine) with idle and attack animations.
