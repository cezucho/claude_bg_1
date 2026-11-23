# Pyra cut-out rig test (2026-11-23)

Following the "AI parts sheet -> layers -> Spine rig" recipe on the heroic chibi Pyra from `../style_test/`.

## What is here
- `parts_sheet_0..2.png` + `parts_sheet_prompt.txt`: Nano Banana (gemini-3.1-flash-image) exploded parts sheets,
  image-to-image from `pyra_A_heroic_chibi_raw.png` (`gen_parts.py`). Sheet 0 used (each part once, round joint caps).
- `parts/`: 15 transparent PNGs cut from sheet 0 (`extract_parts.py`; `split_parts.py` shows the raw components).
  Upper arm and forearm are reused for both sides (right side mirrored). Legs are split at the knee.
- `rig.py`: the rig in one place: 21 bones, 17 slots, three animations (idle 2.0 s loop, cast 1.2 s, hit 0.5 s).
  Writes `spine/pyra.json` (Spine 4.2 JSON), `spine/images/`, and `previews/*.gif` from its own renderer.
- `pack_atlas.py`: packs `spine/pyra.png` + `spine/pyra.atlas` for Spine runtimes.
- `spine/viewer/index.html`: the official Spine web player (4.2.74) playing the rig. Serve the `spine/` folder
  (`python -m http.server -d spine`) and open `/viewer/index.html`. `check_spine_runtime.py` does this headless.

## Verified
- The official spine-ts runtime parses the JSON and atlas and renders all three animations.
- Spine Trial 4.3.08 cannot save, export or use the CLI, so the community Spine MCP servers (which wrap the CLI)
  cannot drive it. Open it by hand: Spine Trial -> Spine menu -> Import Data -> `spine/pyra.json`
  (images path `spine/images/`). It can be inspected and edited there, not saved.

## Known weaknesses
- Rigid pieces only: no meshes or weights (hair, cape and skirt swing as stiff plates). Meshes and weights are a
  Spine Professional feature and need a human in the editor.
- The parts sheet drew both upper arms the same way; the mirror makes lighting on the right arm reversed.
- Torso and upper-arm images both have shoulder pads (doubled at the shoulder).
- Flame VFX is the same sprite scaled and faded.

## Version 2 (2026-11-23): stricter parts prompt, Pyra faces right, run cycle
- `gen_parts_v2.py` + `parts_v2_prompt.txt`: an 18-part brief at 2K (distinct left/right arms, torso without shoulder
  pads, straight limbs, empty fist, fireball). 4 sheets; `parts_v2_2.png` used. Despite "no labels" it wrote labels
  (filtered out by size) and drew full legs instead of thigh/shin (cut at the knee; `cuts_v2.json`).
- `extract_v2.py` -> `parts2/` (18 PNGs, half resolution). `rig2.py` -> `spine2/` (JSON, atlas, viewer) and `previews2/*.gif`.
- Pyra faces the viewer's right (her face and ear in the art say so). Near arm holds the flame, far arm the staff.
- Animations: idle 2.0 s loop, run 0.6 s loop (in place), cast 1.1 s (low back-swing, palm thrust, fireball flies
  right as its own bone in world space), hit 0.5 s (knocked back left, red flash). The hand flame does not inherit
  rotation, so it stays upright.
- Checked in the official spine-ts 4.2 runtime: all four load and play
  (`python check_spine_runtime.py spine2 idle,run,cast,hit`).
- Still rigid pieces (no meshes). Legs are slightly short of a full stride in the run; feet slide (in place cycle).
