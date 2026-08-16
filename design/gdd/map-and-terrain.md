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

The board is a hexagon of 61 playable hexes, radius 4, with the two teams facing each
other across opposite **edges** rather than from opposite corners. Each edge is exactly
five hexes — one per champion — and behind each sits a six-hex **spawn row** that is
off the playable board entirely. There are five towers, two jungle flanks, one champion
per hex, and no lanes.

Two decisions carry the design. First, **routes were tried and rejected**: a lane
running along a hexagon's outer ring is one hex wide at every board radius, so two
champions sharing it are permanently in single file — lanes create the formation
problem they exist to represent. Second, **spawn hexes are dedicated per champion and
sit off-board**, which makes simultaneous respawn structurally impossible to break.
That is not an edge case: the initiative ladder batches every death to round close, so
whole-team wipes and three-champion trades resolve through the same code path as a
single kill.

```
       S S S S S S      S  spawn row   off-board, 6 hexes per team
        F F F F F       F  front line  5 hexes per team — one per champion
       . · · · · .      T  tower       5: two per team, one neutral centre
      . · T · T · .     .  jungle      20 hexes (33% of play), two flanks
     . . · · · · . .    ·  open ground 26 hexes
    . . · · T · · . .
     . . · · · · . .    61 playable hexes · 6.1 per champion
      . · T · T · .     front-to-front 8 hexes · towers 2 from own front
       . · · · · .      centre tower 4 from each front
        F F F F F
       S S S S S S
```

## Player Fantasy

The board should read the way a chessboard reads: one glance, complete information,
nothing hidden and nothing to expand. Five champions per side across 61 hexes is sparse
enough that every piece is individually visible and dense enough that they are always in
reach of one another — you are looking at a fight, not at two clusters separated by
travel.

The feeling to protect is *the board offering you something*. Because tier-4 abilities
are fixed, non-rotatable patterns, most of the time the geometry simply does not line
up — and then in one round it does, and you see it before your opponent does. An open
board is what makes that possible: every hex has six neighbours, so position is
something you shape rather than something a corridor hands you.

The jungle is the exception, and it belongs to one champion. It should feel like a set
of doors only the jungler holds keys to: the rest of the team walks around, the jungler
walks through, and arrives somewhere nobody was watching.

Death should feel like being sent to the back of the room. You watch the fight from the
spawn row, untouchable and useless, and the walk back is long enough to regret.

## Detailed Rules

### 1. Shape and coordinates

The playable board is a regular hexagon of **radius 4** — every hex with `|Q| ≤ 4`,
`|R| ≤ 4` and `|S| ≤ 4`, where `S = −Q − R` — giving **61 hexes**. Axial coordinates,
flat-top layout, exactly as ADR-0005 specifies. The origin `(0,0)` is the board centre.

Two derived readings are used throughout, because raw axial coordinates do not express
"toward the enemy" or "across the board":

| Reading | Definition | Range | Meaning |
|---|---|---|---|
| **rank** | `R` | −4 … +4 | Toward the enemy front. `R = −4` is team A's front line, `R = +4` is team B's |
| **file** | `Q − S` | −8 … +8 | Across the board. `0` on the axis joining the two front-line midpoints, `±8` at the side corners |

Both negate under the symmetry map (rule 4), which is why every zone rule in this
document is written on an **absolute value**. A rule on signed rank or file could not be
symmetric, and the board would not be fair.

At 5v5 this is **6.1 playable hexes per champion**. ⚠ This density is the single number
every other system inherits: it sets how often ability patterns find a target, which is
what prices ability power through the initiative ladder's F4. It was chosen by
judgement, not derived, and it is the first thing to revisit if combat feels wrong.

### 2. One champion per hex

A hex holds at most one champion. Stacking was considered and rejected for three
reasons, in order of weight:

1. **It would dissolve the rigidity tiers.** Tier-4 abilities are priced at roughly 30%
   applicability precisely because a fixed pattern rarely lines up on the champions you
   want. If champions stack, a pattern needs to catch one hex instead of three *and*
   each hex it catches is worth two or three times the damage. Both dials move the same
   way at once, and tier 4 stops being an opportunity the board grants.
2. **It costs legibility**, which Pillar 1 requires. A stack must be expanded before it
   can be read; a chessboard never must be.
3. **It taxes every action.** Single-target abilities would need two-step targeting —
   pick hex, then pick champion — on every use, under a blitz clock.

### 3. No lanes

There are no lane corridors. The playable board is open ground plus jungle.

