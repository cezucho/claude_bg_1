# Champion Data & Ability Definition Schema

> **Status**: **PARKED (2026-08-16)** — written too early. Do not implement, do not
> author content against it, do not treat its numbers as decided.
> **Systems**: #4 Champion Data & Stat Model · #5 Ability Definition Schema
> **Depends on**: ADR-0002 (integer arithmetic), ADR-0003 (state representation),
> ADR-0005 (hex coordinates and patterns), ADR-0007 (content data format),
> `design/gdd/initiative-ladder.md`, **and an undesigned Board & Map GDD**

> **Status**: Unparked and revised (pending review) — 2026-08-17

## Unparked, 2026-08-17

This was authored before the board existed, which was the wrong order. It was parked on
three counts. All three are now resolved, and the resolutions are recorded here because
each one changed a number.

**1. ~~Half the stat model depends on undesigned systems.~~ Partly resolved — `RES` is
deleted.** `SPD` is settled: `movement-and-targeting.md` defines it as **path length
through unoccupied hexes**, spent from the team's basic-action budget rather than the
champion's ability action. `RES` is not settled — Status Effects still has no GDD, so
`RES` remains a placeholder wearing the costume of a decision. The parking note called
both "candidates for deletion, not merely for tuning", and applying that rule honestly
means **the stat model drops to five**. `RES` returns when Status Effects needs it, and
not before. This is deliberately easy to reverse; see open question 1.

**2. ~~Every applicability number is a statement about board density.~~ Resolved by
re-measurement.** The board is fixed at radius 4 — which the tool had hardcoded, so that
particular assumption turned out to be accidentally correct. What was actually wrong was
*where champions stand*: the harness weighted placement toward three placeholder hexes on
the `q=0` axis. Re-run against the **five real towers** (2026-08-17), the decimals moved
enough to put ladder F4 out of conformance at ±9.7%, and `M` was revised
`[1.0, 1.3, 2.0, 4.0]` → **`[1.00, 1.23, 1.64, 3.30]`**. The targets in rule 5 and F5
below are the re-measured ones.

**3. ~~The kit-shape rule is superseded.~~ Rewritten as a currency.** The old budget —
exactly 10, at most two per tier — admitted precisely three kit shapes. Rule 2 and F4 are
rewritten around the agreed trade: **total initiative is a currency traded against
stats**, so `[1,2,2,4]` is now legal and differentiated rather than illegal.

**Amendments absorbed from later documents:**

| Change | Source |
|---|---|
| `RCH` caps at **3**, not 4 | Movement & Targeting rule 7 |
| Fifth slot: one **passive** per champion (rule 8) | Movement & Targeting rule 6 |
| `printed_sigil` and `slot_sigil` fields (rule 9) | Sigils & Beacons rules 2–3 |
| Tier-4 pattern shape is the huddle-tax lever (rule 5) | Sigils & Beacons rule 9 |
| Tier-4 offsets are **team-relative**, not world-absolute | ADR-0005, amended |
| `M` revised, damage falls ~18% at the top (F2) | Ladder F3, re-measured |

**What survived the rewrite unchanged**, as predicted: the cross rule, the
continuous/threshold stat split, `Displace` as the release valve on rigidity, and
load-time validation as a hard gate.

---

## Overview

A champion is **five stats, four active abilities and one passive**. The stats are stored
in permille and drift during the match; the active abilities are the only thing that moves
them. Every active is therefore two things at once — an action taken now, and a permanent
adjustment to the champion taking it — and the schema treats those as inseparable fields
of one record rather than two systems that happen to be adjacent. The design's
load-bearing rule is that **an ability never molds the stat it scales from**: using a
champion's strongest tool makes that tool weaker and something else in the kit stronger,
so a champion is played as a rotation rather than a button.

Champions are further separated by the **shape of their initiative kit**, and by what that
shape costs. A kit's four initiatives sum to 9, 10 or 11, and **the total is a currency
traded against stats** — sitting low on the ladder buys a stat bonus, sitting high pays
for one. The passive is the champion's only reactive element and exists because a basic
attack resolves before the ladder opens and cannot be answered on it.

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

