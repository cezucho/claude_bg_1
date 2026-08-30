# Opening Phase

> **Status**: Drafted (pending review)
> **System**: #15 Opening Phase — Gameplay layer, MVP
> **Depends on**: Draft · Champion & Ability Schema · Map & Terrain ·
> Movement & Targeting · Sigils & Beacons
> **Blocks**: Initiative Ladder (round 1 starts from its output) · AI Opponent
>
> **Reading the markers.** ⚠ marks a value or claim assumed rather than derived.
> ▸ marks something this document deliberately does **not** decide, naming the system
> that owns it.

> **Quick reference** — Layer: `Gameplay` · Priority: `MVP` · Key deps:
> `Draft, Champion & Ability Schema, Map & Terrain`

## Overview

Before round 1, each team plays **one ability from each of its five champions**. An
ability used in the Opening Phase does not do what it does in combat: it issues **three
instructions**, each addressed to a **role** rather than to a champion — *"jungle moves
forward-left; bottom moves left; top moves forward-right"* — and those instructions move
the team into its starting formation.

Three rules give the phase its shape. An ability is **available only if all three of its
instructions can execute** — nothing partially resolves. A player **must play an available
ability whenever one exists**; declining is not a move. And when no unacted champion has
any available ability at all, the **fallback** applies: every remaining champion moves one
hex in any direction, which guarantees the phase can always finish.

The consequence is that the opening is a **sequencing puzzle rather than five independent
choices**. Measured, five abilities that each move only their own champion produce 1.7
distinct formations across all 120 orderings — order is effectively irrelevant, and the
phase is a setup script both players run past each other. Three-instruction
multi-champion abilities produce **20.5**, because instructions become illegal as
champions fill hexes and which ones are illegal depends on what was played before.

Because instructions name roles, an opening kit is also something the **draft** must
account for. A jungler whose abilities push the team left is worth more beside a bottom
laner who wants to be left. Measured, that alignment is worth nothing at two instructions
per ability and about 6% at three — which is why the count is three.

## Player Fantasy

**The feeling is committing to a plan before you are allowed to have one.**

The draft is over. You know your five champions and you know theirs. What you do not yet
know is whether the shapes you imagined can actually be built, because the only tools for
building them are the same abilities you will later need in a fight, and each one moves
three champions at once whether you wanted all three moved or not.

The good moment is **the unlock**: you look at an ability that is greyed out, work out
that playing a different one first will free the hex it needs, and get both. That is the
phase rewarding a player who reads two steps ahead instead of one.

The bad moment — and it should exist — is **the last champion**. Four abilities spent,
one champion left, and everything it holds is greyed out because the board has filled in
around it. You take the fallback, shuffle one hex, and start the match with a formation
you did not choose. That should feel like a consequence, not a dice roll, because with
full information it was foreseeable four decisions ago.

For the draft, the fantasy is a genuine trade: **a champion who is worse in the late game
but places your team properly may be the better pick.** That is a sentence a player should
be able to say and mean.

## Detailed Rules

### 1. Structure

| | |
|---|---|
| **When** | After the Draft, before round 1 |
| **Each team plays** | Exactly 5 abilities — one from each champion |
| **Per ability** | Exactly **3 instructions** |
| **Order** | Teams alternate, one ability each |
| **Ends when** | Both teams have spent all five champions |

**Starting positions are fixed by role.** Each team's five champions begin on its front
line, one per hex, assigned by role in a fixed order:

| Role | Team A hex | Team B hex |
|---|---|---|
| Top | `(0,−4)` | `(0,4)` |
| Jungle | `(1,−4)` | `(−1,4)` |
| Mid | `(2,−4)` | `(−2,4)` |
| Bottom | `(3,−4)` | `(−3,4)` |
| Support | `(4,−4)` | `(−4,4)` |

This is not decoration and it is not a free choice. **An opening ability cannot be
authored at all unless the author knows where roles start** — legality depends on which
hexes are occupied and where the board edge is, so a free starting assignment would make
every ability's availability unknowable at authoring time. Fixed starts are what make
rule 3 checkable.

**⚠ The team that opens round 1 places second.** Committing first in the opening is an
information disadvantage: the second player places against a formation it can see. Opening
the ladder is also a commitment. Giving those to different teams keeps one side from
paying both. See open question 1.

### 2. Instructions

An opening ability declares exactly **three instructions**, resolved in declared order.

