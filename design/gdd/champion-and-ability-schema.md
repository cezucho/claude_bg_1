# Champion Data & Ability Definition Schema

> **Status**: **PARKED (2026-08-16)** — written too early. Do not implement, do not
> author content against it, do not treat its numbers as decided.
> **Systems**: #4 Champion Data & Stat Model · #5 Ability Definition Schema
> **Depends on**: ADR-0002 (integer arithmetic), ADR-0003 (state representation),
> ADR-0005 (hex coordinates and patterns), ADR-0007 (content data format),
> `design/gdd/initiative-ladder.md`, **and an undesigned Board & Map GDD**

## Why this document is parked

This was authored before the board existed, which was the wrong order. Three
consequences, all of which must be resolved before it resumes:

**1. Half the stat model depends on undesigned systems.** `SPD` presupposes the
Movement & Targeting rules; `RES` presupposes Status Effects. Neither GDD exists, so
both stats are placeholders wearing the costume of decisions. They are candidates for
deletion, not merely for tuning.

**2. Every applicability number is a statement about board density, not geometry.** The
measurement hardcoded a radius-4 board — a map decision made by a constant in a tool.
Re-run across board sizes (`tools/Augury.Tools`), the numbers move further than this
document's own ±0.08 conformance band:

| Pattern | r3 (37 hex) | r4 (61 hex) | r5 (91 hex) | r6 (127 hex) |
|---|---|---|---|---|
| free, range 1 (melee) | 61.7% | 50.1% | 40.8% | 34.6% |
| free, range 3 | 98.7% | 94.7% | 90.2% | 85.3% |
| rotatable line r1–2 | 79.8% | 68.7% | 60.2% | 52.6% |
| fixed 5 hex | 41.8% | 30.5% | 27.5% | 23.9% |

A tier-4 five-hex pattern is a 42% ability on a small board and a 24% ability on a large
one. The F4 target of 0.31 is not a measurement of that pattern; it is a measurement of
that pattern *on a board nobody chose*. What survives board size unchanged is the
qualitative set — melee is never the most applicable tier, rotatable pattern **area**
never matters while **reach** always does, and fixed-pattern hex count is monotonic.
Those three findings are safe to build on. The decimals are not.

**3. The kit-shape rule is superseded.** This document's initiative budget of exactly 10
with ≤2 per tier admits precisely three kit shapes (Ladder / Anvil / Vice). That was
elegant and too narrow. **Decision (2026-08-16): total initiative is a currency traded
against stats** — a kit summing to 9 buys a stat bonus, one summing to 11 pays for it,
so `[1,2,2,4]` is legal and differentiated rather than illegal. Rule 2 and F4 below are
obsolete and must be rewritten around the trade. The Vice's initiative-1 lockout stays
worth preserving as *an* archetype; it should not be one of only three.

**What remains sound** and should survive the rewrite: the cross rule (an ability never
molds the stat it scales from), the continuous/threshold stat split, `Displace` being
the release valve on rigidity, and load-time validation as a hard gate.

---

## Overview

A champion is six stats and four abilities. The stats are stored in permille and drift
during the match; the abilities are the only thing that moves them. Every ability is
therefore two things at once — an action taken now, and a permanent adjustment to the
champion taking it — and the schema treats those as inseparable fields of one record
rather than two systems that happen to be adjacent. The design's load-bearing rule is
that **an ability never molds the stat it scales from**: using a champion's strongest
tool makes that tool weaker and something else in the kit stronger, so a champion is
played as a rotation rather than a button. Champions are further separated by the shape
of their initiative kit, which a budget rule restricts to exactly three possibilities —
few enough to read off an opponent's portrait, distinct enough that one of them cannot
answer at the bottom of a ladder at all.

## Player Fantasy

You are not managing five characters. You are tuning five instruments, during the
performance, using only the notes you play.

The fantasy this schema exists to produce is the one the concept calls *Champions
Arrive Unfinished*. A champion out of the draft is a rough shape with obvious
tendencies and no commitments. Sixteen rounds later it is a specific thing, and it
became that thing through decisions that never once looked like character-building —
they looked like fighting. You did not choose to make your duelist into a poke threat.
You chose, eleven separate times, to open with the safe ability because the board did
not offer anything better, and the eleventh time you noticed your reach had grown a hex
and the shape of your options had quietly changed underneath you.