The reason is geometric rather than aesthetic. A lane joining the two teams would run
along the hexagon's outer ring, and that ring is one hex wide — at radius 4, and still
at radius 6. Two champions assigned to such a lane are permanently in single file, one
behind the other. Since the whole point of a bottom-lane pair is that the two champions
stand *beside* one another, lanes actively prevent the formation they exist to
represent.

The five-hex front line is **not** a lane. It is a starting edge, five wide, that opens
directly onto open ground.

Without corridors, any two adjacent open hexes are a pair formation. This is asserted as
an invariant: **every walkable hex has at least one walkable neighbour**.

### 4. Symmetry is rotational, and must be

The board — spawn rows included — maps onto itself under **180° rotation about the
origin**: `(Q,R) → (−Q,−R)`. Every hex has the same zone as its antipode, so the two
teams play identical geometry.

**Mirror symmetry is forbidden, and this is not a preference.** Tier-4 patterns are
fixed hex offsets that the player cannot rotate; the engine expresses pattern variants
only as the six rotations of ADR-0005. A mirrored pattern is a *chirally* different
shape — the relationship between a left hand and a right — and no rotation produces it.
Under a mirrored board, the two copies of one champion in a mirror match would have
abilities of genuinely different shape, and the game would not be fair. Rotation
preserves chirality; mirroring does not.

> **Owed to ADR-0005.** That ADR describes tier-4 patterns as fixed in "absolute board
> orientation". On a rotationally symmetric board that makes a pattern reaching toward
> `R+` good for team A and bad for team B. Patterns must instead be fixed relative to
> **the owning team's forward rank** — still non-rotatable by the player, which
> preserves the entire rigidity design, but oriented consistently per team. This
> amendment is required before any tier-4 ability is authored.

### 5. Front lines and spawn rows

Each team's **front line** is the five playable hexes on its edge of the board:

| Team | Front line | Rank |
|---|---|---|
| A | `(0,−4) (1,−4) (2,−4) (3,−4) (4,−4)` | −4 |
| B | `(0,4) (−1,4) (−2,4) (−3,4) (−4,4)` | +4 |

Front lines are ordinary playable hexes with no special properties. They are where the
board begins, not a zone. ▸ Where champions actually stand when tactical combat starts
is owned by the **Opening Phase**, which moves them off the front line before the first
round.

Behind each front line sits a **spawn row of six hexes, off the playable board**:

| Team | Spawn row |
|---|---|
| A | `(0,−5) (1,−5) (2,−5) (3,−5) (4,−5) (5,−5)` |
| B | `(0,5) (−1,5) (−2,5) (−3,5) (−4,5) (−5,5)` |

Four rules govern them:

**(a) Every champion owns a designated spawn hex.** Assignment is fixed at draft and
never changes during a match. Five champions, six hexes: **the jungler owns two** and
chooses which to use on each respawn, picking a flank to return through. ▸ Which
champion counts as the jungler is owned by **Draft**.

**(b) Spawn hexes are not playable.** They are outside the game. A champion there
cannot be targeted, damaged, healed, displaced, or affected by any status; no ability
pattern extends into a spawn row; nothing is scored there. Spawn camping is impossible
by construction rather than by balance.

**(c) Entering play costs the champion's action.** Stepping from a spawn hex onto an
adjacent front-line hex consumes that champion's action for the half, exactly as
movement does. A respawning champion therefore rejoins the fight one action later than
its timer alone suggests. ⚠ Whether this is the right cost is a feel question that only
play answers — it is a tuning knob, and the structure supports making entry free
without any change to the board.

**(d) Dedicated hexes make simultaneous respawn a non-problem.** Because assignment is
per champion, five champions dying in the same round return to five distinct hexes. No
collision is possible, no tie-break rule is needed, and no fallback placement logic
exists to go wrong. **This is the reason spawn rows exist.** The initiative ladder's
death check runs once at round close and kills every champion at ≤0 HP together
(ADR-0006), so multi-death rounds are the normal shape of a good exchange, not a rare
ace — and a single shared respawn point would have been broken in ordinary play, not
just in extremis.

▸ The respawn *timer*, including its lengthening across the match, is owned by
**Death, Dying Round & Respawn**. ▸ How many rounds the walk back costs is owned by
**Movement & Targeting**.

### 6. Towers

Five towers. Two start held by each team, one at the centre starts neutral:

| Tower | Hexes | Rank | Distance from own front | Starts held by |
|---|---|---|---|---|
| Team A inner | `(0,−2)`, `(2,−2)` | −2 | 2 | Team A |
| Team B inner | `(0,2)`, `(−2,2)` | +2 | 2 | Team B |
| Centre | `(0,0)` | 0 | 4 from both | Neutral |

Each tower does two things:

- **It threatens.** A tower damages enemy champions within range of it.
- **It scores.** A tower generates points for whichever team holds it, continuously.

Together these make a tower a place you must stand to profit and would rather not stand.
▸ Tower damage magnitude and range are owned by **Damage & Combat Resolution**;
▸ scoring rate, capture rules, and the match-winning threshold are owned by
**Objectives & Scoring**. This document fixes only where towers are and what categories
of thing they do.

Three properties are fixed here because they are geometric: **no tower sits in jungle**,
so every tower is contestable in the open by any champion; **each team's two towers are
equidistant from its own front line**, so neither is the easier one to defend; and the
**centre tower is equidistant from both fronts**, so neither team reaches it first.

The two inner towers of a team sit on files −2 and +2, straddling the centre axis. A
team cannot cover both from one position, which is what forces a five-champion team to
split rather than move as a single mass.

### 7. Jungle

The jungle is every playable hex with **`|file| ≥ 5`** — **20 hexes, 33% of play**,
forming two flanks either side of the centre axis. No front-line hex falls inside it, so
neither team begins the match in jungle.

Jungle terrain has one confirmed property: **a jungler moves through it faster than
through open ground, and faster than any other champion moves through it.** The jungle
is a private road, not an obstacle. This is what gives the jungler map control — the
ability to arrive somewhere the enemy was not watching, one round before they could
have — without minions, farming, or a gold economy.

▸ The size of the speed advantage is owned by **Movement & Targeting**. ▸ Whether the
jungle also holds neutral objectives, grants buffs, or blocks line of sight is
**explicitly undecided** and owned by **Jungle & Neutral Powers**. This document
deliberately does not invent them.

⚠ The threshold of 5 was selected as the largest jungle that keeps every front-line hex
and every tower clear of it: at `|file| ≥ 4` the jungle swallows four front-line hexes,
and at `≥ 6` it shrinks to 20% and the flanks become too thin to rotate through.

## Formulas

### F1 — Board Membership

`playable(h) = max(|h.Q|, |h.R|, |h.S|) ≤ radius`, where `h.S = −h.Q − h.R`
`spawn(h) = h ∈ SpawnRowA ∪ SpawnRowB` (12 hexes, never playable)

| Variable | Type | Range | Description |
|---|---|---|---|
| `radius` | int | 3–6 | Board radius. **4** for this board |
| `h.Q`, `h.R` | int | −5–5 | Axial coordinates; `±5` occurs only in spawn rows |

**Output:** boolean. **Example:** `(2,−4)` has `S = 2`, max 4 → playable, a front-line
hex. `(2,−5)` has `S = 3`, max 5 → not playable, and it is team A's spawn hex.

### F2 — Hex Count

`playable(radius) = 3 × radius² + 3 × radius + 1`

**Output:** 37 at radius 3, **61 at radius 4**, 91 at radius 5, 127 at radius 6.
**Example:** `3(16) + 3(4) + 1 = 61`. Per champion at 5v5: `61 ÷ 10 = 6.1`. Spawn hexes
are excluded from this count by design — they add no playable space and therefore do not
change density or measured applicability.

### F3 — Rank and File

`rank(h) = h.R`  ·  `file(h) = h.Q − h.S = 2 × h.Q + h.R`

| Reading | Range | Zero at |
|---|---|---|
| rank | −4 … +4 | Board centre line |
| file | −8 … +8 | The axis joining the two front-line midpoints |

**Example:** `(2,−4)` is rank −4, file 0 — the middle of team A's front line. `(4,0)` is
rank 0, file 8 — the deepest point of the right-hand jungle. Note `file` has the same
parity as `rank`, so adjacent hexes in a row differ by 2 in file.

### F4 — Team Symmetry Map

`antipode(h) = (−h.Q, −h.R)`

**Invariant:** `zone(h) = zone(antipode(h))` across all 61 playable and 12 spawn hexes,
and `antipode(antipode(h)) = h`.

**Example:** team A's front-line hex `(0,−4)` → team B's `(0,4)`. Team A's tower
`(2,−2)` → team B's `(−2,2)`. Under this map `rank → −rank` and `file → −file`, which is
why rules 6 and 7 are written on `|rank|` and `|file|`.