| Instruction | Effect |
|---|---|
| `Move(role, direction)` | The champion in that role moves **one hex** in that direction |
| `PlaceBeacon(role, sigil)` | A friendly beacon of that sigil appears on the hex that role currently occupies |

▸ Hex statuses, buffs to neighbouring champions and other instruction kinds were raised
and are **deferred past MVP**. Two kinds are enough to test whether the phase works, and
`Move` alone carries the sequencing property this document exists for.

**Instructions address roles, never champions.** *"Bottom moves left"* is meaningful
before the draft exists; *"Kara moves left"* is not. This is the mechanism that makes an
opening kit draftable — the same ability is a gift or a nuisance depending on which
champion was picked into that role.

**Directions are team-relative.** The six hex directions are authored in the canonical
forward frame (forward = `+R`) and half-turned for the team facing the other way, exactly
as tier-4 patterns are (ADR-0005, amended). Without this, *"forward"* would mean *"toward
my own nexus"* for one of the two teams. The transform is `Rotate(offset, 3)`, already in
the engine.

**`PlaceBeacon` interacts with ordering, and this is the good kind of interaction.** A
beacon lands on the role's hex *at the moment the instruction resolves*, so placing it
before or after that role has moved puts it in different places. An author writing
`Move(jungle, forward-left)` then `PlaceBeacon(jungle, II)` has written a different
ability from the reverse order. ▸ Beacon rules themselves belong to **Sigils & Beacons**;
this document supplies the placement mechanism that document deferred here.

### 3. Availability is all-or-nothing

> An opening ability is **available** if and only if **all three** of its instructions can
> execute, evaluated in order, each against the board as the previous one left it.

A `Move` instruction can execute when its destination is on the playable board and
unoccupied. Every champion blocks, friendly and enemy alike (Movement & Targeting rule 4).
A `PlaceBeacon` instruction can always execute. ⚠ Beacons do not occupy hexes and never
block (Sigils & Beacons rule 6), so a beacon never makes an ability unavailable.

**Nothing partially resolves.** An ability with two legal instructions and one illegal one
is unavailable, full stop — it is not played at reduced effect.

**Availability is shown, not discovered.** Every available ability is highlighted, for
both teams, at all times. Pillar 1 admits no hidden information, and a phase whose central
constraint had to be worked out by trial and error would be unreadable under a blitz clock.

### 4. You must play if you can

> If any unacted champion holds an available ability, the team **must** play one.

The choice is over **(champion, ability) pairs**, not over champions. A player who wants
to move the jungler but finds every jungler ability greyed out, while the top laner holds
an available one, **must play the top laner's**. Choosing to wait is not a legal move.

This is what makes the phase bind. Without it the compulsory structure evaporates and a
player simply skips anything inconvenient.

**Playing an ability can make a previously unavailable one available.** Measured, ⚠ **27%**
of unavailable abilities become available after another champion moves. That is the
constructive half of the puzzle — playing X specifically to unlock Y — and it is the
reason the phase rewards looking two steps ahead rather than one.

### 5. The fallback

> When **no** unacted champion holds any available ability, each remaining unacted
> champion moves **one hex in any direction**, and the phase ends for that team.

This is the only place in the Opening Phase where a champion moves without an ability, and
it exists to make deadlock structurally impossible. It fires in ⚠ **17.2%** of openings at
three instructions per ability, which is the intended frequency: common enough that a
player must plan against it, rare enough that it is a failure state rather than the norm.

**The fallback is a worse outcome, and must remain so.** One hex in any direction is
strictly less than three instructions' worth of movement, so a team that sequences badly
ends the phase behind one that does not. If tuning ever makes the fallback competitive,
the phase stops being a puzzle and becomes a formality.

### 6. What the opening does not do

- **It does not target enemies.** ⚠ Every instruction addresses a friendly role. Enemy
  champions block movement but cannot be moved. Reaching across is a large design surface
  for a phase that already has one; see open question 3.
- **It does not deal damage, apply status, or resolve combat.** Nothing on the board is
  hostile until round 1 opens.
- **It does not use the initiative ladder.** No ability's initiative value is consulted;
  no answers are permitted; nothing is contested. The ladder begins after this phase ends.
- **It does not consume the ability's combat use.** ▸ Whether the ability spent here starts
  the match on cooldown is **open** — see open question 2. It is not assumed either way in
  this draft.

## Formulas

### F1 — Instruction legality

