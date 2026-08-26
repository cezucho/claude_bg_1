# Movement & Targeting

> **Status**: Drafted (pending review)
> **System**: #6 Movement & Targeting — Gameplay layer, MVP
> **Depends on**: Hex Grid (ADR-0005) · Map & Terrain · Initiative Ladder ·
> Champion Data & Stat Model
> **Blocks**: Ability Definition Schema · Champion Data & Stat Model · Opening Phase ·
> AI Opponent · Damage & Combat Resolution
>
> **Reading the markers.** ⚠ marks a value or claim assumed rather than derived.
> ▸ marks something this document deliberately does **not** decide, naming the system
> that owns it.

> **Quick reference** — Layer: `Gameplay` · Priority: `MVP` · Key deps:
> `Initiative Ladder, Map & Terrain`

## Overview

A half runs in two parts. First the **basic phase**: each team takes exactly **two basic
actions**, and a basic action is either *move one champion* or *make one basic attack*.
The two must be taken by **two different champions**, and taking them is **compulsory**.
Then the **ladder phase**: abilities are played on the initiative ladder as the Initiative
Ladder GDD describes, and **no champion moves for the rest of the half**.

Movement is therefore **not an action on the ladder**. It is a separate economy, resolved
before the exchange begins. This replaces the Initiative Ladder's provisional assumption
that a move was an initiative-1 action costing the champion's action for the half, and
closes that document's open question 3.

Two consequences carry the design. First, **positions are locked during an ability
exchange**, so a fixed pattern can no longer be dodged by a cheap answer mid-ladder — the
board that the exchange resolves against is the board both players committed to before it
started. Second, **breadth is the scarce resource, not speed**: an individual champion
moves exactly as fast as it did under the old rules, but a team can only reposition two
champions per half, so walking one champion across the board costs 50% of the team's
entire basic economy for the two rounds it takes.

Champions **block movement**. With no impassable terrain anywhere on the map, bodies are
the only obstacles the board can have, which makes formation a real instrument rather than
a picture. Targeting requires **no line of sight**.

## Player Fantasy

**The feeling is that every step is a decision about who *doesn't* move.**

Two basics per half against five champions means three champions stand still every half,
and choosing which three is the positional game. There is no drifting the whole team
forward together. A team advances the way a real formation advances — unevenly, with
someone always exposed and someone always late.

The second feeling is **traffic**. Bodies block, so a board with ten champions on it is
congested in a way an empty hex grid never is. Your own front rank gets in your own way.
A champion you wanted at the centre has to go around the fight rather than through it, and
the two hexes of detour are two more basics you do not have.

The third is **commitment**. When the basic phase closes, the board is frozen for the rest
of the half. Everything the exchange does, it does to the positions you just chose. There
is no flinching once the abilities start — you either read the fight correctly before it
began or you did not.

## Detailed Rules

### 1. The two economies

| Economy | Budget | Spent on | On the ladder? |
|---|---|---|---|
| **Basic** | 2 per team per half | moving a champion, or a basic attack | **No** |
| **Ability** | 1 per champion per half | abilities | **Yes** |

They are independent. **A champion that took a basic action may still use an ability in
the same half**, and vice versa. This is the point of separating them: positioning is no
longer paid for out of the same pocket as fighting.

The Initiative Ladder's action economy — one ability per champion per half, availability
resetting at the half boundary — is **unchanged**. This document adds a second budget
beside it; it does not modify the first.

### 2. The basic phase

**Two basic actions per team per half, taken by two different champions.** No champion
takes more than one basic action in a half. ⚠ The special case of a team with only one
champion alive is deferred (open question 2).

**Basics are compulsory.** A team must spend both. This is an anti-stall rule, and it has
a property worth naming: with an enemy in reach it is satisfied by attacking, so it is
silent during a fight; with nobody in reach the only legal basic is a move, so it becomes
**pressure toward contact** exactly when the board has gone quiet. It never binds during
combat and always binds during a standoff.

**Order.** ⚠ Basics alternate, beginning with the team that opens that half. So the
sequence is opener, opponent, opener, opponent. The opening team commits first, which is
the same cost it already pays for opening the ladder.