**Five stats.** All are stored in **permille** (ADR-0002), all are moldable, and each one
must be able to change a decision — a stat that only changes a number is decoration and
does not belong in a model this small.

| Stat | Symbol | Base | Read as | Clamp | What it decides |
|---|---|---|---|---|---|
| **Vitality** | `VIT` | 30000 | HP pool, floored to integer | ±standard | How many exchanges the champion survives |
| **Power** | `POW` | 1000 | multiplier on ability damage | ±standard | How hard everything hits |
| **Armour** | `ARM` | 0 | flat reduction per hit, floored | ±standard | Whether chip damage matters |
| **Reach** | `RCH` | 2000 | +hexes of range, floored | **+1000 max** | Which board states its low tiers can touch |
| **Speed** | `SPD` | 2000 | **path-length** hexes per basic move, floored | ±standard | How much of the team's basic budget it costs to reposition |

> **`RCH` is the one stat with a special clamp, and it is not a balance nicety.** Measured,
> a reach of 4 covers **the entire board from the centre** and 59% of it averaged across
> all positions — on a radius-4 board, "am I safe from that champion?" would stop having
> an answer. `RCH` drift therefore clamps at **+1000**, giving a hard ceiling of 3 hexes,
> where every other stat uses the standard rails in F3.

> **`RES` was deleted.** It read as a permille reduction of status duration, which
> presupposes a Status Effects system that does not exist. See the unparking note.

**Continuous and threshold stats behave differently on purpose.** `POW` and `ARM`
are read as scalars and drift invisibly — a 2.5% change to Power is felt across a
match and unnoticeable within a round. `VIT`, `RCH` and `SPD` are floored to integers
at read time, so they hold still and then snap. Both behaviours are wanted: the
continuous stats deliver Pillar 5 ("small choices, felt not seen") and the threshold
stats deliver the moment where a player *sees* what their play has been doing. The
combat HUD must therefore show progress toward the next threshold, not merely the
current integer, or the snap reads as a bug.

**Reach applies only to free-targeting abilities, and to basic attacks.** Tier-3 and
tier-4 patterns are declared as fixed offsets and are unaffected by `RCH` — a rotatable
arc at range 2 is at range 2 whatever the champion's reach. A basic attack, however,
strikes at `RCH` (Movement & Targeting rule 5), which gives Reach a second job it did not
have when this document was parked: it is the only stat that improves what a champion does
with the team's basic-action budget. This is a consequence of ADR-0005 rather than
a separate rule, and it produces a genuine archetype: a champion molded toward Reach
becomes a relentless low-tier threat and gains nothing whatsoever on its heavy
abilities.

### 2. Abilities: four active slots, and initiative as a currency

Each champion has exactly four **active** abilities in slots **Q, W, E, R**, ordered by
initiative, non-decreasing, plus one **passive** (rule 7). R is always the champion's
heaviest ability. This borrows the MOBA convention deliberately
(`technical-preferences.md`): a player who has never seen a champion before still knows
what R means.

**Initiative total is a currency traded against stats.** A kit whose initiatives sum to
**10** is neutral. Below 10 the kit sits lower on the ladder — individually weaker
abilities, since `M(i)` rises with tier — and **buys a stat bonus** in compensation. Above
10 it sits higher, hits harder, and **pays for it** out of the same account.

```
stat_adjustment = (10 − Σ initiative) × C          ⚠ C = 150 permille
```

The structural constraints that remain:

- initiatives are **non-decreasing** across Q → W → E → R;
- **at most two** abilities may share a tier, so no kit is a single note;
- the sum lies in **9…11**, so the trade stays a nudge rather than a second design axis.

This replaces a rule that admitted exactly three kit shapes. `[1,2,2,4]` sums to 9 and is
now legal, differentiated, and *cheaper on the ladder in exchange for being better in the
stat line* — where before it was simply illegal.

The archetypes survive as **examples rather than an exhaustive list**:

| Shape | Σ | Name | Character |
|---|---|---|---|
| **1-2-3-4** | 10 | **Ladder** | One tool at every rung. Can always answer, can always open, is never the best at either |
| **1-1-4-4** | 10 | **Anvil** | Two cheap tools and two opportunities, nothing between. Pokes and waits for geometry |
| **2-2-3-3** | 10 | **Vice** | **Owns the middle, cannot answer at the bottom.** Once a ladder descends to 1 it is locked out entirely |
| **1-2-2-4** | 9 | *(new, was illegal)* | Sits low, carries a stat bonus. The trade made visible |
| **1-3-3-4** | 11 | *(new, was illegal)* | Top-heavy and stat-poor. Wins the openings it gets |

The Vice's lockout remains the most valuable thing here — a structural, visible,
exploitable weakness a draft can target and a ladder can punish. It is now *an* archetype
rather than a third of the entire space.

> ⚠ **`C` is a guess and the harness must price it.** There is a real tension to check:
> ladder F4 already flattens *effective value* across tiers, so a per-ability initiative
> change is supposed to be balance-neutral. If it truly is, then trading initiative for
> stats **double-compensates** and `C` should be near zero. The case for `C > 0` is that
> F4 prices abilities in isolation while a kit's initiative *distribution* decides
> something F4 never measures — how deep into a descending ladder the champion can still
> act. See open question 2.

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
(`POW`, `ARM`) — 2.5% per use, invisible in the moment. Threshold stats
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
| 1 | Free targeting | **range 3** | **0.96** |
| 2 | Free targeting | range 2 | **0.84** |
| 3 | Rotatable pattern, six facings | 2-hex arc at range 2 | **0.67** |
| 4 | Fixed offsets, **team-relative**, no rotation | 5 hexes | **0.36** |

> Re-measured 2026-08-17 against the five real towers. Tier 1's reference dropped from
> range 4 to range 3 because `RCH` now caps at 3, so the old reference is not a legal
> ability. Tier 4's offsets are authored in a canonical forward frame and half-turned for
> the far team (ADR-0005, amended) — the shape is identical for both, the aim is not.

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
- **Tier-4 pattern *shape* prices the combo system, not just this ability.** A compact
  tier-4 catches **81%** of the champion pairs a beacon zone forces together; a line-shaped
  one catches **24%** (`sigils-and-beacons.md` rule 9). Shape is therefore the lever
  controlling how risky beacon combos are, and a tier-4 author is making a decision about
  a different system whether or not they realise it. Compact shapes make beacons riskier;
  lines make them safe.

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

### 7. Passives

Every champion carries **one passive** alongside its four actives. Passives exist for a
structural reason rather than a flavour one: a basic attack resolves in the basic phase,
before the ladder opens, so it **cannot be answered on the ladder**. The answer has to be
automatic, and a passive is that answer.

- Passives are **triggered, never played**. They consume no ability action, no basic
  action, and no ladder step, and they may fire during either phase.
- Passives **carry no sigil and no slot**, so they can neither start nor finish a
  chain (rule 8). This keeps the 20-ability chain-density figure in `sigils-and-beacons.md` exact.
- Passives **do not mold**. Molding is the price of *choosing* to use an ability (rule 3),
  and a trigger is not a choice.

⚠ The MVP trigger vocabulary is deliberately tiny, and every entry must name a moment the
round structure already has:

| Trigger | Fires when |
|---|---|
| `OnDamaged` | This champion takes damage from any source |
| `OnEnemyEntersReach` | An enemy ends a basic move within `RCH` |
| `OnAllyDies` | A friendly champion dies at round close |
| `OnRoundClose` | The round-close upkeep, after the death check |

**Simultaneous triggers resolve in a defined order, and the order is not incidental**
(ADR-0002). Passives resolve by **ascending champion index within the team, acting team
first**. A passive that triggers another passive resolves the second immediately, to a
depth of ⚠ **1** — no cascades, because a cascade is unbounded and the AI must search it.

### 8. Sigils

Every ability declares two optional fields, both fixed for that ability in every match
and never generated (`sigils-and-beacons.md` rules 2–3):

| Field | Values | Meaning |
|---|---|---|
| `printed_sigil` | none, or one of 3 | Always active. Chains anywhere on the board |
| `slot_sigil` | none, or one of 3 | Inert until its champion stands in a matching friendly beacon |

Most abilities declare **neither**. The measured authoring target across a five-champion
team's 20 abilities is ⚠ **3 printed, 5 slotted, 12 plain** — 15% and 25% respectively.
Those rates are not stylistic: they are what produce 1.01 chainable champion duos with no
beacon and 4.08 with one, which is the density the combo system was tuned to.

