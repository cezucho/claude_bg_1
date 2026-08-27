# Active Session State

*Last updated: 2026-08-16*

## Current Task

**STEP 1 of 3 DONE — ADR-0005 AMENDED 2026-08-17 (team-relative tier-4 patterns).**
Tier-4 patterns were specified as applied *verbatim* (world space). Because the board's
symmetry is a 180-degree rotation, the two teams face opposite ways, so a verbatim wedge
aimed at the enemy for team A aimed at team B's own nexus. Now: tier-4 offsets are
authored in a **canonical frame with forward = +R** and reoriented per team.
- Added `Hex.HalfTurn` and `Hex.ForForward` to `src/Augury.Sim/HexCoord.cs`.
- **The fix needed no new geometry**: a half-turn is exactly `Rotate(offset, 3)` — a
  rotation the six-facing system already expresses — and it is the same map that defines
  board symmetry, so the far team's pattern covers exactly the antipodal hexes.
- **Why rotational symmetry was load-bearing all along:** a chiral pattern's *mirror* is
  reachable by NONE of the six facings. On a mirror-symmetric board the two teams would
  hold different-shaped versions of the same ability and no orientation rule could fix it
  — every chiral tier-4 would need authoring twice. Asserted in
  `PatternOrientationTests.MirroredPattern_IsReachableByNoRotation`.
- 8 new tests, 40 passing. ADR validation criteria now checked off rather than open.
- Cross-referenced from Map & Terrain rule 4.

*Remaining: step 2 = re-run applicability against real tower positions; step 3 = unpark
the champion/ability schema.*

---