**Basics resolve fully before the ladder opens.** Once the fourth basic resolves, positions
are final for the half.

### 3. Movement

A **move** relocates one champion up to `SPD` hexes along a **path of unoccupied hexes**.

- Path length, not straight-line distance, is what `SPD` limits. A champion that must go
  around a body pays for the detour.
- **A move must be at least one hex.** A zero-hex move is not a legal basic action;
  otherwise the compulsory rule would be meaningless.
- A champion may not end on an occupied hex, and may not pass through one.
- ▸ Beacons do **not** block and do not occupy a hex for movement (Sigils & Beacons rule 6).

### 4. Champions block

**Every champion blocks movement, friendly and enemy alike.**

This is the map's only source of obstruction. Map & Terrain rule 3 rejects impassable
terrain, and its open question 6 asks whether the board needs any; this answers it —
**the board's terrain is the other players**, which is dynamic, symmetric, and earned
rather than authored.

Friendly champions blocking is deliberate, not an oversight. It means a tight formation
gets in its own way, which is a real cost for clustering and pairs with the huddle tax in
Sigils & Beacons rule 9. ⚠ This is the most likely thing here to feel bad in play; see
`friendlies_block`.

**A wall never seals the board.** Measured: five champions across the board's waist add
**3 hexes of detour** and leave every hex reachable — a radius-4 hexagon is too wide to
span with five bodies. Blocking taxes movement; it cannot deny it. That is what keeps this
rule from recreating the corridors Map & Terrain rejected.

### 5. Basic attacks

A **basic attack** is the other thing a basic action may be: one champion damages one
enemy champion or structure within `RCH`.

- **Not answerable on the ladder.** A basic attack resolves in the basic phase, before the
  ladder opens, so there is no exchange in which to answer it. This is why it must be
  weak — ▸ its damage is owned by **Damage & Combat Resolution**.
- **Answered by passives instead** (rule 6).
- ▸ Whether basic attacks can damage structures at all is owned by **Objectives &
  Scoring**, and it matters: if they can, a team can grind an objective without ever
  opening the ladder.

### 6. Passives

**Every champion has one passive ability**, in addition to its four active abilities.

Passives exist principally so that basic attacks have an answer. A basic attack cannot be
responded to on the ladder, so the response has to be automatic: a passive triggers on a
condition — being damaged, an enemy entering reach, a neighbour dying — and resolves
without either player choosing.

- Passives are **triggered, never played**. They consume no action, no basic, and no
  ladder step.
- Passives **carry no sigil and no slot**. They cannot start or finish a chain, so the
  20-ability figure in Sigils & Beacons rule 8 is unaffected.
- ▸ The passive trigger vocabulary, and the constraint that triggers resolve
  deterministically in a defined order, are owned by the **Ability Definition Schema**.

> This is an addition to the champion schema, which currently defines four abilities per
> champion. It becomes **4 actives + 1 passive**.

### 7. Targeting

**No line of sight.** Any legal target within range may be targeted, regardless of what
stands between. Three reasons, in order of weight:

1. **Rigidity already constrains targeting.** Tier 3 is a rotatable pattern and tier 4 a
   fixed one — the game's answer to "you cannot just hit what you like" is the shape of
   the ability, not an occlusion rule. Adding LOS would be a second, redundant constraint.
2. **There is no friendly fire**, so blocking shots on allies has nothing to model.
3. Hex line-of-sight is notoriously fiddly, and every edge case it generates is a rule the
   player must hold under a blitz clock for very little depth in return.

**Reach.** A champion threatens exactly the hexes within `RCH` of where it stands. Because
the basic phase closes before the ladder opens, threat during an exchange is **static and
fully readable** — there is no move-then-strike, so no target ever needs to compute
`RCH + SPD`.

⚠ **`RCH` must cap at 3.** Measured, a reach of 4 covers **the entire board from the
centre** and 59% of it averaged across all positions, so a radius-4 board cannot express
4 as a limit at all — "am I safe from that champion?" stops having an answer. The Champion
Data & Stat Model currently declares `RCH` as 1–4 and needs amending. See open question 1.