The failure mode this design guards against is the opposite feeling: opening a stat
sheet, seeing a build, and executing it. If a player can name their intended end-state
in round two, the system has become a skill tree and the fantasy is dead. The stats
must always be legible when inspected (Pillar 1 demands it) and never *prescriptive*.
That is the tension the whole schema is arranged around, and it is why molding
magnitudes are small enough that no single use feels like a decision about identity.

## Detailed Rules

### 1. The stat model

Six stats. All are stored in **permille** (ADR-0002), all are moldable, and each one
must be able to change a decision — a stat that only changes a number is decoration and
does not belong in a model this small.

| Stat | Symbol | Base | Read as | What it decides |
|---|---|---|---|---|
| **Vitality** | `VIT` | 30000 | HP pool, floored to integer | How many exchanges the champion survives |
| **Power** | `POW` | 1000 | multiplier on ability damage | How hard everything hits |
| **Armour** | `ARM` | 0 | flat reduction per hit, floored | Whether chip damage matters |
| **Reach** | `RCH` | 2000 | +hexes of range, floored | Which board states its low tiers can touch |
| **Speed** | `SPD` | 2000 | movement hexes per move, floored | Whether it can set up its own patterns |
| **Resolve** | `RES` | 0 | permille reduction of status duration | How long it stays poisoned, slowed, held |

**Continuous and threshold stats behave differently on purpose.** `POW`, `ARM` and
`RES` are read as scalars and drift invisibly — a 2.5% change to Power is felt across a
match and unnoticeable within a round. `VIT`, `RCH` and `SPD` are floored to integers
at read time, so they hold still and then snap. Both behaviours are wanted: the
continuous stats deliver Pillar 5 ("small choices, felt not seen") and the threshold
stats deliver the moment where a player *sees* what their play has been doing. The
combat HUD must therefore show progress toward the next threshold, not merely the
current integer, or the snap reads as a bug.

**Reach applies only to free-targeting abilities.** Tier-3 and tier-4 patterns are
declared as fixed offsets and are unaffected by `RCH` — a rotatable arc at range 2 is
at range 2 whatever the champion's reach. This is a consequence of ADR-0005 rather than
a separate rule, and it produces a genuine archetype: a champion molded toward Reach
becomes a relentless low-tier threat and gains nothing whatsoever on its heavy
abilities.

### 2. Abilities: four slots, one budget

Each champion has exactly four abilities in slots **Q, W, E, R**, and the slots are
ordered by initiative, non-decreasing. R is always the champion's heaviest ability.
This borrows the MOBA convention deliberately (`technical-preferences.md`): a player
who has never seen a champion before still knows what R means.

The four initiatives must satisfy an **initiative budget**:

- they sum to exactly **10**;
- **at most two** abilities may share an initiative tier;
- they are non-decreasing across Q → W → E → R.

Those three constraints admit exactly three kit shapes, and each is a distinct thing to
play against:

| Shape | Name | Character |
|---|---|---|
| **1-2-3-4** | **Ladder** | One tool at every rung. Can always answer, can always open, is never the best at either. The default and the safest draft pick |
| **1-1-4-4** | **Anvil** | Two cheap tools and two opportunities, nothing in between. Spends most rounds poking and waiting for geometry; when the board lines up it ends someone |
| **2-2-3-3** | **Vice** | **Owns the middle and cannot answer at the bottom.** Once a ladder descends to initiative 1, a Vice champion is locked out entirely and can only watch |

The Vice's lockout is the reason the budget rule exists. A structural, visible,
exploitable weakness that a draft can target and a ladder can punish is worth more than
a wider space of kit shapes — and driving a ladder down to initiative 1 specifically to
silence the enemy's Vice is exactly the kind of plan the concept means by *the draft
opens, tactics decide*.

### 3. Every ability molds, and never its own scaling stat

Each ability declares a **mold pair**: one stat raised, a different stat lowered, both
applied at resolution, both permanent for the remainder of the match.

The binding constraint is the **cross rule**:

> An ability's `MoldUp` stat and its `MoldDown` stat must both differ from the stat the
> ability scales from.