An ability may declare both a printed sigil and a slot, but ⚠ should rarely do so — it
makes the ability a chain participant in every circumstance, which is what the typed-slot
design exists to avoid.

### 9. Validation is a load-time gate

ADR-0007 requires content to fail loudly. These invariants are checked on load and a
breach is a hard failure, not a warning:

1. Initiative in 1–4; cooldown in 0–4.
2. Slot initiatives non-decreasing, **summing to 9–11**, at most two per tier, and the
   champion declares which stat carries the `stat_adjustment` (F4).
3. Tier 3 and tier 4 abilities declare at least one offset; tiers 1–2 declare none.
4. Tier 4 declares 4–6 offsets, **authored in the canonical forward frame** (ADR-0005).
5. `MoldUp ≠ MoldDown`, and neither equals the ability's `ScalesFrom`.
6. All permille magnitudes positive; `PowerPermille` matches its tier's `M(i)` within
   ±10%.
7. Exactly **four actives and one passive**; the passive declares a trigger from rule 7's
   vocabulary, declares no mold pair, and declares no sigil.
8. `printed_sigil` and `slot_sigil` each name a sigil or nothing; a **tier-1 ability with
   range 1 fails** unless it also declares a `Move` effect (rule 5).
9. `RES` is not a legal stat reference anywhere in content — it was deleted, and a
   dangling reference is a stale-content bug rather than a tuning value.

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
| `M(i)` | permille | 1000–3300 | Initiative multiplier, ladder F3, **`[1.00, 1.23, 1.64, 3.30]`** |
| `POW` | permille | 700–1800 | Attacker's Power |
| `ARM` | int | 0–6 | Defender's Armour, flat |

**Output range:** 1–17 at `base_power` 3.
**Example:** a tier-4 ability, `base_power` 3, attacker at `POW` 1200, defender at
`ARM` 2 → `⌊3 × 3300 × 1200 ÷ 1000000⌋ − 2` = `11 − 2` = **9 damage**, 30% of a
baseline champion.

> **This example used to read 12 damage and 40%.** The re-measurement against the real
> towers dropped `M(4)` from 4.00 to 3.30, because a fixed 5-hex pattern lines up 36% of
> the time rather than 31% and was being paid for a scarcity it does not have. A tier-4
> strike is now roughly a third of a champion rather than two-fifths — still the largest
> single blow in the game, and still unanswerable as a Last Word.

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

### F4 — Initiative Budget and the Stat Trade

`valid(kit) = (9 ≤ Σ init ≤ 11) ∧ (∀t: count(t) ≤ 2) ∧ (init_Q ≤ init_W ≤ init_E ≤ init_R)`

`stat_adjustment = (10 − Σ init) × C`

| Variable | Type | Range | Description |
|---|---|---|---|
| `Σ init` | int | 9–11 | Kit's total initiative |
| `C` | permille | ⚠ 0–300, default **150** | Exchange rate: stat permille bought per point of initiative forgone |

**Output:** a boolean, plus a signed permille adjustment applied at champion definition to
one declared stat. Evaluated at content load; the adjustment is baked into the champion's
base, not applied as drift.
**Examples:**
- `[1,2,3,4]` Σ 10 → valid, adjustment **0** (Ladder, neutral)
- `[1,2,2,4]` Σ 9 → valid, adjustment **+150** (was illegal under the old rule)
- `[1,3,3,4]` Σ 11 → valid, adjustment **−150**
- `[1,3,3,3]` → **invalid**, three abilities at tier 3
- `[1,1,1,4]` Σ 7 → **invalid**, outside the 9–11 band
- `[2,1,3,4]` → **invalid**, initiatives not non-decreasing

### F5 — Applicability Conformance

`conforms(a) = |measured_applicability(a) − target(tier(a))| ≤ 0.08`
where `target = [0.96, 0.84, 0.67, 0.36]` (ladder F4, **re-measured against the five
real towers, 2026-08-17**)