```
legal(Move(role, dir), board) ⟺  let to = hex(role) + forward_frame(dir, team)
                                  to ∈ playable board
                                ∧ to unoccupied by any champion

legal(PlaceBeacon(role, sigil), board) ⟺ true
```

**Variables:** `forward_frame(dir, team)` is the identity for the team advancing toward
`+R` and `Rotate(dir, 3)` for the other (ADR-0005, amended).
**Example:** Team B's *forward-left* resolves to the world direction that Team A calls
*back-right*, so both teams advance toward each other rather than one walking home.

### F2 — Ability availability

```
available(ability, board) ⟺ ∀ i ∈ 1..3 : legal(instruction_i, board_after(i−1))

board_after(0) = board
board_after(i) = result of applying instruction_i to board_after(i−1)
```

**Output:** boolean, per ability, recomputed after every play and displayed.
**Note** the sequential evaluation: instruction 2's legality is judged against the board
instruction 1 has already changed. An ability that moves the same role twice is therefore
checked as a two-hex path, not as two independent steps.
**Example:** `Move(jungle, fwd-left)`, `Move(jungle, fwd-left)`, `Move(top, left)` is
available only if the jungler can take *both* steps and the top laner can then take its
one — three legality tests against three different boards.

### F3 — The team's legal move set

```
options(team) = { (c, a) : c ∈ unacted(team), a ∈ abilities(c), available(a, board) }

must_play(team) ⟺ options(team) ≠ ∅
fallback(team)  ⟺ options(team) = ∅
```

**Output:** the set of legal plays. ⚠ Measured at three instructions per ability, this set
holds **4.4 of a possible 20** pairs at the first play and shrinks as champions act.
**Example:** four champions acted, one remains with four abilities, none available →
`options = ∅` → that champion takes the fallback and the team is done.

### F4 — Sequencing sensitivity (the property the design exists for)

```
distinct(kit) = |{ final_formation(σ) : σ ∈ permutations of the five abilities }|
```

**Output range:** 1 … 120. ⚠ Measured:

| Design | `distinct` | Reading |
|---|---|---|
| Each ability moves only its own champion | **1.7** | Order is irrelevant. Not a phase |
| 2 instructions, multi-champion | 8.0 | Order matters |
| **3 instructions, multi-champion** | **20.5** | **Adopted** |
| 4 instructions, multi-champion | 36.8 | Deeper, but fallback reaches 24.5% |

**Example:** the same five abilities played jungle-first versus top-first end with the
team on different hexes, because the jungler's second step occupies a hex the top laner's
instruction needed.

### F5 — Draft alignment value

```
misplacement(team) = mean over champions of |file(final) − file(preferred)|
```

**Variables:** `file(h) = 2·q + r`, the across-board reading from Map & Terrain,
ranging −8…+8.
**Output:** ⚠ measured, lower is better:

| Instructions | Aligned draft | Mismatched draft | Benefit |
|---|---|---|---|
| 2 | 2.37 | 2.38 | **none** |
| **3** | **2.37** | **2.52** | **6%** |
| 4 | 2.51 | 2.71 | 7.4% |

**This is why three is the number.** At two instructions an opening kit is not worth
drafting for — the abilities do not push hard enough for compatibility to matter, and the
trade of a weaker late-game champion for better placement does not exist. The axis appears
at three. ⚠ The measured alignment proxy is crude and the placement search plays near
optimally in both conditions, so 6% is a **floor**.

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | An ability's three instructions would move the same role three times | Legal. Availability tests it as a three-hex path (F2), so it is available only if the whole path is clear |
| 2 | An instruction targets a role whose champion is dead | Cannot arise. Nothing damages anything during the Opening Phase (rule 6) |
| 3 | Two instructions in one ability would move two roles into the same hex | The first resolves and occupies it; the second is then illegal, so the **whole ability is unavailable** (F2). It never half-resolves |
| 4 | An enemy champion blocks the destination | The instruction is illegal exactly as if a friendly champion blocked it. Both teams' champions are on the board throughout |
| 5 | A team has one champion left and all four of its abilities are unavailable | The fallback applies to that champion alone: one hex, any direction (rule 5) |
| 6 | No unacted champion has an available ability while three remain | All three take the fallback and the team's phase ends. The fallback is not per-champion; it fires once, for everyone left |
| 7 | A `PlaceBeacon` instruction resolves on a hex that already holds a friendly beacon | ⚠ The new beacon replaces the old one. Two beacons on one hex is a state with no meaning, and rejecting the ability outright would make a beacon a trap laid by your own earlier play |
| 8 | `PlaceBeacon` names a role whose champion has already acted | Legal. Instructions address positions, not availability — a champion that has spent its ability is still standing somewhere |
| 9 | A beacon is placed on a tower or nexus hex | ▸ Open in Sigils & Beacons (its question 3). This document does not decide it |
| 10 | Both teams' champions would meet in the middle during the opening | Possible but unlikely — the front lines are 8 hexes apart and five abilities move each team at most 15 single hexes in total. If they do meet, blocking applies normally |
| 11 | A team could play an available ability that leaves it worse off than the fallback | It must still play it (rule 4). The compulsion has no escape hatch, and this is the zugzwang the phase is meant to produce |
| 12 | The last ability played makes an enemy ability unavailable | Legal and intended. Blocking is mutual, so late placements can deny the opponent's plan — but only as a side effect of your own move, never as a targeted action |