## Formulas

### F1 — Legal move destinations

```
moves(X) = { h : h ∈ board
               ∧ h unoccupied
               ∧ 1 ≤ pathlen(hex(X), h) ≤ SPD(X) }

pathlen(a, b) = length of the shortest path from a to b through unoccupied hexes
                (breadth-first over the six neighbour directions; ∞ if none exists)
```

**Variables:** `SPD(X)` ∈ 1…4 hexes, floored from permille (Champion schema).
**Note** `pathlen ≥ distance` always, with equality only when no body intervenes.
**Example:** `SPD 2`, five enemies walling rank 0. A champion at `(0,−3)` reaching `(0,3)`
has straight-line distance 6 but path length 9, so it needs 5 moves rather than 3 — the
wall costs it one extra round and a half.

### F2 — Legal basic attack targets

```
strikes(X) = { T : T is an enemy champion or structure
                 ∧ distance(hex(X), hex(T)) ≤ RCH(X) }
```

**Straight-line distance, not path length** — reach is not blocked (rule 7).

### F3 — Basic action legality

```
basics(team, half) must contain exactly 2 entries
    with distinct champions
    each entry ∈ { move(X, h) : h ∈ moves(X) } ∪ { strike(X, T) : T ∈ strikes(X) }
```

**The requirement is always satisfiable.** Every champion always has at least one legal
move unless fully enclosed, and a structure is always somewhere on the board to walk
toward. ▸ Full enclosure of *every* champion simultaneously is addressed in edge case 4.

### F4 — Threat set

```
threat(X) = { h : distance(hex(X), h) ≤ RCH(X) }
```

**Coverage**, measured over the 61-hex board:

| `RCH` | from board centre | averaged over every hex |
|---|---|---|
| 1 | 7 hexes (11.5%) | 6.1 (10.0%) |
| 2 | 19 hexes (31.1%) | 14.7 (24.1%) |
| 3 | 37 hexes (60.7%) | 25.1 (41.1%) |
| ~~4~~ | ~~61 hexes (100%)~~ | ~~35.9 (58.9%)~~ — see open question 1 |

### F5 — Cost of crossing the board

```
basics_to_cross = ceil(front_to_front / SPD)          = ceil(8 / SPD)
rounds          = basics_to_cross / 2                  (1 basic per champion per half)
economy_share   = basics_to_cross / (rounds × 4)       (team has 4 basics per round)
```

**Result:** at `SPD 2`, 4 basics over 2.0 rounds = **50%** of the team's basic economy. At
`SPD 3`, 3 basics over 1.5 rounds = **50%**. The share is invariant in `SPD` — speed buys
time, never budget. **The price of walking one champion across is always the other four
standing still.**

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | A champion is fully surrounded by other champions | It has no legal move. It may still basic-attack if anything is in reach, and may still use an ability. It simply cannot be one of the two movers. |
| 2 | A team has only one champion alive | ⚠ Deferred — open question 2. The "two different champions" rule cannot be satisfied. |
| 3 | A team wants to move a champion zero hexes to satisfy the compulsory rule | Illegal. A move is at least one hex (rule 3). |
| 4 | *Every* champion on a team is enclosed and nothing is in reach | The compulsory requirement lapses for whichever basics cannot be legally spent. The rule never makes a position illegal — it only spends what can be spent. |
| 5 | A champion takes a basic action, then uses an ability in the same half | Legal and expected. The economies are independent (rule 1). |
| 6 | An ability with a `Move` effect is played during the ladder phase | Legal. `Move`/`Displace` effects are **ability** effects and are not bound by the basic phase. Positions being "locked" means no *basic* movement, not that abilities cannot relocate anyone. |
| 7 | `Displace` pushes a champion into an occupied hex | Truncates at the last unoccupied hex along the path, per Champion & Ability Schema edge case 4. Consistent with rule 4 — bodies block forced movement too. |
| 8 | A basic attack kills its target before the ladder opens | Death resolves at round close per ADR-0006, as with any other damage. The champion is not removed mid-half. |
| 9 | A passive would trigger during the basic phase | It triggers there. Passives are not ladder steps and are not confined to the ladder phase. |
| 10 | Two passives would trigger simultaneously | ▸ Resolution order is owned by the **Ability Definition Schema**, which must define it deterministically (ADR-0002). |
| 11 | A champion moves onto a beacon's hex | Legal. Beacons do not occupy hexes (Sigils & Beacons rule 6). |
| 12 | A champion wants to move through a friendly champion | Illegal. All champions block, friendly included (rule 4). |
| 13 | The path to a destination exists but exceeds `SPD` while the straight line does not | The move is illegal. `SPD` limits path length, not distance (F1). |
| 14 | A basic attack targets a structure | ▸ Owned by Objectives & Scoring — see rule 5. |

