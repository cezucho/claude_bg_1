# Map & Terrain

> **Status**: Designed (pending review)
> **System**: #12 Map & Terrain — first in the Core layer design order
> **Depends on**: ADR-0005 (hex coordinates and patterns)
> **Blocks**: Movement & Targeting · Champion Data & Stat Model · Ability Definition
> Schema · Objectives & Scoring · Jungle & Neutral Powers · Opening Phase
>
> **Reading the markers.** ⚠ marks a value or claim that is assumed rather than
> derived. ▸ marks something this document deliberately does **not** decide, naming
> the system that owns it. Both exist so the document can be scanned for soft spots
> instead of read cover to cover.

## Overview

The board is a hexagon of 61 hexes, radius 4, with two bases at opposite corners, five
towers, and two jungle wedges flanking the diagonal between the bases. It is
symmetric under 180° rotation, one champion occupies one hex, and there are no lanes.
That last point is the design's central claim: routes were tried and rejected, because
on a hexagon the outer edge is a single ring, so any edge-hugging lane is one hex wide
at every board radius — which forces two champions sharing it into single file. The
open board removes corridors entirely, so a pair of champions can stand abreast
anywhere, and the formation problem that motivated the whole layout question stops
existing rather than being worked around.

```
        . . · · B          B  base        (4,-4) and (-4,4)
       . . T · · ·         T  tower       (0,0), (3,-1), (1,-3), (-3,1), (-1,3)
      . . · · · · ·        .  jungle      |S| >= 3, 22 hexes in two wedges
     . . · · · · T .       ·  open ground 32 hexes
    . . · · T · · . .
     . T · · · · . .       61 hexes · 6.1 per champion · 36% jungle
      · · · · · . .        base-to-base 8 hexes · towers 3 from own base
       · · · T . .         centre tower 4 from each base
        B · · . .
```

## Player Fantasy

The board should read the way a chessboard reads: one glance, complete information, no
hidden state and nothing to expand or inspect. Five champions per side on 61 hexes is
sparse enough that every piece is individually visible and dense enough that they are
always in reach of one another — you are looking at a fight, not at two clusters
separated by travel.

The feeling to protect is *the board offering you something*. Because tier-4 abilities
are fixed, non-rotatable patterns, most of the time the geometry simply does not line
up, and then in one round it does, and you see it before your opponent does. A board
without corridors is what makes that possible: every hex has six neighbours and the
space is open, so position is something you shape rather than something a corridor
hands you.

The jungle is the exception, and it belongs to one champion. It should feel like a set
of doors only the jungler holds keys to — the rest of the team walks around, the
jungler walks through, and arrives somewhere nobody was watching.

## Detailed Rules

### 1. Shape and coordinates

The board is a regular hexagon of **radius 4** — every hex with `|Q| ≤ 4`, `|R| ≤ 4`
and `|S| ≤ 4`, where `S = −Q − R` — giving **61 hexes**. Axial coordinates, flat-top
layout, exactly as ADR-0005 specifies. The origin `(0,0)` is the board centre.

At 5v5, that is **6.1 hexes per champion**. ⚠ This density is the single number every
other system inherits: it sets how often ability patterns find a target, which is what
prices ability power through the initiative ladder's F4. It was chosen by judgement,
not derived, and it is the first thing to revisit if combat feels wrong.

### 2. One champion per hex

A hex holds at most one champion. Stacking was considered and rejected for three
reasons, in order of weight:

1. **It would dissolve the rigidity tiers.** Tier-4 abilities are priced at roughly 30%
   applicability precisely because a fixed pattern rarely lines up on the champions you
   want. If champions stack, a pattern needs to catch one hex instead of three *and*
   each hex caught is worth two or three times the damage. Both dials move the same way
   at once, and tier 4 stops being an opportunity the board grants.
2. **It costs legibility**, which Pillar 1 requires. A stack must be expanded before it
   can be read; a chessboard never must be.
3. **It taxes every action.** Single-target abilities would need two-step targeting —
   pick hex, then pick champion — on every use, under a blitz clock.

### 3. No lanes

There are no lane corridors. The board is open ground plus jungle.