An ability that scales from `POW` may not mold `POW` in either direction. The
consequence is that using a champion's best ability repeatedly makes it worse at that
ability while making some other part of the kit better — so a kit is a rotation, and
the "five minds, one machine" pillar has a mechanical basis rather than a thematic one.
Without this rule the dominant strategy is always to find each champion's best ability
and press it until the match ends, which is the exact failure the pillar names.

**Molding is applied to state, never to content** (ADR-0007). A champion's definition
is immutable; the drift lives in `MatchState` and dies with the match.

### 4. Molding magnitudes

Mold deltas are permille and small. Default magnitude is **25** on continuous stats
(`POW`, `ARM`, `RES`) — 2.5% per use, invisible in the moment. Threshold stats
(`VIT`, `RCH`, `SPD`) use larger deltas because their read is floored, defaulting to
**60**, so roughly seventeen uses buy a hex of reach.

A cooldown-2 ability fires about eight times across a sixteen-round match, so a typical
ability contributes ≈200 permille of drift to its up-stat: a 20% change over a match,
reached in increments no one would call a decision. Spamming a cooldown-0 ability is
the degenerate case, and it is self-correcting — the cross rule means the spammed
ability is not the one getting stronger.

### 5. Rigidity and applicability are schema constraints

The ladder GDD's F4 fixes an applicability band per initiative tier, measured rather
than assumed (`tools/Augury.Tools`). Those bands are **authoring constraints on this
schema**, not advisory targets:

| Tier | Rigidity | Reference geometry | Target applicability |
|---|---|---|---|
| 1 | Free targeting | range 4 | 0.99 |
| 2 | Free targeting | range 2 | 0.81 |
| 3 | Rotatable pattern, six facings | 2-hex arc at range 2 | 0.59 |
| 4 | Fixed offsets, no rotation | 5 hexes | 0.31 |

Two authoring rules fall directly out of the measurement and are binding:

- **Tier-1 abilities are ranged.** A range-1 ability reaches an enemy in only half of
  contested board states. Melee at tier 1 is not a stylistic choice, it is a 50%
  applicability ability priced as a 99% one, and the schema rejects it unless the
  ability also carries a movement component that closes the gap.
- **Extra hexes on a tier-3 pattern are damage, not reach.** A 2-hex line and a 3-hex
  wedge have identical applicability, because six facings make pattern area nearly
  irrelevant. Tier-3 patterns are priced on which distance rings they touch. Tier-4
  patterns are the opposite case — with rotation unavailable, each hex is worth ≈6
  points of applicability, which is what sets the 5-hex target.

### 6. Effects

An ability declares one `EffectKind`. The MVP set is deliberately short; every entry
must interact with the ladder's round structure or it waits.

| Effect | Resolves | Notes |
|---|---|---|
| `Damage` | Immediately, before the next answer | Cannot kill mid-ladder — the death check is at round close (ladder Core Rule 6) |
| `Heal` | Immediately | Can rescue a Dying champion if it lands before the next death check |
| `Status` | Applies a status; ticks in the round-close status phase | The only route to the Dying state |
| `Displace` | Immediately, moves a champion by declared offset | The tier-4 setup tool: moves an enemy *into* a fixed pattern |
| `Shield` | Immediately; cleared at round-close upkeep | Absorbs before Armour |
| `Move` | Immediately, moves the caster | What makes a melee tier-1 ability legal under rule 5 |

`Displace` is the most important entry and the one to watch in review. It is the only
way a player can manufacture tier-4 applicability rather than wait for it, which makes
it the release valve on the whole rigidity design — and, if mispriced, the thing that
turns a 31% ability into a 90% one.

### 7. Validation is a load-time gate

ADR-0007 requires content to fail loudly. These invariants are checked on load and a
breach is a hard failure, not a warning:

1. Initiative in 1–4; cooldown in 0–4.
2. Slot initiatives non-decreasing, summing to 10, at most two per tier.
3. Tier 3 and tier 4 abilities declare at least one offset; tiers 1–2 declare none.
4. Tier 4 declares 4–6 offsets.
5. `MoldUp ≠ MoldDown`, and neither equals the ability's `ScalesFrom`.
6. All permille magnitudes positive; `PowerPermille` matches its tier's `M(i)` within
   ±10%.