| Variable | Type | Range | Description |
|---|---|---|---|
| `measured_applicability(a)` | fraction | 0.00–1.00 | From `tools/Augury.Tools`, contested placement, 100,000 samples |
| tolerance | fraction | 0.05–0.12 | Band width. Wider than 0.12 and the tier stops meaning anything |

**Output:** boolean, per ability, evaluated by the harness rather than at load — it
needs a board and a sampler, not just the record.
**Example:** a tier-4 ability with a 3-hex fixed pattern measures 0.233 against a target
of 0.36; `|0.233 − 0.36| = 0.127` → **fails**, and the fix is two more hexes or a
compensating power increase re-solved through ladder F4. Note that the same ability failed
under the old numbers too, by a similar margin — the conformance *verdicts* are mostly
stable even though every decimal moved, which is what a well-shaped band should do.

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
| **Status Effects** | this → that | `EffectKind.Status` is declared here; durations and stacking belong there. **`RES` was deleted** pending that GDD — if status duration needs a stat, it is reintroduced there, not assumed here |
| **Movement & Targeting** | **bidirectional** | `SPD` and `RCH` are declared here, and it caps `RCH` at 3 and defines `SPD` as path length; legal target set generation, basic attacks and the passive's reason for existing belong there |
| **Death, Dying Round & Respawn** | this → that | `VIT` and the edge cases at rows 9–10 constrain what death can mean |
| **Draft** | this → that | Kit shape, initiative total and the stat trade (F4), base stats, and **sigil distribution** are the draft's read on a champion |
| **Content Authoring Pipeline** | this → that | The load-time invariants in rule 9 are this schema's contract with the authoring tool |
| **Sigils & Beacons** | **bidirectional** | Supplies the chain rules and the measured 15%/25% sigil rates; this schema carries `printed_sigil` and `slot_sigil` and owns tier-4 pattern shape, which sets the huddle tax |
| **Combat HUD & State Inspection** | this → that | Pillar 1 requires every stat, its drift, and distance to the next threshold be inspectable without a menu |
| **AI Opponent** | this → that | Stat reads and damage must be cheap; the AI evaluates F1 and F2 at every search node |

**Schema change owed to ADR-0007.** The `AbilityDef` record there carries a single
`MoldStat` / `MoldDelta` pair. This design requires a *pair of pairs* — `MoldUp`,
`MoldUpDelta`, `MoldDown`, `MoldDownDelta` — plus a `ScalesFrom` field to enforce the
cross rule, **`PrintedSigil` and `SlotSigil` fields**, and a **`PassiveDef`** with a
trigger. That is an additive change to a record that has no implementation yet, so it
costs an ADR amendment rather than a migration, but it must be made before content
authoring begins.

## Tuning Knobs

| Knob | Default | Safe range | Too high | Too low |
|---|---|---|---|---|
| `initiative_budget` | 10 (neutral) | band 9–11 | Widening the band past 11 lets kits skew heavy and the ladder rarely descends | Narrowing to exactly 10 restores the old three-shape space this rewrite removed |
| `stat_trade_rate` `C` | ⚠ 150 permille | 0–300 | Above ~250 the trade dominates kit design and everyone drafts the same corner of it | At 0 the trade vanishes and sums 9/10/11 become a free choice — which may in fact be correct, see open question 2 |
| `rch_drift_clamp` | **+1000** | +1000 fixed | Above it, reach 4 threatens the whole board from the centre | Below it, Reach stops being a moldable stat at all |
| `printed_sigil_rate` | ⚠ 15% | 10–30% | Chains stop needing beacons | Combos become invisible to some drafts |
| `slot_sigil_rate` | ⚠ 25% | 15–35% | One beacon lights too much of the team | Dead drafts that cannot chain at all |
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
    ladder F4, **THEN** all four tiers fall within **0.89–0.91** (re-measured; the band
    was 0.92–0.95 under the placeholder objectives).
18. **GIVEN** any tier-1 ability with range 1, **WHEN** content loads, **THEN** load
    fails unless the ability also declares a `Move` effect (rule 5).

### Passives, sigils and the stat trade

19. **GIVEN** a champion definition, **WHEN** it loads, **THEN** it declares exactly four
    actives and one passive, and the passive declares no mold pair and no sigil (rule 7).