## Dependencies

| System | Direction | What passes across |
|---|---|---|
| **Initiative Ladder** | → modifies | Closes its open question 3. Movement leaves the ladder entirely; the ability economy is unchanged. |
| **Map & Terrain** | ← consumes, → answers | Board geometry and front-to-front distance. Answers its open question 6: the terrain is the other players. |
| **Champion Data & Stat Model** | **bidirectional** | Consumes `SPD` and `RCH`. Requires `RCH` capped at 3 and a fifth ability slot for the passive. |
| **Ability Definition Schema** | → requires | Passive trigger vocabulary and deterministic trigger ordering. `Move`/`Displace` effects must respect blocking. |
| **Damage & Combat Resolution** | → requires | Basic attack damage. |
| **Objectives & Scoring** | → requires | Whether basic attacks may damage structures. |
| **Sigils & Beacons** | ↔ interacts | Locked positions remove the mid-ladder dodge that made denial B sharp. Beacons do not block. Passives carry no sigils. |
| **AI Opponent** | → requires | Must search the basic phase and the ladder phase separately; the basic phase alone is ~2 champions × ~19 destinations, before abilities. |

## Tuning Knobs

| Knob | Default | Safe range | Affects | Failure at the edges |
|---|---|---|---|---|
| `basics_per_half` | **2** | 1–3 | Positional breadth | At 1 the board freezes and formations never change; at 3 the team repositions in 0.8 rounds and position stops being scarce |
| `basics_compulsory` | **true** | — | Stalling | At false, a team ahead on points can freeze the board entirely |
| `distinct_champions_per_half` | **true** | — | Whether one champion can sprint | At false, one champion crosses the board in 1.0 round instead of 2.0 and breadth stops being the constraint |
| `friendlies_block` | ⚠ **true** | — | Cost of clustering | At false, formations have no self-cost and the huddle tax is the only thing pricing clustering |
| `min_move_hexes` | **1** | — | Whether compulsory has teeth | At 0 the compulsory rule is decorative |
| `SPD` range | 1–4 | — | Crossing time | Owned by Champion Data; note F5 — `SPD` buys time but never economy share |
| `RCH` range | ⚠ **1–3** | — | Whether position confers safety | At 4 a centred champion threatens the whole board (F4) |

### Knobs that interact

- **`basics_per_half` × team size.** Two basics against five champions is what makes
  breadth scarce. If either moves, the ratio — and the whole positional game — moves with
  it. The meaningful figure is **basics per champion per round**, currently 0.8.
- **`friendlies_block` × `basics_per_half`.** Self-blocking costs detour, and detour is
  paid in basics. At `basics_per_half` 1 a self-inflicted traffic jam could cost several
  rounds to untangle, which is likely past frustrating.
- **`RCH` × `basics_per_half`.** Reach is the substitute for mobility. If breadth is
  scarce, high-reach champions matter more, because they need to be repositioned less.

## Acceptance Criteria

### Rules

1. **GIVEN** a half, **WHEN** the basic phase resolves, **THEN** exactly two basic actions
   were taken by each team, by two distinct champions (F3).
2. **GIVEN** the ladder phase in progress, **WHEN** any basic action is attempted, **THEN**
   it is illegal — positions are locked (rule 2).