### F5 — Zone Predicate

```
zone(h) = Spawn   if h ∈ SpawnRowA ∪ SpawnRowB
        else Tower   if h ∈ {(0,0), (0,−2), (2,−2), (0,2), (−2,2)}
        else Front   if |rank(h)| = 4
        else Jungle  if |file(h)| ≥ 5
        else Open
```

**Output:** one of five zones. Counts: 12 spawn, 5 tower, 10 front, 20 jungle, 26
open — 61 playable plus 12 off-board.
**Example:** `(3,−3)` is rank −3, file 3 → open ground. `(4,−2)` is rank −2, file 6 →
jungle. `(3,−4)` is rank −4 → front line, and its file of 2 keeps it clear of jungle.

### F6 — Distance

`distance(a, b) = (|a.Q − b.Q| + |a.R − b.R| + |a.S − b.S|) ÷ 2`

**Output range:** 0–8 across playable hexes. **Example:** front line to front line is 8;
a tower is 2 from its own front; the centre tower is 4 from each front; a spawn hex is 1
from the front-line hex it opens onto. ▸ Converting distance into rounds is owned by
**Movement & Targeting**; this document supplies only hex counts.

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | An entire team of five dies in the same round | Each returns to its own designated spawn hex. No collision is possible because assignment is per champion, so this resolves through exactly the same path as a single death (rule 5d) |
| 2 | A champion's spawn hex is occupied when its timer expires | Cannot occur. Only that champion is ever assigned that hex, and a champion cannot be dead and standing in the spawn row simultaneously |
| 3 | The jungler dies and both of its spawn hexes are free | The player chooses at the moment of respawn. This is a real decision — it selects which flank to return through — and it is the only spawn choice in the game |
| 4 | A champion is left in the spawn row and does not enter play | Legal. It simply does not participate that half. There is no forced entry, and no timer pressure beyond losing the champion's action value |
| 5 | An ability pattern would extend into a spawn row | Those hexes are silently dropped from the target set. Spawn rows are not merely empty, they are outside the game (rule 5b) |
| 6 | A `Displace` effect would push a champion into a spawn row | Truncated at the last playable hex. No champion is ever pushed out of play |
| 7 | A `Displace` would push a champion off the board edge | Truncated at the last playable hex. There is no ring-out; the edge is a wall, not a hazard |
| 8 | A `Displace` would push a champion into an occupied hex | Truncated at the last unoccupied hex along the path. If that is the origin, no movement occurs and the ability still resolves |
| 9 | An ability pattern extends past the board edge | Off-board hexes are dropped, and the ability remains legal if any playable hex remains. A tier-4 ability standing at the rim is genuinely weaker, and that is intended — the rim is a bad place to stand |
| 10 | A tier-4 pattern's every hex falls outside the board | The ability has no legal target and is excluded by the ladder's F1. Not an error; board edges are a legitimate reason for a rigid ability to be dead this round |
| 11 | A champion stands on a tower hex | Permitted. Towers are terrain, not blockers — a tower must be stood on to be contested. ▸ Whether standing on it is *required* to hold it is owned by Objectives & Scoring |
| 12 | A non-jungler enters the jungle | Permitted, at normal movement cost. The jungle is not restricted terrain; it is merely faster for one champion |
| 13 | A champion dies in jungle | No special handling. Death and respawn are unaffected by terrain |
| 14 | Two champions attempt to move into the same hex in one resolution | Cannot arise. The initiative ladder resolves one action at a time, so the second mover sees the first already placed (ADR-0006). Noted here so it is not re-solved elsewhere |

## Dependencies