## Dependencies

| System | Direction | What crosses the boundary |
|---|---|---|
| **Draft** | ← consumes | The five champions and their role assignments. F5 is the reason opening kits belong in draft evaluation |
| **Champion & Ability Schema** | → requires | Each ability needs an `OpeningInstructions` field of exactly 3 entries, in addition to its combat definition |
| **Map & Terrain** | ← consumes | Front-line hexes, board bounds, the `file` reading used by F5 |
| **Movement & Targeting** | ← consumes | Blocking rules. Note the opening does **not** use `SPD` or the basic-action budget — instructions move a fixed one hex each |
| **Sigils & Beacons** | **bidirectional** | Supplies beacon semantics; this document supplies the placement mechanism that one deferred to the Opening Phase |
| **Initiative Ladder** | → produces | Round 1 begins from the formation this phase leaves. The ladder itself is not used here |
| **AI Opponent** | → requires | Must search this phase separately from the ladder: 5 champions × 4 abilities × ordering ≈ 123,000 sequences before beacon choices |
| **Board & Unit Presentation** | → requires | Availability highlighting is load-bearing, not a convenience (rule 3) |

## Tuning Knobs

| Knob | Default | Safe range | Affects | Failure at the edges |
|---|---|---|---|---|
| `instructions_per_ability` | **3** | 2–4 | Sequencing depth, draft relevance, fallback rate | At 2 the draft axis vanishes entirely (F5) and `distinct` halves to 8.0; at 4 the fallback reaches 24.5% of openings |
| `abilities_per_champion_with_opening` | **4** (all) | 2–4 | How much choice each champion offers | At 2 a champion's opening role is nearly fixed and the phase loses its selection layer |
| `availability_rule` | **strict** | strict / first-instruction | Fallback rate | Lenient drops the fallback to ~8% but removes most of the constraint that creates the puzzle |
| `fallback_hexes` | **1** | 1–2 | How punishing a failed sequence is | At 2 the fallback rivals a real ability and sequencing stops mattering (rule 5) |
| `opening_places_second` | ⚠ round-1 opener | — | Information balance between the two commitments | See open question 1 |

### Knobs that interact

- **`instructions_per_ability` × `availability_rule`.** These are the same dial viewed from
  two ends. More instructions means more sequencing depth *and* more ways to be
  unavailable; strictness converts the second into fallbacks. Measured under strict,
  fallback runs 8.6% / 17.2% / 24.5% at 2 / 3 / 4 instructions. Moving either knob without
  re-measuring the other will land somewhere unintended.
- **`instructions_per_ability` × Draft.** Below 3 the opening kit is not a draft
  consideration at all (F5). This is the only knob in the project that switches another
  system on and off rather than scaling it.

## Acceptance Criteria

### Rules

1. **GIVEN** a completed Opening Phase, **WHEN** the plays are counted, **THEN** each team
   played exactly one ability from each of its five champions, or took the fallback for
   the remainder (rules 1, 5).
2. **GIVEN** an ability with one illegal instruction, **WHEN** availability is evaluated,
   **THEN** it is unavailable and no part of it resolves (F2).
3. **GIVEN** an ability whose second instruction is illegal only because its first already
   moved a champion, **WHEN** availability is evaluated, **THEN** it is unavailable —
   instructions are judged sequentially, not independently (F2).
4. **GIVEN** a team with at least one available ability, **WHEN** it attempts to decline,
   **THEN** the attempt is rejected (rule 4).