3. **GIVEN** a champion that has taken a basic action, **WHEN** it uses an ability in the
   same half, **THEN** it is legal (rule 1).
4. **GIVEN** a destination whose straight-line distance is within `SPD` but whose shortest
   unoccupied path exceeds it, **WHEN** the move is attempted, **THEN** it is illegal (F1).
5. **GIVEN** a champion adjacent only to friendly champions, **WHEN** its legal moves are
   enumerated, **THEN** the set is empty (rule 4).
6. **GIVEN** a target behind three other champions and within `RCH`, **WHEN** it is
   targeted, **THEN** it is legal — no line of sight is required (rule 7).
7. **GIVEN** a team with an enemy in reach, **WHEN** it satisfies the compulsory rule,
   **THEN** it may do so with basic attacks and need not move (rule 2).
8. **GIVEN** an ability with a `Move` effect played during the ladder phase, **WHEN** it
   resolves, **THEN** the champion relocates (edge case 6).

### Geometry

9. **GIVEN** five champions across rank 0, **WHEN** a path from `(0,−3)` to `(0,3)` is
   computed, **THEN** it is 9 hexes against a straight-line 6, and **no hex of the board is
   unreachable** (rule 4).
10. **GIVEN** any `RCH` value in range, **WHEN** its threat set is enumerated from the board
    centre, **THEN** it covers strictly less than the whole board (F4, open question 1).

### Balance — for the harness

11. **GIVEN** simulated matches, **WHEN** basic actions are classified, **THEN** both moves
    and basic attacks occur in meaningful numbers. ⚠ If attacks exceed **80%** of basics,
    the board has stopped moving and `basics_compulsory` is doing nothing.
12. **GIVEN** simulated matches, **WHEN** halves are classified, **THEN** the ladder is
    opened in a **large majority**. ⚠ Below **70%**, abilities have become optional and
    the game's central system is being declined — the risk this two-economy split creates.
13. **GIVEN** simulated matches, **WHEN** tier-4 abilities are examined, **THEN** their
    hit rate is at or above the 31% static-position figure the applicability harness
    measured. Locked positions should make that number *achievable* rather than optimistic.

### Cross-system

14. **GIVEN** the AI, **WHEN** it searches a full half including both phases, **THEN** it
    stays within the 1.5 s decision budget.
15. **GIVEN** identical board state and identical inputs, **WHEN** a half resolves twice,
    **THEN** the outcomes are byte-identical, including path selection (ADR-0002). Shortest
    paths are frequently non-unique, so **the tie-break must be defined**, not incidental.

## Open Questions

| # | Question | Why it matters | Owner | By when |
|---|---|---|---|---|
| 1 | **`RCH` must cap at 3, and the schema says 1–4.** A reach of 4 threatens the entire board from the centre | Position stops conferring safety, which is most of what positioning is for. Requires a Champion Data & Stat Model amendment | Champion Data & Stat Model | Before the schema is unparked |
| 2 | **What happens when a team has one champion alive?** The "two distinct champions" rule is unsatisfiable | Rare but reachable, especially during the ace windows the comeback design depends on | Design | Before Death & Respawn is authored |
| 3 | **Can basic attacks damage structures?** | If yes, a team can grind objectives without ever opening the ladder, which is the stalling risk in criterion 12 wearing a different hat | Objectives & Scoring | Before Objectives GDD is approved |
| 4 | **Should friendly champions block?** Currently yes | Self-blocking is the most likely rule here to feel bad. It prices clustering, but a self-inflicted traffic jam costs basics to untangle and may simply read as clumsiness | Design + playtest | Vertical Slice |
| 5 | **Is the basic-alternation order right?** ⚠ Assumed opener-first, alternating | The team committing first gives information away. That is consistent with opening the ladder, but it means the opener pays twice in the same half | Initiative Ladder | Before ladder prototype round 4 |
| 6 | **What is the shortest-path tie-break?** Paths of equal length are common on a hex grid | Determinism requires it be defined rather than incidental (criterion 15). Likely the canonical direction order in ADR-0005, but that order carries a warning about truncation | Hex Grid + Simulation Core | Before implementation |