20. **GIVEN** two passives that trigger on the same event, **WHEN** they resolve, **THEN**
    the order is ascending champion index, acting team first, and is identical on replay
    (rule 7, ADR-0002).
21. **GIVEN** a passive that triggers another passive, **WHEN** it resolves, **THEN**
    resolution stops at depth 1 — no cascades.
22. **GIVEN** a champion molded toward Reach without limit, **WHEN** `RCH` is read,
    **THEN** it never exceeds **3** (rule 1).
23. **GIVEN** a kit summing to 9, **WHEN** it loads, **THEN** it is valid and carries a
    `+C` adjustment on its declared stat; at 11, `−C`; outside 9–11, load fails (F4).
24. **GIVEN** a full five-champion team at the target sigil rates, **WHEN** chainable duos
    are counted, **THEN** the figures match `sigils-and-beacons.md` criterion 11 — this
    schema is where those rates are actually authored, so a drift here breaks that
    system silently.

### Performance

19. **GIVEN** a `MatchState` containing ten champions' drift, **WHEN** it is cloned,
    **THEN** the clone is a struct assignment with no allocation (ADR-0003).
20. **GIVEN** an AI search node, **WHEN** F1 and F2 are evaluated, **THEN** neither
    allocates, so the 1.5 s decision budget is unaffected by content lookup.

## Open Questions

| # | Question | Why it matters | Owner | Resolve by |
|---|---|---|---|---|
| 1 | **Should `RES` come back?** It was deleted here because Status Effects has no GDD and an inert stat is worse than no stat. If durations end up needing a resistance stat, it must be reintroduced *there*, with a designed job, rather than restored on the assumption it will find one | A five-stat model is simpler to read, mold and display; adding a sixth later costs a HUD change and a mold-space rebalance | Status Effects | When Status Effects is authored |
| 2 | **Is `C`, the initiative-for-stats rate, greater than zero at all?** Ladder F4 already flattens *effective value* across tiers, so trading initiative for stats may double-compensate. The case for `C > 0` is that F4 prices abilities in isolation while a kit's initiative *distribution* decides how deep into a descending ladder a champion can still act — something F4 never measures | If `C` should be 0, the currency rule is decoration and the 9–11 band is a free choice. If it should be large, kit shape becomes the dominant draft axis | Balance Harness | Before the first 8 champions are authored |
| 3 | **Is `Displace` pricing sound?** It is the only way to manufacture tier-4 applicability. If a `Displace` reliably sets up a fixed pattern, tier 4's measured **36%** becomes an illusion and ladder F4 collapses | The whole rigidity design rests on tier 4 being situational | Design + Balance Harness | Before the first tier-4 ability is authored |
| 4 | **Does the kit space now have the opposite problem?** The old rule admitted three shapes and was too narrow. The 9–11 band with ≤2 per tier admits many more — possibly enough that kit shape stops being a legible thing to draft against | The Vice lockout was valuable precisely because it was *recognisable*. A large space of near-identical shapes is worse than three sharp ones | Design | Vertical Slice, after 8 champions exist |
| 5 | **Does the cross rule survive contact with authoring?** It is easy to state and may prove very hard to satisfy for a champion with a strong thematic identity | It is the mechanical basis of the "one machine" pillar; if it is unauthorable it must be replaced, not relaxed | Design | After the first 4 champions |
| 6 | **What is the passive trigger vocabulary's real size?** Four triggers is a guess. Too few and every champion's passive feels alike; too many and the AI must reason about a large reactive surface inside its 1.5 s budget | Passives are the only answer to a basic attack, so a thin vocabulary makes basic attacks effectively unanswerable | Design + AI Opponent | Before the first 4 champions |
| 7 | **What does the HUD show for a threshold stat mid-drift?** A raw permille number breaks the fantasy; no indicator makes the snap read as a bug | Pillar 1 requires inspectability, Pillar 5 requires subtlety, and these pull against each other exactly here | `/ux-design` | Pre-Production |
| 6 | **Does molding apply on use or on resolution?** Assumed at resolution here, before any answer is declared — which means a molded stat can change the damage of the *answering* ability's target calculation mid-ladder | Affects whether the ladder can be read ahead accurately, which Pillar 1 requires | Design | Before Molding GDD is approved |