The reason is geometric rather than aesthetic. Lanes on a hexagon naturally follow the
outer edge, and the outer edge is a single ring of hexes — so an edge lane is one hex
wide at radius 4, and still one hex wide at radius 6. Two champions assigned to it are
permanently in single file, one behind the other. Since the whole point of a bottom-lane
pair is that the two champions stand *beside* each other, lanes actively prevent the
formation they exist to represent.

Without lanes, any two adjacent open hexes are a pair formation. This is asserted as an
invariant: **every walkable hex has at least one walkable neighbour**, so champions can
always stand abreast.

### 4. Symmetry is rotational, and must be

The board maps onto itself under **180° rotation about the origin**: `(Q,R) → (−Q,−R)`.
Every hex has the same zone as its antipode, so the two teams play identical geometry.

**Mirror symmetry is forbidden, and this is not a preference.** Tier-4 patterns are
fixed hex offsets that the player cannot rotate; the engine expresses pattern variants
only as the six rotations of ADR-0005. A mirrored pattern is a *chirally* different
shape — the relationship between a left and a right hand — and no rotation produces it.
Under a mirrored board, the two copies of one champion in a mirror match would have
abilities of genuinely different shape, and the game would not be fair. Rotation
preserves chirality; mirroring does not.

> **Owed to ADR-0005.** That ADR describes tier-4 patterns as fixed in "absolute board
> orientation". On a rotationally symmetric board that makes a north-facing pattern
> good for the team attacking north and bad for the team attacking south. Patterns must
> instead be fixed relative to **the owning team's forward direction** — still
> non-rotatable by the player, which preserves the entire rigidity design, but oriented
> consistently per team. This is an amendment ADR-0005 needs before any tier-4 ability
> is authored.

### 5. Bases

Two bases, at the opposite corners `(4,−4)` and `(−4,4)`, **8 hexes apart**. A base is
the team's respawn point: a champion killed at round close returns to its own base
corner when its respawn timer expires.

Respawning at the corner rather than at the fight is what gives death a positional cost
on top of a temporal one — the walk back is real, and it lengthens as the fight moves
toward the enemy's half. ▸ The respawn *timer*, including its lengthening over the
match, is owned by **Death, Dying Round & Respawn**. ▸ How many rounds the walk costs
is owned by **Movement & Targeting**.

A base has no combat function. It does not shoot, score, or block; it is a location.

### 6. Towers

Five towers. Two start held by each team, one at the centre starts neutral:

| Tower | Hexes | Distance from own base | Starts held by |
|---|---|---|---|
| Team A inner | `(3,−1)`, `(1,−3)` | 3 | Team A |
| Team B inner | `(−3,1)`, `(−1,3)` | 3 | Team B |
| Centre | `(0,0)` | 4 from both | Neutral |

Each tower does two things:

- **It threatens.** A tower damages enemy champions within range of it.
- **It scores.** A tower generates points for whichever team holds it, continuously.

Together these make a tower a place you must stand to profit and would rather not stand.
▸ Tower damage magnitude and range are owned by **Damage & Combat Resolution**;
▸ scoring rate, capture rules, and the match-winning threshold are owned by
**Objectives & Scoring**. This document fixes only where towers are and what
categories of thing they do.

Two properties are fixed here because they are geometric: **no tower sits inside
jungle**, so every tower is contestable in the open by any champion; and each team's
two towers are equidistant from its own base, so neither is the easier one to defend.

### 7. Jungle

The jungle is every hex with `|S| ≥ 3` — **22 hexes, 36% of the board**, forming two
wedges either side of the base-to-base diagonal.

Jungle terrain has one confirmed property: **a jungler moves through it faster than
through open ground, and faster than any other champion moves through it.** The jungle
is a private road, not an obstacle. This is what gives the jungler map control — the
ability to arrive somewhere the enemy was not watching, one round before they could
have.

▸ The size of the speed advantage, and how "jungler" is identified — a role tag on the
champion, an item, a chosen assignment — are owned by **Movement & Targeting** and
**Draft** respectively. ▸ Whether the jungle also holds neutral objectives, grants
buffs, or blocks line of sight is **explicitly undecided** and owned by **Jungle &
Neutral Powers**. This document deliberately does not invent them.