## Formulas

> Every value is a starting point for the Balance Simulation Harness, except where
> marked as measured. All arithmetic is integer; `⌊÷⌋` is `Arith.FloorDiv` (ADR-0002),
> which is the project's single rounding rule.

### F1 — Stat Read

`read(stat) = ⌊(base(stat) + drift(stat)) ÷ 1000⌋` for threshold stats
`read(stat) = base(stat) + drift(stat)` for continuous stats (used as permille)

| Variable | Type | Range | Description |
|---|---|---|---|
| `base(stat)` | int | 0–40000 | Value from the champion definition, immutable |
| `drift(stat)` | int | −1000–+2000 | Accumulated molding, lives in `MatchState` |

**Output:** `VIT` 20–45 HP · `RCH` 1–4 hexes · `SPD` 1–4 hexes · `POW` 700–1800 permille.
**Example:** `RCH` base 2000 with +1050 drift reads as `⌊3050 ÷ 1000⌋` = **3 hexes**;
the champion is 950 permille — sixteen further uses — from a fourth.

### F2 — Damage

`damage = max(1, ⌊base_power × M(i) × POW ÷ 1000000⌋ − ARM)`

| Variable | Type | Range | Description |
|---|---|---|---|
| `base_power` | int | 3–5 | Reference damage, ladder F3 |
| `M(i)` | permille | 1000–4000 | Initiative multiplier, ladder F3, `[1.0, 1.3, 2.0, 4.0]` |
| `POW` | permille | 700–1800 | Attacker's Power |
| `ARM` | int | 0–6 | Defender's Armour, flat |

**Output range:** 1–21.
**Example:** a tier-4 ability, `base_power` 3, attacker at `POW` 1200, defender at
`ARM` 2 → `⌊3 × 4000 × 1200 ÷ 1000000⌋ − 2` = `14 − 2` = **12 damage**, 40% of a
baseline champion.

The `max(1, …)` floor exists so that Armour can never fully negate an ability. An
ability that cannot do anything is an ability that cannot bait a response, and the
ladder needs every legal action to be worth answering.

### F3 — Molding Application

`drift′(up) = clamp(drift(up) + δ_up, −1000, +2000)`
`drift′(down) = clamp(drift(down) − δ_down, −1000, +2000)`

| Variable | Type | Range | Description |
|---|---|---|---|
| `δ_up`, `δ_down` | permille | 20–80 | Per-use magnitude. Default 25 continuous, 60 threshold |
| clamp bounds | permille | −1000, +2000 | Hard rails: no stat may fall below 0 base or triple |

**Output:** new drift values, applied at resolution, before any answering ability is
declared.
**Example:** a `POW`-scaling ability with `MoldUp = RCH (+60)`, `MoldDown = ARM (−25)`,
used eight times across a match: `RCH` +480 permille (roughly half a hex), `ARM` −200.

The clamps are what stop a sixteen-round match from producing a champion unrecognisable
from its definition. They are rails, not a budget — a champion that hits a clamp has
been played very narrowly and should feel the cost.

### F4 — Initiative Budget Validation

`valid(kit) = (Σ init = 10) ∧ (∀t: count(t) ≤ 2) ∧ (init_Q ≤ init_W ≤ init_E ≤ init_R)`

**Output:** boolean, evaluated at content load.
**Example:** `[1,2,3,4]` → valid (Ladder) · `[2,2,3,3]` → valid (Vice) ·
`[1,3,3,3]` → invalid, three abilities at tier 3 · `[1,1,3,4]` → invalid, sums to 9.

### F5 — Applicability Conformance

`conforms(a) = |measured_applicability(a) − target(tier(a))| ≤ 0.08`
where `target = [0.99, 0.81, 0.59, 0.31]` (ladder F4, **measured**)

| Variable | Type | Range | Description |
|---|---|---|---|
| `measured_applicability(a)` | fraction | 0.00–1.00 | From `tools/Augury.Tools`, contested placement, 100,000 samples |
| tolerance | fraction | 0.05–0.12 | Band width. Wider than 0.12 and the tier stops meaning anything |