5. **GIVEN** a team whose jungler has no available ability while its top laner does,
   **WHEN** legal options are enumerated, **THEN** the top laner's appears and the
   jungler's does not — the choice is over pairs, not champions (F3).
6. **GIVEN** no unacted champion with an available ability, **WHEN** the fallback fires,
   **THEN** every remaining champion moves exactly one hex and the team's phase ends
   (rule 5, edge case 6).
7. **GIVEN** Team B, **WHEN** an instruction says *forward*, **THEN** it moves toward Team
   A's front line, not Team B's own (F1, ADR-0005 amended).
8. **GIVEN** `Move(jungle, d)` followed by `PlaceBeacon(jungle, s)`, **WHEN** both resolve,
   **THEN** the beacon lands on the jungler's **new** hex; with the instructions reversed,
   on its old one (rule 2).

### Measured behaviour — for the harness

9. **GIVEN** the shipped ability set, **WHEN** all 120 orderings of a team's five plays
   are simulated, **THEN** the mean count of distinct final formations is **≥ 15**. Below
   that the phase is drifting back toward the 1.7 of the independent design (F4).
10. **GIVEN** simulated openings, **WHEN** the fallback rate is measured, **THEN** it lies
    between ⚠ **10% and 25%**. Below 10% the compulsion never bites; above 25% the phase
    is degenerating into move-one-hex-each, which is what it replaced.
11. **GIVEN** aligned and mismatched drafts over identical ability sets, **WHEN**
    misplacement is compared, **THEN** the aligned draft is better by **≥ 5%** (F5). If it
    is not, opening kits are not worth drafting for and the phase's draft layer is fiction.
12. **GIVEN** simulated openings, **WHEN** unlocks are counted, **THEN** ≥ **20%** of
    unavailable abilities become available after another champion acts (rule 4). This is
    the constructive puzzle; near zero means players can only avoid conflicts, never
    create opportunities.

### Cross-system

13. **GIVEN** the AI, **WHEN** it searches the Opening Phase, **THEN** it stays within the
    1.5 s decision budget despite the ≈123,000-sequence space.
14. **GIVEN** identical drafts and identical choices, **WHEN** the phase is replayed,
    **THEN** the resulting formation is byte-identical (ADR-0002).
15. **GIVEN** every shipped ability, **WHEN** content loads, **THEN** each declares exactly
    three opening instructions and each instruction names a role that exists.

## Open Questions

| # | Question | Why it matters | Owner | By when |
|---|---|---|---|---|
| 1 | **Who places first?** ⚠ Assumed: the team that opens round 1 places second, so the two commitments fall on different teams | Placing first is an information disadvantage and opening the ladder is a tempo one. If the same team takes both, the opening is systematically unfair — and the ladder prototype has already been bitten once by an asymmetry that looked like a rules property and was a bug | Initiative Ladder + Balance Harness | Before ladder prototype round 4 |
| 2 | **Does the ability spent in the opening start the match on cooldown?** Not assumed either way in this draft | It would make the opening a decision about combat as well as position — you are choosing which tool to begin without. It also generalises the cost Sigils & Beacons already assumed for beacon-placing abilities. Against: it couples two systems that currently do not touch, and a champion forced into an ability by rule 4 would be punished twice for it | Design | Before the first 4 champions are authored |
| 3 | **May an instruction address an enemy role?** Currently no (rule 6) | "Bottom pushes the enemy support left" is a large and interesting surface, but it turns the opening from a construction puzzle into a contested one, and every legality question doubles | Design | Vertical Slice |
| 4 | **Do hex statuses and neighbour buffs belong here?** Raised and deferred (rule 2) | `Move` and `PlaceBeacon` are enough to test the phase. More instruction kinds would enrich it, but none of them is needed to know whether the sequencing puzzle works | Design | Vertical Slice |
| 5 | **Is a fixed role-to-hex assignment right?** It is currently forced, because authoring needs known start positions (rule 1) | If starting hexes were drafted or chosen, opening abilities could not be checked for availability at authoring time. But a fixed assignment means every match starts from an identical board, which may make openings converge on a solved best line | Design + Balance Harness | Vertical Slice, once 8 champions exist |
| 6 | **Can the phase be solved?** Full information, no opponent interaction until the formations meet, and a bounded search | If a single best opening exists per draft, the phase becomes a lookup rather than a decision. The counterweight is that the opponent's placement is visible and reactable — but only for the team placing second | Balance Harness | Vertical Slice |