⚠ The 36% figure is a consequence of the `|S| ≥ 3` rule rather than a target. It is
worth checking against play: too much jungle and the open board shrinks toward
corridors, which is the thing rule 3 exists to prevent.

## Formulas

### F1 — Board Membership

`in_board(h) = max(|h.Q|, |h.R|, |h.S|) ≤ radius`, where `h.S = −h.Q − h.R`

| Variable | Type | Range | Description |
|---|---|---|---|
| `radius` | int | 3–6 | Board radius in hexes. **4** for this board |
| `h.Q`, `h.R` | int | −4–4 | Axial coordinates (ADR-0005) |

**Output:** boolean. **Example:** `(4,−4)` has `S = 0`, max is 4 → on the board, and it
is base A. `(4,1)` has `S = −5`, max is 5 → off the board.

### F2 — Hex Count

`hexes(radius) = 3 × radius² + 3 × radius + 1`

**Output:** 37 at radius 3, **61 at radius 4**, 91 at radius 5, 127 at radius 6.
**Example:** `3(16) + 3(4) + 1 = 61`. Per champion at 5v5: `61 ÷ 10 = 6.1`.

### F3 — Team Symmetry Map

`antipode(h) = (−h.Q, −h.R)`

**Invariant:** `zone(h) = zone(antipode(h))` for all 61 hexes, and
`antipode(antipode(h)) = h`.

**Example:** base A `(4,−4)` → base B `(−4,4)`. Team A's tower `(3,−1)` → team B's
tower `(−3,1)`. Note `S → −S` under this map, which is why every zone rule in this
document is written on `|S|` rather than `S` — a rule on signed `S` could not be
symmetric.

### F4 — Zone Predicate

`zone(h) = Base if h ∈ {(4,−4), (−4,4)}`
`         else Tower if h ∈ {(0,0), (3,−1), (1,−3), (−3,1), (−1,3)}`
`         else Jungle if |h.S| ≥ 3`
`         else Open`

**Output:** one of four zones. Counts: 2 base, 5 tower, 22 jungle, 32 open = 61.
**Example:** `(2,−4)` has `S = 2`, so it is open ground. `(1,−4)` has `S = 3`, so it is
jungle — the wedges begin one hex further out than the towers.

### F5 — Distance

`distance(a, b) = (|a.Q − b.Q| + |a.R − b.R| + |a.S − b.S|) ÷ 2`

**Output range:** 0–8 on this board. **Example:** base to base is 8; base to own tower
is 3; base to centre tower is 4. ▸ Converting distance into rounds is owned by
**Movement & Targeting**; this document supplies only the hex counts.

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | A champion's respawn hex — its own base — is occupied when the timer expires | It spawns on the nearest unoccupied hex to the base, breaking ties by ADR-0005's canonical direction order. Never by team index, and never by truncating the direction list (control manifest) |
| 2 | Every hex adjacent to a base is occupied, and the base itself is occupied | Respawn is deferred one round. A team cannot lock its own respawn permanently because at most 10 champions exist and the base has 6 neighbours |
| 3 | A `Displace` effect would push a champion off the board | Truncated at the last legal hex. There is no ring-out; the board edge is a wall, not a hazard |
| 4 | A `Displace` would push a champion into an occupied hex | Truncated at the last unoccupied hex along the path. If that is the origin, no movement occurs |
| 5 | An ability pattern extends past the board edge | Off-board hexes are silently dropped from the target set, and the ability remains legal if any on-board hex remains. A tier-4 ability standing at the rim is genuinely weaker, and that is intended — the rim is a bad place to stand |
| 6 | A tier-4 pattern's every hex falls off the board | The ability has no legal target and is excluded by the ladder's F1. It is not an error; board edges are a legitimate reason for a rigid ability to be dead this round |
| 7 | A champion stands on a tower hex | Permitted. Towers are terrain, not blockers — a tower must be stood on to be contested. ▸ Whether standing on it is *required* to hold it is owned by Objectives & Scoring |
| 8 | A non-jungler champion enters the jungle | Permitted, at normal movement cost. The jungle is not restricted terrain; it is merely faster for one champion |
| 9 | A champion dies while standing in jungle | No special handling. Death and respawn are unaffected by terrain |
| 10 | Two champions attempt to move into the same hex in one resolution | Cannot arise. The initiative ladder resolves one action at a time, so the second mover sees the first already placed. This is a consequence of ADR-0006's sequencing, and is noted here so it is not re-solved |