**MOVEMENT & TARGETING GDD WRITTEN 2026-08-17** — `design/gdd/movement-and-targeting.md`
(system #6, Drafted). **Two economies, and movement is off the ladder entirely.**

- **Basic economy:** 2 basic actions per team per half, by **two DIFFERENT champions**,
  **compulsory**. A basic = move one champion, or one basic attack.
- **Ability economy:** unchanged — 1 ability per champion per half, on the ladder.
- **Independent.** A champion that took a basic MAY also use an ability that half.
- **Basics resolve BEFORE the ladder. Positions are then LOCKED for the half.**
- **Champions block movement**, friendly included. Path length, not distance, is what SPD
  limits. **No line of sight** for targeting.
- **NEW: every champion has a passive ability** (4 actives + 1 passive). Passives exist so
  basic attacks — unanswerable on the ladder, since they resolve before it — have an
  automatic answer. Passives carry no sigil, so the 20-ability chain figure is unaffected.

*Measured (`dotnet run --project tools/Augury.Tools mobility`) — and I had to correct
myself here.* I first reported "one champion crosses in 1.0 round, the economy inverts,
fast or broad". That assumed one champion could take both basics. With the
different-champions rule the true picture is:
- Individual crossing speed is **unchanged** (2.0 rounds at SPD 2).
- Team-wide repositioning is **2.5x slower** (1.2 rounds vs 0.5).
- Crossing costs **50% of the team's basic economy** for its whole duration, and that
  share is **invariant in SPD** — speed buys time, never budget.
- A champion averages **0.8 basics/round** against 2.0 before.
- **Breadth is the scarce resource, not speed.**

*Closed three open questions across other GDDs:*
- Ladder Q3 (is movement an initiative-1 action?) → **no**, off the ladder.
- Map Q6 (impassable terrain?) → **no — the terrain is the other players.** A 5-champion
  wall adds 3 hexes of detour and leaves no hex unreachable, so bodies tax movement
  without recreating corridors.
- Sigils denial B is **partly superseded**: the universal cheap dodge no longer exists,
  because nobody can move during an exchange. Denial B survives only via deliberate
  low-initiative abilities; denial A untouched.

*Amendments owed to `champion-and-ability-schema.md` (noted in that file):*
1. **RCH must cap at 3, not 4** — reach 4 covers the whole board from the centre (100%),
   59% averaged. A radius-4 board cannot express 4 as a limit.
2. **Fifth ability slot for the passive**, plus trigger vocabulary and deterministic
   ordering for simultaneous triggers.

*Open:* one-champion-alive case (2 distinct champions unsatisfiable) · can basic attacks
damage structures (stalling risk) · should friendlies block (most likely to feel bad) ·
basic alternation order · shortest-path tie-break for determinism.

*Risk to watch — criterion 12:* the two-economy split makes **the ladder opt-in**. A team
ahead on points can basic-attack and decline abilities. Partly self-solving, since molding
comes from ability use so passivity forfeits stat growth. Harness bound: ladder opened in
≥70% of halves.

---

## Earlier Task

**Map & Terrain GDD written** — `design/gdd/map-and-terrain.md`. Board fixed:
radius-4 hexagon, 61 playable hexes, teams facing across opposite **edges**, 12
off-board spawn hexes, 180-degree rotational symmetry, one champion per hex, **no
lanes**, 5 towers, 2 jungle flanks, front-to-front 8 hexes.

**Spawn rows (user's idea, adopted).** Six off-board hexes per team behind each front
line. Every champion owns a designated hex; the jungler owns two and picks a flank.
Untargetable — outside the game entirely, so spawn camping is impossible by
construction. Entering play costs the champion's action (tunable to free).

*Why they exist:* ADR-0006 batches every death to round close, so multi-death rounds
are the normal shape of a good exchange, not a rare ace. A single shared respawn hex
would have broken in ordinary play. Dedicated per-champion hexes make simultaneous
respawn collision-free with no tie-break and no fallback placement path.

**Board flipped corner-facing to edge-facing** to fit them: a corner has only 3
off-board neighbours (cannot seat 5), an edge has 8. A radius-4 edge is exactly 5
hexes — one per champion — and radius 4 is therefore the *smallest* board that seats
a team abreast. A corner base would also have funnelled 5 respawning champions through
2-3 hexes, reintroducing the single-file problem that removing lanes solved.

Coordinates now read as **rank** (`R`, toward the enemy) and **file** (`Q - S`, across).
Both negate under the symmetry map, so every zone rule is written on absolute value.

The finding that decided it: on a hexagon the outer edge is a single ring, so any
edge-hugging lane is **1 hex wide at every radius** (verified at radius 4 and 6 with
`tools/Augury.Tools board`). Lanes therefore *create* the bot/support single-file
problem rather than solving it — and lanes were never asked for. Removing them makes
the problem vanish and lets one-champion-per-hex stand, which the rigidity tiers need.

**Constraint discovered — owed to ADR-0005 as an amendment.** Symmetry must be
180-degree rotational, never mirror. A mirrored tier-4 pattern is *chirally* different
and no rotation produces it, so under a mirrored board the two teams would have
differently-shaped versions of the same ability. Consequently tier-4 patterns must be
fixed relative to **the owning team's forward direction**, not world-absolute as
ADR-0005 currently says. Must land before any tier-4 ability is authored.

**User decisions this session:** 61 hexes (over my 91 recommendation) · towers both
damage and score · 5 towers, 2 per team plus a neutral centre · jungle is a **road for
the jungler**, not an obstacle, extra jungle effects explicitly deferred · edge-facing
board · spawn hexes fully untargetable · entering play costs the champion's action.

**Timing principle agreed for "decide now vs defer to vertical slice":** decide now
what changes the *shape of the data*; defer what changes only a *number*. A structural
choice deferred gets built twice; a numeric choice decided early was going to be tuned
anyway. Third category worth naming: questions play cannot answer at all (e.g. "are
spawn hexes targetable?" is rules-consistency, not feel) — those must be decided now.

**COMBO PROBLEM — sigils & beacons. GDD WRITTEN 2026-08-17: `design/gdd/sigils-and-beacons.md` (system #30, Drafted, pending review). Bidirectional deps wired into systems-index, initiative-ladder and map-and-terrain. Notes below are the working record.**

*Problem:* a player sets up a combo over rounds, and the opponent denies it procedurally
for free. Two distinct denial mechanisms, found by re-reading the ladder rules:
1. **Pass cuts turn count.** Play alternates, so you only get the actions the opponent
   grants by answering. A 3-piece combo needs two answers; they just pass. (2-piece
   combos already survive — the Last Word covers them.)
2. **Ceiling crash — the sharper one.** Answers must be ≤ last initiative played. Open
   Vortex at 3, they answer with a **1**, and your init-3 Meteor is now *illegal for the
   rest of the half*. Symmetric (they capped themselves too) but asymmetrically good for
   whoever has less to do — always the defender.

*Principle adopted:* counterplay to a combo must be **spatial and anticipatory**, not
**procedural and reactive**. Passing costs no position, no read, no commitment — that is
why it feels cheap.

*Mechanic converged on (USER'S refinement of my proposal, and better):*
- Abilities carry **0 or 1 printed sigil** and **0 or 1 typed slot**. Most have neither.
- Sigils are **fixed per ability, never generated** — also forced by Pillar 1 (no randomness).
- A **beacon** is a visible board object that **fills matching slots** in its area. It does
  NOT grant sigils on top of existing ones — my version did, and the harness shows that
  floods the board. The ability holds the *potential*; the beacon is the key.
- **Chain** = two same-active-sigil abilities on two different champions, resolving in ONE
  ladder step. Uninterruptible, so both denials die at once.
- **Chains may ASCEND** — the ladder never otherwise allows it, so today a combo's payoff
  must be weaker than its setup, which is backwards. Ceiling afterwards = highest
  initiative in the chain, so a big finisher still invites a full-power answer.
- Brake is action economy: a 2-chain spends 2 of 5 champions in one beat.

*Measured — `dotnet run --project tools/Augury.Tools sigils`:*
- Unit is the **champion duo** (5 champions = 10 duos), not the ability pair; a half has
  room for ~2 chains, so duo coverage binds.
- **WILD slots (take any beacon's sigil) are too generous** — one beacon reaches 4.7-9.7
  of 10 duos in every config. Confirms the user's instinct exactly.
- **TYPED slots behave.** Recommended: **3 sigils, 15% printed, 25% slots** → 1.01 duos
  natural, **4.08 with one beacon**, 1.1% dead drafts. Beacon lift 4x, so beacons are
  load-bearing rather than decorative.
- In kit terms that is **3 printed / 5 slotted / 12 plain** out of 20 abilities — which
  also answers the legibility worry, since most abilities carry no iconography at all.
- Caveat: bounds *draft-level potential* only. Live usage is lower (position, cooldown,
  availability, both champions unacted); random draft makes it a floor for skilled drafters.

*Rejected, with reasons:* combo meter (ambient/procedural — fights the spatial principle,
and stacks a second economy of restraint on the ladder). Rage-meter-on-death (rewards
being killed; we already have an *earned* comeback in the nexus terminator, and two
comeback systems make trading badly correct). Declared chains (user disliked; heaviest).

*Beacon decisions taken 2026-08-17:* **placement is mostly OPENING PHASE, with mid-match
placement possible via an ability and deliberately harder.** **Radius 1** (beacon hex +
6 neighbours).

*Measured — `dotnet run --project tools/Augury.Tools beacon`:*
- **The edge penalty makes the user's placement rule self-enforcing.** A radius-1 zone is
  7 hexes in the interior but clips against the board edge: 5 on a nexus hex, **4 on a
  lane mouth**. So a beacon planted safely at home controls 6.6% of the board versus
  11.5% in the middle — a **43% coverage penalty for playing safe**. Walking a beacon
  forward is rewarded by geometry alone; no extra rule needed to make late placement
  "harder but better".
- **On-tower vs beside-tower is a real placement decision, not a formality.** A tower has
  6 playable approaches. Beacon ON it → all 6 inside the zone, contesting is unavoidable.
  Beacon BESIDE it → the tower hex is still in the zone but **3 of 6 approaches stay
  free**. So beside-placement taxes *occupying* the tower while leaving *contesting* it
  possible; on-placement taxes both. Same for all five towers.
- Max coverage 11.5% of board, so refusing a zone is never ruinous — beacons shape ground
  without dominating it.

*Decisions 2026-08-17, second pass:*
- **THE CHAIN RULE MUST NOT MENTION BEACONS.** The rule is only: *two abilities with the
  same **active** sigil, on two different champions, resolve in one ladder step.* Printed
  sigils are always active; a slot is active only inside a matching beacon's zone. Beacons
  are a *source* of active sigils, not a term in the rule. This resolves the "must both
  champions be in range?" question automatically — each ability independently needs its
  own sigil live, so printed+slot needs one champion in the zone and slot+slot needs both.
  Not a choice; it falls out.
- **Beacon count is not a fixed number** — only certain abilities grant beacons, and they
  are rare because a beacon is powerful. An opening-phase beacon ability pays for itself
  with a cooldown carried into the action phase.
- **Action-phase beacon abilities are rarer still than opening-phase ones.** Rationale is
  an information argument: an opening-phase beacon is placed *in the dark*, before anyone
  knows where the fight goes; an action-phase beacon is placed knowing exactly where every
  champion stands. The same object is worth far more with information, so the price scales
  with information rather than with the object.
- **Beacons are DESTROYABLE, never expiring.** Intent is a race: two champions converging
  to use a beacon while two converge to break it.

*Measured — the huddle tax (`beacon` command):* a slot+slot chain forces both champions
inside one 7-hex zone, which is close to a fixed tier-4's 5-hex footprint.
- Compact tier-4 (blob/wedge): **81% of in-zone pairs catchable vs 16% of board pairs** —
  roughly **5x more exposed**. That is the "something for something", quantified.
- **But 4 of 21 in-zone pairs are still safe**, so positioning inside the zone is its own
  game, not a death sentence.
- **Pattern shape is the lever**: a *line* tier-4 catches only 24% of in-zone pairs. If
  beacons should be riskier, print more compact tier-4 shapes. Ability schema owns this.

*Flagged for Objectives/Ability schema — the trap to avoid:* **beacon destruction must not
be cheap or low-initiative.** A 1-action, initiative-1 "break beacon" would recreate
exactly the cheap-procedural-denial-of-expensive-setup problem that beacons exist to
escape — the whole design folding back on itself. Destruction should cost roughly what
the chain it denies costs (~2 champion-actions), which also matches the intended race.

*Still open:* whether a beacon may sit on a tower hex (user undecided) · chain length cap
(start at 2) · exact beacon durability in actions.

**Objectives brainstorm — decisions taken 2026-08-16:**
- **THE MATCH IS A POINTS RACE.** Everything scores: kills, tower control, possibly
  tower destruction, and the nexus. *(Corrected 2026-08-17 — I had wrongly written
  "destroying the nexus ends the match" as decided; the user was only asking how
  destruction could work. Do not reintroduce this.)*
- **Towers are captured (reversible, flip, tick score); only the NEXUS is destroyed.**
  Reversible towers are the main brake on snowball. The nexus is the largest single
  point source, not a separate way to win — a team can win without ever touching it.
- **NEXUS = PURE TERMINATOR, awards no points, ends the match at any score.**
  Two endings only: target score (the large majority) or nexus destroyed (rare).
  No round limit needed — towers tick, so the target is always approached.
- **The comeback is a DESIGN GOAL, not a leak.** *(Corrected 2026-08-17 — I had
  written the invariant "no team may destroy the nexus while behind on points". That
  is the exact opposite of the intent. The user wants a behind team that lands an ace
  and capitalises to win. Do not reintroduce that invariant.)* Criteria 24-26 now test
  the inverse: nexus endings must be non-zero, some must come from behind, and the
  nexus must be destructible in ~one ace window.
- **The calibration that makes it work:** nexus durability ≈ *most but not all* of one
  ace window for a full unopposed team. Too durable → comeback never happens, nexus is
  decoration. Too fragile → every ace ends the match, points race is irrelevant.
- Meshes with defender scaling for free: after an ace nothing is adjacent, so the nexus
  takes full-rate damage. The window when it is attackable and the window when the
  enemy is dead are the same window — neither rule was written to produce that.
- **Nexus = the middle three hexes of each front line.** Reaching it means standing on
  the enemy's own front rank. This closed board Open Question 7 — the front line now
  matters in every round instead of only the first.
- **Defenders scale structure damage down, never to zero** (user chose this over my
  "immune while defended"). Their version is better: full immunity lets one parked
  champion veto a siege forever and the match stalls. Scaling guarantees progress, so
  defence buys rounds rather than denying outright — and after an ace, nothing is
  adjacent, so structures fall at full rate. That is the ace-capitalisation answer.
- **Minion waves deferred to Vertical Slice.** MVP must not depend on them; they plug
  in later as the tempo layer (windows when a tower is takeable). Nothing else
  currently supplies rhythm — recorded as Map open question 8.

**Geometric finding — the board already had lanes.** Exactly **two** straight
front-to-front lines exist on a radius-4 hexagon: `(0,-4)` going `(0,+1)`, and
`(4,-4)` going `(-1,+1)`. They cross at the centre tower and **carry all five towers**
— unplanned; the towers were placed for symmetry and landed on the two natural axes.
A square map affords three lanes, a hexagon exactly two, which suits five champions
better anyway: **two per lane plus a jungler**, so every lane is a pair. Lanes are
minion routes only — champions are never obliged to use them, so they carry none of
the single-file cost that movement corridors do.

**Next:** re-run `applicability` with the real tower positions as contested points
(acceptance criterion 11, blocking before abilities), then Movement & Targeting, then
unpark the champion/ability schema.

---

**Earlier: champion/ability schema GDD PARKED; process changed.**

User pushback, 2026-08-16, and it was correct on every point:

1. **Wrong order.** The stat model was designed before the board. Board size sets
   champion density → density sets pattern applicability → applicability prices ability
   power. Every number in the schema silently encoded a radius-4 board nobody chose.
   Verified by sweeping radii: a tier-4 5-hex pattern is 41.8% applicable at radius 3
   and 23.9% at radius 6, a spread wider than the ±0.08 conformance band built on it.
   Only the qualitative findings survive (melee never most applicable; tier-3 reach
   matters and area does not; tier-4 hex count monotonic).
2. **Too many stats, some premature.** Six was not derived. `SPD` presupposes Movement
   & Targeting, `RES` presupposes Status Effects — neither designed. Both are deletion
   candidates, not tuning candidates.
3. **Kit shapes too narrow.** *Decision: initiative total is a currency traded against
   stats* — `[1,2,2,4]` (sum 9) buys a stat bonus, sum 11 pays for it. Replaces the
   "exactly three shapes" rule, which is now obsolete.
4. **Prototypes ran on unstated assumptions** (a damage model nobody had agreed).

**Process change in force — see "Collaboration Process" below.**

**Board decisions already given:** jungle · towers/defended points · symmetric and
chess-like. Lane corridors *not* selected.

Preceded by a real measurement rather than an assumption. `tools/Augury.Tools` (the
Balance Simulation Harness ADR-0001 says exists from day one) sampled 100,000
actor-positions per placement model to close ladder Open Question 2:

- **Melee is not the baseline.** A range-1 ability reaches an enemy in 50% of contested
  board states, not the 100% ladder F4 assumed. Tier-1 abilities must be ranged.
- **Rotatable pattern size is irrelevant; reach is the dial.** 2-hex line and 3-hex
  wedge measure identically (68.7%) — six facings make area nearly meaningless.
- **Fixed pattern size matters linearly**, ≈6pp per hex → 5 hexes is the tier-4 target.

`M` revised `[1.0, 1.3, 2.2, 4.4]` → `[1.0, 1.3, 2.0, 4.0]`; effective value now flat
within ±2%. The curve barely moved — the shape was right, the top tiers ~10% hot.

**Key schema decisions:** six permille stats (VIT/POW/ARM/RCH/SPD/RES), split into
continuous (drift invisibly) and threshold (floored, snap visibly); the **cross rule**
(an ability never molds the stat it scales from, so a kit is a rotation not a button);
and an **initiative budget** of 10 with ≤2 per tier, which admits exactly three kit
shapes — Ladder `1-2-3-4`, Anvil `1-1-4-4`, Vice `2-2-3-3`. The Vice cannot answer at
initiative 1 at all, which is a deliberate exploitable weakness.

**Owed to ADR-0007:** `AbilityDef` needs `MoldUp`/`MoldDown` pairs plus `ScalesFrom` to
enforce the cross rule. Additive change, no implementation exists yet, but it must land
before content authoring.

---

**Earlier: test infrastructure scaffolded AND VERIFIED — 32 tests passing.**

I installed the .NET 8 SDK in this container (extracted from
packages.microsoft.com's jammy repo; `dot.net` and the Azure CDN are blocked by
the egress proxy, `packages.microsoft.com` is not). So this scaffold is
**compiled and run**, not written blind.

```
Augury.sln
src/Augury.Sim/          Arith.cs, HexCoord.cs — specified verbatim by ADR-0002/0005
tests/unit/Augury.Sim.Tests/
    Architecture/ManifestRuleTests.cs   the manifest's 🤖 rules, executable
    Foundation/ArithTests.cs            ADR-0002 validation criteria
    Foundation/HexCoordTests.cs         ADR-0005 validation criteria
tests/integration/       gdUnit4, empty until Augury.Game exists
tests/smoke/critical-paths.md
tests/evidence/
.github/workflows/tests.yml   sim-tests · manifest-guard · integration (if: false)
```

**Three real defects the verification caught** — all would have shipped as
"looks right" documentation:
1. `ReadOnlySpan<HexCoord>` cannot be backed by a collection expression (CS9203)
2. Missing XML doc comments are build **errors** here — `TreatWarningsAsErrors`
   plus `GenerateDocumentationFile` enforces `coding-standards.md` mechanically
3. The prototype-isolation CI guard false-positived on a doc comment *citing*
   `prototypes/initiative-ladder/REPORT.md`; narrowed to real references

**Two repo-level fixes:**
- `.gitignore` excluded `*.csproj` and `*.sln` (Unity-oriented, since Unity
  regenerates them). Ours are hand-authored source — `Augury.Sim.csproj` is
  where ADR-0001's boundary is declared. Un-ignored, with a note.
- `coding-standards.md`'s Godot CI command assumed GDScript. Now names
  `dotnet test Augury.sln` for this project. (The long-standing follow-up.)

**Not verified:** anything needing Godot. No engine in this container, so the
gdUnit4 job is `if: false` and the integration directory is empty by design.

**Next:** `/design-system` #2 (Champion Data & Stat Model + Ability Definition
Schema) · `/architecture-decision` ADR-0008 (AI search) · `/design-review` on
the ladder GDD in a **fresh session**.

## Collaboration Process

`.claude/rules/design-docs.md` already required section-by-section authoring with
approval between sections. It was not followed for the ladder GDD or the schema GDD —
both were written in one pass. The cause was over-correcting on earlier feedback about
asking the same question twice: the fix for *asking twice* was taken as *not asking*.

**The four rules that apply from here:**

1. **Decisions listed before drafting.** Before writing any GDD section, state the
   decisions it will make, split into **yours** (vision, feel, identity) and **mine**
   (derivable from decisions already made, or purely technical). Only the vision list
   gets asked, once, in one batch.
2. **Assumptions are marked in the document, never buried.** Any value not derived from
   a stated decision carries a visible marker so it can be scanned for and challenged.
3. **Prototypes state their model before they run.** The ladder prototype used a damage
   model nobody had agreed to. The model gets confirmed *before* CPU is spent, so the
   numbers describe the intended game.
4. **Dependency order is enforced, not assumed.** If a GDD needs a quantity another
   undesigned GDD owns, that is a blocker — write the dependency first or mark the
   number provisional. This is what parking the schema GDD is for.

Challenging a written document is always cheap. Markdown costs nothing to revise;
what gets expensive is code and content built on it, and none exists yet.

## Project

**AUGURY (working title)** — turn-based tactics / MOBA hybrid. One player commands
five drafted champions on a hex map; abilities carry an **initiative** value and
both sides trade down a descending response ladder.

- Phase: **Concept → Systems Design**
- Engine: Godot 4.6, C# (.NET 8+), PC
- Review mode: `lean` (director gates fire only at `/gate-check`)

## Progress

- [x] `/start` — onboarding, review mode set to lean
- [x] `/brainstorm` — `design/gdd/game-concept.md` written
- [x] `/setup-engine` — Godot 4.6 + C# pinned; `CLAUDE.md`, `technical-preferences.md`, `VERSION.md` updated
- [x] `/map-systems` — `design/gdd/systems-index.md` written (33 systems, 21 MVP)
- [x] `/prototype initiative-ladder` — PROCEED; 3 rounds, 2 harness bugs found and fixed
- [x] `/design-system initiative-ladder` — complete; Open Question 2 now closed
- [x] `/create-architecture` — master blueprint + ADR-0001..0007 accepted
- [x] `/test-setup` — 32 tests passing, CI in `.github/workflows/tests.yml`
- [x] Applicability measurement — `tools/Augury.Tools`, ladder F3/F4 revised
- [x] `/design-system` champion + ability schema
- [ ] `/design-review` on both GDDs — **next, fresh session**
- [ ] ADR-0007 amendment for the mold pair; ADR-0008 (AI search); ADR-0009 (replay)
- [ ] Remaining 11 design-owned MVP GDDs

## Key Decisions

- **Initiative ladder**: an ability at initiative N may be answered by any ability
  at initiative ≤ N, from any champion; the ladder descends until someone passes.
- **Round order**: ladder exchange → death check → status resolution. A champion
  taken to ≤0 HP by statuses survives one more round, debuffed, before dying.
- **Molding**: ability use applies permanent in-match stat changes. Champions
  arrive from the draft unfinished.
- **Three phases**: draft → opening phase (one ability per champion, starts on
  cooldown) → tactical combat. ~25 turns, 10–15 minutes.
- **Turn structure**: alternating, not simultaneous — the ladder supplies the
  collision feel that simultaneous commit would have provided.
- **Action Economy merged into the Initiative Ladder GDD** — they define each other.
- **Physics is presentation-only.** Determinism is a pillar requirement and a
  precondition for post-launch async PvP.
- **Tests split**: xUnit for engine-independent simulation, gdUnit4 for engine
  integration. If a sim test needs Godot to run, the architecture has drifted.
- Items and blitz clock deferred to Vertical Slice — validate one economy first.

## Files Written This Session

| File | Purpose |
|------|---------|
| `design/gdd/game-concept.md` | Full game concept: pitch, pillars, loop, MVP, scope, risks |
| `design/gdd/systems-index.md` | 33 systems with dependencies, priorities, design order, risks |
| `CLAUDE.md` | Technology stack → Godot 4.6 / C# |
| `.claude/docs/technical-preferences.md` | Engine, input, naming, budgets, testing, routing |
| `docs/engine-reference/godot/VERSION.md` | Corrected LLM cutoff (May 2026); 4.6 risk HIGH → MEDIUM |
| `production/review-mode.txt` | `lean` |

## Open Questions

- ~~Action economy~~ **CLOSED**: one action per champion per **half**. Measured —
  per-round left a team with zero available champions entering the half it opens in
  61.8% of rounds, and ended 62% of halves by exhaustion rather than choice.
- Movement's relationship to the ladder — action with initiative, separate phase, or free?
  *(Assumed initiative-1 costing the champion's action; still unverified.)*
- **Applicability geometry** — F4 assumes tier-4 fixed patterns are legal in ~30% of
  board states. That is an assumption about hex geometry, not a decision.
- Equal-initiative exchanges — what prevents ping-pong at initiative 1?
- Negative HP and the dying round — what debuffs apply, can healing rescue?
- Map composition — lanes and minions unresolved (recommendation: cut minions,
  keep lane-shaped corridors plus a jungle).
- Points economy — what awards points, at what rate, to what total.
- Snowball risk — scaling respawns + points race + 15-minute match, with no runway.

## Highest Risks

1. **AI Opponent** — mandatory for ship, hardest technical problem in the project.
2. ~~Initiative Ladder~~ — **de-risked.** Prototyped over 3 rounds; rules hold.
3. **Determinism** — expensive to retrofit, gates PvP permanently.
4. **Molding legibility** — Pillars 1 and 5 collide; drift may read as noise.

## Follow-Ups Noted

- `.claude/docs/coding-standards.md` hardcodes the Godot CI command as
  `godot --headless --script tests/gdunit4_runner.gd`, which assumes GDScript.
  With C# it needs `dotnet test` for the sim plus a gdUnit4 pass. Fix in `/test-setup`.