| System | Direction | What crosses the boundary |
|---|---|---|
| **Hex Grid (ADR-0005)** | this ← that | Coordinates, distance, rotation, pattern offsets. **This document owes ADR-0005 an amendment**: tier-4 patterns must be team-relative, not world-absolute (rule 4) |
| **Movement & Targeting** | **bidirectional** | This supplies zones, rank/file, distances and the jungle terrain flag; that supplies movement cost per zone, the jungler's speed advantage, and the cost of entering from a spawn hex |
| **Death, Dying Round & Respawn** | **bidirectional** | This supplies dedicated spawn hexes and the guarantee that simultaneous respawn cannot collide; that supplies respawn timers and their lengthening |
| **Champion Data & Stat Model** | this → that | Board density (6.1 hexes per champion) sets the reach and speed scales, which is why that GDD was parked until this one existed |
| **Ability Definition Schema** | this → that | Board size determines measured pattern applicability, which prices ability power through ladder F4 |
| **Objectives & Scoring** | this → that | Tower positions and count; scoring rate and win threshold belong there |
| **Damage & Combat Resolution** | this → that | Tower threat exists; its magnitude and range belong there |
| **Jungle & Neutral Powers** | this → that | Jungle extent and the speed property; everything else about the jungle is deliberately unspecified |
| **Draft** | this ← that | Which champion is the jungler, and therefore which owns two spawn hexes |
| **Opening Phase** | this → that | Front lines are where the board begins; that system decides where champions actually stand when tactical combat starts |
| **Initiative Ladder** | this ← that | Death batching at round close is what makes dedicated spawn hexes necessary rather than merely tidy |
| **Board & Unit Presentation** | this → that | 61 playable hexes, five zone types, twelve off-board hexes that must read as outside the game |
| **AI Opponent** | this → that | 61 hexes bounds the movement branching factor; spawn entry adds one action per dead champion |

## Tuning Knobs

| Knob | Default | Safe range | Too high | Too low |
|---|---|---|---|---|
| `board_radius` | 4 | 3–6 | At 6 the board is 127 hexes and 12.7 per champion; melee applicability falls to 35% and the walk back after death eats most of a match | At 3 the board is 37 hexes, the front line shrinks below five, and one champion per hex stops fitting |
| `jungle_file_threshold` | 5 | 5–6 | At 6 the jungle is 12 hexes and the flanks are too thin to rotate through | At 4 the jungle swallows four front-line hexes and both teams start inside it |
| `spawn_row_size` | 6 | 5–7 | Above 6 the extra hexes go unused unless more champions get a choice | At 5 the jungler loses its flank choice and every respawn is fully predetermined |
| `spawn_entry_cost` | 1 action | 0–1 action | At 1 action, respawning costs a full half of participation on top of the timer — the two penalties compound | At 0 death costs only the timer, and dying near the enemy front stops being punished |
| `tower_count` | 5 | 3–7 | At 7 the score ticks from too many sources and holding ground beats fighting | At 3 the map has a single flashpoint |
| `tower_rank` | ±2 | ±1 – ±3 | At ±3 a team's towers sit near the centre and are hard to defend, so leads snowball | At ±1 towers hug the front and are nearly uncontestable; the centre becomes the only real objective |
| `tower_file_spread` | ±2 | ±2 – ±4 | Above ±4 the two towers sit in jungle and stop being contestable in the open | At 0 both towers occupy the centre axis and a team can cover both from one position, so the team never splits |

### Knobs that interact

- **`board_radius` × ladder `M(i)` × `applicability`** are one equation. Board size sets
  champion density, density sets how often patterns find targets, and that is exactly the
  `applicability(i)` term ladder F4 uses to price ability power. Changing the radius
  invalidates the measured applicability table and requires re-running
  `tools/Augury.Tools applicability`. This coupling is why this GDD precedes the ability
  schema.
- **`board_radius` × front line size.** The front line is `radius + 1` hexes, so radius 4
  is the *smallest* board that seats five champions abreast. Radius 3 does not fit the
  team, which sets a hard floor the tuning range must respect.
- **`spawn_entry_cost` × respawn timer × match length.** Both are death penalties and
  they compound. Front-to-front is 8 hexes; adding an action cost on top of a lengthening
  timer risks the snowball listed as the concept's fourth-highest risk.
- **`jungle_file_threshold` × `tower_file_spread`.** Towers must stay out of jungle
  (rule 6). At threshold 4 the current tower files of ±2 are still clear, but any wider
  spread collides.

## Acceptance Criteria

### Geometry

1. **GIVEN** radius 4, **WHEN** the board is built, **THEN** it contains exactly 61
   playable hexes and 12 spawn hexes (F2).
2. **GIVEN** any playable or spawn hex, **WHEN** its antipode is computed, **THEN** that
   hex exists and has the identical zone (F4). Zero exceptions across all 73.
3. **GIVEN** the zone assignment, **WHEN** hexes are tallied, **THEN** the counts are
   12 spawn, 5 tower, 10 front, 20 jungle, 26 open (F5).
4. **GIVEN** each team's front line, **WHEN** its hexes are counted, **THEN** there are
   exactly 5 — one per champion.
5. **GIVEN** each team's two towers, **WHEN** distance to that team's nearest front-line
   hex is measured, **THEN** both equal 2, and both teams' figures match.