**Output:** boolean, per ability, evaluated by the harness rather than at load — it
needs a board and a sampler, not just the record.
**Example:** a tier-4 ability with a 3-hex fixed pattern measures 0.188 against a target
of 0.31; `|0.188 − 0.31| = 0.122` → **fails**, and the fix is two more hexes or a
compensating power increase re-solved through ladder F4.

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | A champion molds `SPD` below 1000, reading as 0 movement hexes | It cannot move, and this is legal and intended. `Move`-effect abilities still relocate it, so the kit becomes its only mobility. The clamp at −1000 means base 2000 can reach 1000, never 0 |
| 2 | An ability's `MoldDown` stat is already at the −1000 clamp | The decrement is discarded; the increment still applies. The ability becomes strictly better, which is a real and intended reward for having paid the cost already |
| 3 | A tier-4 pattern's offsets are all off-board | The ability has no legal targets and F1 of the ladder GDD returns false. It is skipped by the legal action generator and never offered. It does **not** error — board edges are a legitimate reason for a rigid ability to be dead |
| 4 | `Displace` moves an enemy onto an occupied hex | The displacement is truncated at the last unoccupied hex along the path. If that is the origin, no movement occurs and the ability still resolves and still molds |
| 5 | `Displace` would move a champion off-board | Same truncation rule. A champion cannot be pushed off the board — there is no ring-out in this game |
| 6 | `Heal` on a champion in the Dying state | Resolves normally. If it lifts HP above 0 before the next death check, the champion survives and Dying clears (ladder Core Rule 6, ADR-0006) |
| 7 | Two abilities in a kit share an initiative and a slot ordering is ambiguous | Slot order breaks the tie by declaration order in the JSON, which is stable. Never by index comparison at runtime |
| 8 | An ability scales from a stat it also displays as its mold-up stat | Rejected at load by invariant 5. This is the cross rule and it is a hard failure, because the alternative is a self-reinforcing ability that silently becomes optimal |
| 9 | A champion's `VIT` drift raises max HP mid-match | Current HP does **not** rise with it. Molding grants a bigger pool, never free healing — otherwise `VIT` molding becomes a sustain ability with no cooldown |
| 10 | `VIT` drift lowers max HP below current HP | Current HP is clamped down to the new maximum immediately. This can bring a champion to 0, which enters the normal death check at round close rather than killing instantly |
| 11 | A `Shield` is still active at round close | Cleared at upkeep (ADR-0006 step 4), after the status phase. A shield therefore protects against the status tick, which is the point of taking one |
| 12 | An ability with `Move` effect and a free-targeting pattern is used with `RCH` 0 | Reach floors at a minimum of 1 for `Move`-effect abilities specifically; a mobility tool that cannot move anywhere is a dead slot, and dead slots break the initiative budget's guarantee that a champion can always act |

## Dependencies

| System | Direction | What crosses the boundary |
|---|---|---|
| **Deterministic Simulation Core** | this ← that | Stat drift lives in `MatchState`; all reads go through `Arith.FloorDiv` |
| **Initiative Ladder & Action Economy** | **bidirectional** | Ladder supplies initiative tiers, `M(i)`, exposure and the applicability bands (F3/F4 there); this schema supplies initiative, cooldown, rigidity tier and pattern offsets (its Interactions table) |
| **Molding** | this → that | Mold pairs and magnitudes are declared here; the Molding GDD owns presentation, history and the drift readout |
| **Damage & Combat Resolution** | this → that | F2 is the reference damage formula; the Damage GDD owns crits, mitigation order and multi-target falloff |
| **Status Effects** | this → that | `EffectKind.Status` and `RES` are declared here; durations and stacking belong there |
| **Movement & Targeting** | **bidirectional** | `SPD` and `RCH` are declared here; legal target set generation from pattern offsets belongs there |
| **Death, Dying Round & Respawn** | this → that | `VIT` and the edge cases at rows 9–10 constrain what death can mean |
| **Draft** | this → that | Kit shape (Ladder / Anvil / Vice) and base stats are the draft's read on a champion |
| **Content Authoring Pipeline** | this → that | The load-time invariants in rule 7 are this schema's contract with the authoring tool |
| **Combat HUD & State Inspection** | this → that | Pillar 1 requires every stat, its drift, and distance to the next threshold be inspectable without a menu |
| **AI Opponent** | this → that | Stat reads and damage must be cheap; the AI evaluates F1 and F2 at every search node |