## Dependencies

| System | Direction | What crosses the boundary |
|---|---|---|
| **Hex Grid (ADR-0005)** | this ← that | Coordinates, distance, rotation, pattern offsets. **This document owes ADR-0005 an amendment**: tier-4 patterns must be team-relative, not world-absolute (rule 4) |
| **Movement & Targeting** | **bidirectional** | This supplies zones, distances and the jungle terrain flag; that supplies movement cost per zone and the jungler's speed advantage |
| **Champion Data & Stat Model** | this → that | Board density (6.1 hexes per champion) sets the reach and speed scales, which is why that GDD was parked until this one existed |
| **Ability Definition Schema** | this → that | Board size determines measured pattern applicability, which prices ability power through ladder F4 |
| **Objectives & Scoring** | this → that | Tower positions and count; scoring rate and win threshold belong there |
| **Damage & Combat Resolution** | this → that | Tower threat exists; its magnitude and range belong there |
| **Death, Dying Round & Respawn** | this → that | Base hexes are respawn points; timers belong there |
| **Jungle & Neutral Powers** | this → that | Jungle extent and the speed property; everything else about the jungle is deliberately unspecified |
| **Draft** | this ← that | Which champion counts as the jungler |
| **Opening Phase** | this → that | Champions are placed into position on this board before tactical combat begins, which is what lets the board be open without a long approach |
| **Board & Unit Presentation** | this → that | 61 hexes, four zone types, five towers to render legibly at a glance |
| **AI Opponent** | this → that | 61 hexes bounds the movement branching factor — six neighbours per champion, capped by board membership |

## Tuning Knobs

| Knob | Default | Safe range | Too high | Too low |
|---|---|---|---|---|
| `board_radius` | 4 | 3–6 | At 6 the board is 127 hexes and 12.7 per champion; melee applicability falls to 35% and the walk-back after death costs most of a match | At 3 the board is 37 hexes; everything is in range of everything and positioning stops mattering |
| `jungle_depth` (`|S| ≥ n`) | 3 | 3–4 | At 4 the jungle shrinks to 10 hexes and is too thin to rotate through | At 2 the jungle is 40 hexes, 66% of the board, and the open ground becomes corridors — the thing rule 3 exists to prevent |
| `tower_count` | 5 | 3–7 | At 7 the score ticks from too many sources and holding ground beats fighting | At 3 there is one objective per side plus the centre, and the map has a single flashpoint |
| `tower_distance_from_base` | 3 | 2–4 | At 4 a team's towers sit near the centre and are hard to defend, so leads snowball | At 2 towers hug the base and are nearly uncontestable, so the centre becomes the only real objective |
| `base_separation` | 8 | 6–8 | Fixed by the board radius — bases are at opposite corners, so this is `2 × radius` and not independently tunable | — |

### Knobs that interact

- **`board_radius` × ladder `M(i)` × `applicability`** are one equation. Board size sets
  champion density, density sets how often patterns find targets, and that is exactly
  the `applicability(i)` term the ladder's F4 uses to price ability power. Changing the
  radius invalidates the measured applicability table and requires re-running
  `tools/Augury.Tools applicability`. This coupling is why this GDD was moved ahead of
  the ability schema.
- **`board_radius` × respawn timer × match length.** Base separation is `2 × radius`, so
  a larger board silently lengthens every death penalty. At radius 4 the walk-back is
  8 hexes; at radius 6 it is 12, in a match of roughly the same number of rounds.
- **`jungle_depth` × `tower_distance_from_base`.** Towers must stay out of the jungle
  (rule 6). At `jungle_depth = 2` the current tower sites `(3,−1)` and `(1,−3)` fall
  inside it, and the layout breaks.