6. **GIVEN** the centre tower, **WHEN** distance to each front line is measured, **THEN**
   both equal 4.
7. **GIVEN** all five towers, **WHEN** their zones are checked, **THEN** none lies in
   jungle.
8. **GIVEN** every front-line hex, **WHEN** its zone is checked, **THEN** none lies in
   jungle — neither team starts the match in cover.
9. **GIVEN** every walkable hex, **WHEN** its six neighbours are examined, **THEN** at
   least one is also walkable, so two champions can always stand abreast (rule 3).
10. **GIVEN** every spawn hex, **WHEN** its neighbours are examined, **THEN** at least
    one is playable — no champion can be stranded off-board.
11. **GIVEN** every spawn hex, **WHEN** `playable()` is evaluated, **THEN** it returns
    false — spawn rows consume no playable space and do not change density.

### Spawn and respawn

12. **GIVEN** all five champions of one team dying in the same round, **WHEN** respawn
    resolves, **THEN** each occupies its own designated spawn hex, no placement conflict
    is raised, and no fallback placement path executes (rule 5d).
13. **GIVEN** a champion in a spawn hex, **WHEN** any enemy ability of any tier is
    evaluated for legality, **THEN** that champion is never in the target set (rule 5b).
14. **GIVEN** a champion in a spawn hex, **WHEN** the round-close status phase runs,
    **THEN** no status, damage, or healing is applied to it.
15. **GIVEN** a champion entering play from a spawn hex, **WHEN** the move resolves,
    **THEN** its action for that half is consumed (rule 5c).
16. **GIVEN** the jungler respawning, **WHEN** placement resolves, **THEN** the player is
    offered both of its spawn hexes and the choice is recorded in the event stream.
17. **GIVEN** a `Displace` directed at a champion standing on a front-line hex, **WHEN**
    it resolves, **THEN** the champion is never pushed into a spawn row (edge case 6).

### Cross-system

18. **GIVEN** the board and its tower positions, **WHEN**
    `tools/Augury.Tools applicability` is re-run using towers as the contested points,
    **THEN** the measured applicability table is regenerated and ladder F4's reference
    values are updated to match. **Blocking gate before any ability is authored.**
19. **GIVEN** an identical starting board, **WHEN** it is constructed twice, **THEN** the
    serialised hex ordering is byte-identical — construction must not depend on
    dictionary iteration order or any other unstable source.
20. **GIVEN** a tier-4 pattern owned by each team, **WHEN** both resolve from antipodal
    positions, **THEN** they cover antipodal hex sets — the team-relative orientation
    amendment (rule 4) is in effect and neither team has a shape the other cannot express.

## Open Questions

| # | Question | Why it matters | Owner | Resolve by |
|---|---|---|---|---|
| 1 | **Is 6.1 hexes per champion right?** Chosen by judgement. It is the number every other system inherits, and the applicability table is a direct function of it | Too dense and positioning stops mattering; too sparse and melee becomes unplayable and walk-backs eat the match | Design + Balance Harness | Vertical Slice playtest |
| 2 | **Should entering play from spawn cost an action?** Structured now so it can be changed to free without touching the board. Pure feel — analysis cannot settle it | Death penalty compounds with the lengthening respawn timer; snowball is a listed top risk | Design | Vertical Slice playtest |
| 3 | **Does the jungler's speed advantage need terrain, or only a rule?** A third of the playable board is currently dedicated to giving one champion a road | If the advantage can be expressed without the terrain, open ground could grow by 20 hexes | Design | With Movement & Targeting |
| 4 | **What else is the jungle for?** Deliberately unanswered — neutral objectives, buffs, line-of-sight blocking all open | A jungle that is only a speed lane may not justify a dedicated role | Jungle & Neutral Powers | Vertical Slice |
| 5 | **Do five towers produce a points race or a stalemate?** Two defended towers each plus a contested centre could settle into neither side attacking | The 10–15 minute match target depends on the score actually moving | Objectives & Scoring | Before Objectives GDD is approved |
| 6 | **Should the board have impassable terrain?** Every playable hex is currently walkable. Walls would create the corridors rule 3 rejects, but might make tier-4 patterns more setup-able | Interacts directly with `Displace` as the tier-4 release valve | Design | Vertical Slice |
| 7 | **Is the front line the right starting position?** The Opening Phase moves champions before round 1, so the front line may only ever be a respawn destination | If nothing ever happens on the front line, the board is effectively radius 3.5 | Opening Phase | Before Opening Phase GDD is approved |