**Schema change owed to ADR-0007.** The `AbilityDef` record there carries a single
`MoldStat` / `MoldDelta` pair. This design requires a *pair of pairs* — `MoldUp`,
`MoldUpDelta`, `MoldDown`, `MoldDownDelta` — plus a `ScalesFrom` field to enforce the
cross rule. That is an additive change to a record that has no implementation yet, so
it costs an ADR amendment rather than a migration, but it must be made before content
authoring begins.

## Tuning Knobs

| Knob | Default | Safe range | Too high | Too low |
|---|---|---|---|---|
| `initiative_budget` | 10 | 9–11 | At 11+, kits skew heavy and the ladder rarely descends | At 9 the three archetypes become one; below that, tier 4 disappears from kits |
| `max_per_tier` | 2 | 1–2 | At 3 the Vice's lockout weakness vanishes and kit shapes stop being legible | At 1 only `1-2-3-4` is legal and champions stop differing structurally |
| `mold_delta_continuous` | 25 | 20–40 | Above 40 a match is decided by drift rather than tactics; identity becomes visible in round two | Below 20 molding is arithmetic noise and Pillar 3 has no mechanical existence |
| `mold_delta_threshold` | 60 | 40–100 | Thresholds cross more than twice a match and the board stops being predictable | Thresholds never cross and half the stat model is inert |
| `drift_clamp` | −1000 / +2000 | ±(500–3000) | Champions become unrecognisable from their definitions | Molding hits the rail by mid-match and the last third of the match has no drift left |
| `base_vitality` | 30000 | 24000–40000 | Exchanges never resolve; matches run past the F5 round budget | Champions die in two exchanges and the ladder never descends |
| `base_reach` | 2000 | 2000–3000 | At 3 hexes base, tier-2 applicability rises to 0.95 and F4's flatness breaks | Below 2 every champion is melee, and melee measures at 50% applicability |
| `applicability_tolerance` | 0.08 | 0.05–0.12 | Above 0.12 tiers overlap and rigidity stops pricing power | Below 0.05 almost no hand-authored pattern conforms and authoring stalls |
| `armour_scale` | flat | flat / permille | — | — |

### Knobs that interact

- **`base_reach` × ladder `M(i)` × `applicability`** are one equation, not three knobs.
  Reach moves tier-1 and tier-2 applicability directly (measured: range 2 → 3 is +14
  points, range 3 → 4 is only +4.5), so raising base reach requires re-solving ladder
  F4. The saturation is the useful part — Reach is safe to mold precisely because it
  runs out of value.
- **`mold_delta_*` × `drift_clamp` × match length** set how much of a champion's
  identity is authored versus played. At the defaults, roughly 20% is played.
- **`initiative_budget` × `max_per_tier`** jointly determine the set of legal kit
  shapes, and that set is currently exactly three. Either knob moving changes the
  archetype vocabulary of the whole game, so they move together or not at all.

## Acceptance Criteria

### Stat model

1. **GIVEN** a champion at `RCH` base 2000 with +1050 drift, **WHEN** reach is read,
   **THEN** it returns 3 (F1).
2. **GIVEN** any threshold stat and any drift, **WHEN** it is read twice, **THEN** both
   reads are identical — no read is time-, order-, or float-dependent.
3. **GIVEN** `VIT` drift that lowers max HP below current HP, **WHEN** the stat is
   applied, **THEN** current HP is clamped down in the same operation and no HP is
   silently retained (edge case 10).
4. **GIVEN** `VIT` drift that raises max HP, **WHEN** the stat is applied, **THEN**
   current HP is unchanged (edge case 9).
5. **GIVEN** drift already at a clamp bound, **WHEN** a further mold in that direction
   is applied, **THEN** the value is unchanged and no error is raised (edge case 2).

### Ability schema

6. **GIVEN** a kit with initiatives `[1,3,3,3]`, **WHEN** content loads, **THEN** load
   fails with a message naming the tier-count violation (F4, invariant 2).
7. **GIVEN** a kit with initiatives summing to 9, **WHEN** content loads, **THEN** load
   fails (F4).