## Acceptance Criteria

### Geometry

1. **GIVEN** radius 4, **WHEN** the board is built, **THEN** it contains exactly 61
   hexes (F2).
2. **GIVEN** any board hex, **WHEN** its antipode `(−Q,−R)` is computed, **THEN** that
   hex is also on the board and has the identical zone (F3). Zero exceptions across all
   61 hexes.
3. **GIVEN** the zone assignment, **WHEN** hexes are tallied, **THEN** the counts are
   2 base, 5 tower, 22 jungle, 32 open (F4).
4. **GIVEN** each team's two towers, **WHEN** distance to that team's own base is
   measured, **THEN** both equal 3, and both teams' figures match (F5).
5. **GIVEN** the centre tower, **WHEN** distance to each base is measured, **THEN**
   both equal 4.
6. **GIVEN** all five towers, **WHEN** their zones are checked, **THEN** none lies in
   jungle — every tower is contestable in the open.
7. **GIVEN** every walkable hex, **WHEN** its six neighbours are examined, **THEN** at
   least one is also walkable, so two champions can always stand abreast (rule 3).

### Occupancy and movement

8. **GIVEN** a hex containing a champion, **WHEN** another champion attempts to enter,
   **THEN** the move is illegal — one champion per hex, with no exception for allies.
9. **GIVEN** a champion respawning onto an occupied base, **WHEN** placement resolves,
   **THEN** it takes the nearest free hex chosen by ADR-0005's canonical direction
   order, and the choice is identical on every replay (edge case 1).
10. **GIVEN** a `Displace` toward the board edge, **WHEN** it resolves, **THEN** the
    champion stops at the last on-board hex and is never removed from play (edge case 3).

### Cross-system

11. **GIVEN** the board and the tower positions, **WHEN**
    `tools/Augury.Tools applicability` is re-run with towers as the contested points,
    **THEN** the measured applicability table is regenerated and the ladder's F4
    reference values are updated to match. Blocking gate before any ability is authored.
12. **GIVEN** an identical starting board, **WHEN** it is constructed twice, **THEN**
    the serialised hex ordering is byte-identical — board construction must not depend
    on dictionary iteration order or any other unstable source.
13. **GIVEN** a tier-4 pattern owned by each team, **WHEN** both are resolved from
    mirror-image positions, **THEN** they cover antipodal hex sets — the team-relative
    orientation amendment (rule 4) is in effect and neither team has a shape the other
    cannot express.

## Open Questions

| # | Question | Why it matters | Owner | Resolve by |
|---|---|---|---|---|
| 1 | **Is 6.1 hexes per champion right?** Chosen by judgement. It is the number every other system inherits, and the applicability table is a direct function of it | Too dense and positioning stops mattering; too sparse and melee becomes unplayable and walk-backs eat the match | Design + Balance Harness | Vertical Slice playtest |
| 2 | **Does the jungler's speed advantage need terrain, or only a rule?** The jungle is currently 36% of the board defined purely to give one champion a road | If the advantage can be expressed without dedicating a third of the board to it, the open ground could grow | Design | With Movement & Targeting |
| 3 | **What else is the jungle for?** Deliberately unanswered — neutral objectives, buffs, line-of-sight blocking are all open | A jungle that is only a speed lane may not justify a dedicated role | Jungle & Neutral Powers | Vertical Slice |
| 4 | **Is respawning at the corner too harsh late?** Base separation is 8 hexes and respawn timers lengthen as the match runs, so the two penalties compound | Snowball risk is already the concept's fourth-highest listed risk | Design | With Death, Dying Round & Respawn |
| 5 | **Do five towers produce a points race or a stalemate?** Two defended towers each plus a contested centre could equally settle into neither side attacking | The match-length target of 10–15 minutes depends on the score actually moving | Objectives & Scoring | Before Objectives GDD is approved |
| 6 | **Should the board have any impassable terrain at all?** Currently every hex is walkable. Walls would create the corridors rule 3 rejects, but might make tier-4 patterns more setup-able | Interacts directly with `Displace` as the tier-4 release valve | Design | Vertical Slice |
