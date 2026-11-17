# Visual Pipeline Plan: Chibi Champions on an Isometric Hex Board

*Status: proposal, 2026-11-17. Owner's request: a cartoony, two-dimensional, chibi look in the
spirit of Rogue Legend, an isometric view of the hex board, animated champions, status icons
and spell effects, with Nano Banana (Google's Gemini image models) as the image source.*

## 1. The look in words

Prompts should describe the style itself, never name another game. Naming a product invites
copies of its characters and creates rights risk. The style in words:

- **Figures:** chibi proportions, a head about a third of the body, big readable silhouettes,
  thick dark outlines, flat cel shading with one shadow tone and one highlight, saturated
  colours, a clear main colour per champion so it reads at thumbnail size.
- **View:** three-quarter front view, as if the camera looks down at about 30 degrees, which
  matches an isometric board.
- **Board:** hex tiles seen from above at an angle (each hex drawn about twice as wide as it is
  tall), soft painted textures, gentle ambient occlusion where tiles meet.
- **Effects:** chunky, bright, short-lived: puffs, sparks, rings, rather than realistic fire.

## 2. How to animate: four options

| Option | What it is | Quality | Effort | Fits this game |
|---|---|---|---|---|
| A. One sprite + motion "juice" | One static image per champion. Motion comes from code: idle bob, squash and stretch, a lunge toward the target, a white flash and shake when hit, a spin and fade on death, particles for spells | Good for chibi; many mobile auto-battlers do mostly this | Low | **Start here** |
| B. Cut-out rig | The champion generated as separate parts (head, body, arms, legs, weapon) on one sheet, then rigged with bones in the engine (Godot `Skeleton2D` + `AnimationPlayer`, or Spine). Idle, attack, cast, hit and death are keyframed | Very good, smooth, consistent | Medium per champion, mostly one-time rigging | **Upgrade path** |
| C. Frame-by-frame from the image model | Generating 6–8 drawn frames per action with Nano Banana, using the first frame as a reference | Inconsistent: faces, weapons and proportions drift between frames, so most frames need retries or hand fixing | High, and it scales badly (≈80 frames per champion) | Only for a few effects or a hero moment |
| D. Image to video, then frames | Animating the sprite with a video model and extracting frames | Drifts, backgrounds must be removed per frame, hard to loop | High | Not recommended |

**Recommendation:** ship option A for every champion first. Chibi figures with big heads carry
a lot of personality with simple motion, and it lets the game look alive within days. Then
move the champions people see most to option B. Nano Banana makes the art; the engine makes
the motion. Do not ask the image model for animation frames except for small effects.

## 3. What has to be made

Counts are for the current 10 champions; a 15-champion roster adds half again to the
champion rows. "Generations" includes retries: image models need several attempts per kept
image, more for things that must match a set.

| Asset | Kept images | Generations with retries | Notes |
|---|---|---|---|
| Style anchor (one champion on a hex, final look) | 3 | 30 | Decides everything else; spend time here |
| Champion full sprite, three-quarter view | 10 | 60 | Team colour comes from a tint shader, not separate art |
| Champion part sheets for the cut-out rig (option B) | 10 | 60 | Only when moving to option B |
| Champion portraits for cards and draft | 10 | 40 | Head and shoulders, same style |
| Hex tiles: ground, hill, stone, rubble, nexus base, board edge | 6 × 3 variants | 60 | Variants avoid a tiled look |
| Towers (intact, damaged, rubble) and nexus | 5 | 30 | Team colour by tint |
| Status and chance icons | about 26 | 40 | Generate as sheets of 9 so the set matches; see section 4 |
| Spell and hit effect textures (fireball, frost, poison cloud, lightning, slash, heal, shield bubble, sparks, smoke) | about 15 | 45 | Used as particle textures; the motion is the engine's |
| UI frames, buttons, bars, panels | about 10 | 30 | Can also stay flat vector UI |
| Props (trees, rocks, banners at the board edge) | about 10 | 30 | Optional, adds charm |
| **Total** | **about 115–125** | **about 425** | |

## 4. Status and chance icons

One consistent set: round badge, thick outline, one symbol, one colour family per kind.

- **Chances (now in the prototype):** poison, frost, crit, double strike, splash, cleave, chain
  lightning, fireball, knockback, lifesteal, mend, shield, dodge, thorns.
- **Statuses (if the status idea goes ahead):** stunned, frozen, airborne, blinded, silenced,
  rooted, taunted, berserk, burning, slowed, plus a "steadfast" immunity mark.
- **Others:** level, bounty, gold, initiative.

Generate them nine to a sheet on a flat background with the same prompt template, then slice.
Icons from the same sheet match far better than icons generated one at a time.

## 5. Cost

The Gemini API bills image models per image. At the published rates found on 2026-11-17
(check Google's pricing page before budgeting):

| Model | Price per image | About 425 generations |
|---|---|---|
| Nano Banana (Gemini 2.5 Flash Image) | $0.039, or $0.0195 in batch | about $17, or $8 in batch |
| Nano Banana Pro (Gemini 3 Pro Image), 1K–2K | $0.134, or about $0.067 in batch | about $57, or $28 in batch |
| Nano Banana Pro, 4K | $0.24 | about $102 |

**Plan on $50–150 including exploration.** Use Pro for the style anchor, champions and tiles,
where consistency and detail matter. Use the cheaper model or batch mode for icon sheets,
effect textures and variants. The real cost is time, not money: choosing images, removing
backgrounds, slicing sheets, setting pivots and tuning motion.

## 6. Pipeline, step by step

1. **Style anchor.** Generate a champion on two hex tiles in the target style until one image
   is right. That image goes into every later request as a reference, along with a short style
   paragraph. This is the single most important step for consistency.
2. **Prompt templates.** One template per asset type (champion, part sheet, tile, icon sheet,
   effect), with fixed wording for camera angle, outline, shading and background. Only the
   subject line changes.
3. **Flat background, then cut out.** Ask for a flat, uniform background colour not used in the
   art, then remove it with a background-removal tool. Image models rarely give clean
   transparency directly.
4. **Champions.** Write a one-line visual brief per champion from its kit (Anchor: armoured
   guardian with a big shield and anchor; Ember: fire-haired jungle brawler; and so on).
   Generate the full sprite with the anchor as reference, then the portrait from the sprite.
5. **Board.** Tiles at a 2:1 isometric hex footprint, drawn in the engine with depth sorting so
   champions in front overlap those behind.
6. **Motion (option A).** Per action, a short code animation: attack = wind-up squash,
   lunge, return; cast = rise, glow, release with an effect; hit = flash and shake; death =
   spin, shrink, puff. Initiative beats from the prototype map directly onto these.
7. **Effects.** Particle systems using the generated textures, colour-coded to match the icons.
8. **Cut-out rigs (option B, later).** For each champion, a parts sheet in the same pose, sliced,
   pivots set at joints, five keyframed animations: idle, attack, cast, hit, death.

## 7. What Claude can do, and what needs the owner

- **Claude can:** write the generation scripts against the Gemini API, keep the prompt
  templates, batch the requests, remove backgrounds and slice sheets, build the engine scenes,
  write the code animations and particle effects, and set up cut-out rigs from part sheets.
- **The owner decides:** the style anchor and every kept image. Taste is the bottleneck, and
  image review should happen in batches.
- **Needed to start:** a Gemini API key saved as a credential for this environment (never
  pasted into chat), and a decision on whether the visual game stays in the browser for now
  or moves into the Godot project.

## 8. Suggested first step

A one-day style test: the style anchor plus three champions with option A motion on a small
isometric hex board, in the browser prototype. It costs a few dollars of generations and
answers the big question, whether this look works for this game, before anything larger.

## Sources

- [Nano Banana API Pricing (2026): Every Plan & Per-Image Cost](https://www.myarchitectai.com/blog/nano-banana-api-pricing)
- [Nano Banana Pro pricing & free access: API rates](https://magichour.ai/blog/nano-banana-pro-pricing)
- [Gemini 3 Pro Image API Pricing & Cost Calculator (2026)](https://yingtu.ai/blog/gemini-3-pro-image-pricing-calculator)