8. **GIVEN** an ability whose `ScalesFrom` equals its `MoldUp`, **WHEN** content loads,
   **THEN** load fails naming the cross rule (invariant 5).
9. **GIVEN** a tier-4 ability declaring 3 offsets, **WHEN** content loads, **THEN**
   load fails (invariant 4).
10. **GIVEN** a tier-1 or tier-2 ability declaring offsets, **WHEN** content loads,
    **THEN** load fails — free-targeting abilities have no pattern (invariant 3).
11. **GIVEN** the shipped champion set, **WHEN** kit shapes are tallied, **THEN** every
    champion is exactly one of Ladder, Anvil or Vice, and all three appear.

### Damage and molding

12. **GIVEN** `base_power` 3, tier 4, `POW` 1200, `ARM` 2, **WHEN** damage is computed,
    **THEN** it equals 12 (F2).
13. **GIVEN** any attacker and a defender whose Armour exceeds raw damage, **WHEN**
    damage is computed, **THEN** it equals 1, never 0 or negative (F2).
14. **GIVEN** an ability used eight times with `MoldUp = RCH (+60)`, **WHEN** drift is
    read, **THEN** it equals exactly +480 — molding accumulates by integer addition
    with no rounding at any step (F3).
15. **GIVEN** an identical action sequence replayed, **WHEN** final stat drift is
    serialised, **THEN** it is byte-identical to the first run. Blocking gate: Pillar 1
    and asynchronous PvP both depend on it.

### Applicability conformance

16. **GIVEN** every shipped ability, **WHEN** `tools/Augury.Tools` measures its pattern
    under contested placement, **THEN** each conforms to its tier band within ±0.08
    (F5).
17. **GIVEN** the tier reference patterns, **WHEN** effective value is computed through
    ladder F4, **THEN** all four tiers fall within 0.92–0.95.
18. **GIVEN** any tier-1 ability with range 1, **WHEN** content loads, **THEN** load
    fails unless the ability also declares a `Move` effect (rule 5).

### Performance

19. **GIVEN** a `MatchState` containing ten champions' drift, **WHEN** it is cloned,
    **THEN** the clone is a struct assignment with no allocation (ADR-0003).
20. **GIVEN** an AI search node, **WHEN** F1 and F2 are evaluated, **THEN** neither
    allocates, so the 1.5 s decision budget is unaffected by content lookup.

## Open Questions

| # | Question | Why it matters | Owner | Resolve by |
|---|---|---|---|---|
| 1 | **Is `Displace` pricing sound?** It is the only way to manufacture tier-4 applicability. If a `Displace` reliably sets up a fixed pattern, tier 4's measured 31% becomes an illusion and ladder F4 collapses | The whole rigidity design rests on tier 4 being situational | Design + Balance Harness | Before the first tier-4 ability is authored |
| 2 | **Are three kit shapes enough?** The budget rule yields exactly Ladder, Anvil and Vice. Structural variety then has to come entirely from patterns, effects and base stats | If three is too few, champions will feel same-shaped regardless of their abilities | Design | Vertical Slice, after 8 champions exist |
| 3 | **Does the cross rule survive contact with authoring?** It is easy to state and may prove very hard to satisfy for a champion with a strong thematic identity | It is the mechanical basis of the "one machine" pillar; if it is unauthorable it must be replaced, not relaxed | Design | After the first 4 champions |
| 4 | **Should `RES` exist in MVP?** It only matters once Status Effects is designed, and a six-stat model with one inert stat is worse than a five-stat model | Cutting it simplifies the HUD and the mold space | Design | After Status Effects GDD |
| 5 | **What does the HUD show for a threshold stat mid-drift?** A raw permille number breaks the fantasy; no indicator makes the snap read as a bug | Pillar 1 requires inspectability, Pillar 5 requires subtlety, and these pull against each other exactly here | `/ux-design` | Pre-Production |
| 6 | **Does molding apply on use or on resolution?** Assumed at resolution here, before any answer is declared — which means a molded stat can change the damage of the *answering* ability's target calculation mid-ladder | Affects whether the ladder can be read ahead accurately, which Pillar 1 requires | Design | Before Molding GDD is approved |
