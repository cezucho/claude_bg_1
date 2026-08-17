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
off the playable board entirely. There are five towers, three nexus hexes per team, two jungle
flanks, one champion per hex, and no movement corridors.

Three decisions carry the design. First, **movement corridors were tried and
rejected**: a lane running along a hexagon's outer ring is one hex wide at every board
radius, so two champions sharing it are permanently in single file — corridors create
the formation problem they exist to represent. Champions therefore move freely, and the
two lanes on this board are *minion routes*, not walls. Second, **spawn hexes are dedicated per champion and
sit off-board**, which makes simultaneous respawn structurally impossible to break.
That is not an edge case: the initiative ladder batches every death to round close, so
whole-team wipes and three-champion trades resolve through the same code path as a
single kill. Third, **the board is a points race with an escape hatch**: kills and
towers score, most matches end when a team reaches the target, and destroying the enemy
nexus ends the match outright regardless of the score — the comeback route.

```
       S S S S S S      S  spawn row   off-board, 6 hexes per team
        F N N N F       N  nexus       middle 3 of the front line
       . = · · = .      F  lane mouth  the other 2 front hexes
      . · T · T · .     T  tower       5: two per team, one neutral centre
     . . · = = · . .    =  minion lane 2 lanes of 9, crossing at centre
    . . · · T · · . .   .  jungle      20 hexes (33% of play), two flanks
     . . · = = · . .    ·  open ground
      . · T · T · .
       . = · · = .      61 playable hexes · 6.1 per champion
        F N N N F       front-to-front 8 · towers 2 from own front
       S S S S S S      centre tower 4 from each front
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

### 3. No movement corridors — but two minion lanes

**No hex constrains champion movement.** The playable board is open ground plus jungle,
and a champion may walk anywhere.

Corridors were tried and rejected on geometry. A lane joining the two teams along the
hexagon's outer ring is one hex wide — at radius 4, and still at radius 6. Two champions
assigned to it are permanently in single file, one behind the other, so a corridor
actively prevents the side-by-side pair formation it exists to represent. Any two
adjacent open hexes are a pair formation instead, asserted as an invariant: **every
walkable hex has at least one walkable neighbour**.

**Two lanes nevertheless exist as geometry**, because minion waves need routes even
though champions do not. They are the only two straight front-to-front lines on a
radius-4 hexagon:

| Lane | From | Direction | Hexes | Structures on it |
|---|---|---|---|---|
| 1 | `(0,−4)` | `(0,+1)` | 9 | `(0,−2)` A tower · `(0,0)` centre · `(0,2)` B tower |
| 2 | `(4,−4)` | `(−1,+1)` | 9 | `(2,−2)` A tower · `(0,0)` centre · `(−2,2)` B tower |

They cross at the centre tower, and between them they carry **all five** towers. This was
not arranged: the towers were placed for symmetry and equidistance and landed on the
board's two natural axes. A square map affords three lanes; a hexagon affords exactly
two, which is also a better fit for five champions — **two per lane plus a jungler**,
so every lane is a pair.

Because no champion is ever obliged to use a lane, lanes carry none of the single-file
cost that corridors do. ▸ Minion waves themselves are owned by **Objectives & Scoring**
and deferred to Vertical Slice; this document fixes only where the routes run.

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

The front line is not merely a starting edge — it is **where a team's nexus stands**
(rule 7). Its five hexes divide into the **middle three, which are the nexus**, and the
**two outer hexes, which are the lane mouths** where the minion routes begin. ▸ Where
champions actually stand when tactical combat starts is owned by the **Opening Phase**,
which moves them forward before the first round.

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

**Towers are captured, never destroyed.** Ownership flips, and a flipped tower can be
flipped back. This is deliberate: a reversible objective keeps the match live, which is
the main brake available against the snowball risk that a lengthening respawn timer and
a points race would otherwise compound. Only the nexus is permanent (rule 7).

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

### 7. The nexus, and how structures are attacked

Each team's **nexus** is the middle three hexes of its front line:

| Team | Nexus hexes |
|---|---|
| A | `(1,−4)` `(2,−4)` `(3,−4)` |
| B | `(−1,4)` `(−2,4)` `(−3,4)` |

**The nexus is destroyed, not captured** — the only irreversible objective in the game.
Reaching it means standing on the enemy's own front rank, which is the deepest commitment
the board allows and leaves your own nexus eight hexes behind you.

**The nexus awards no points. It is a pure terminator: destroying it ends the match,
whatever the score.** There are therefore exactly two ways a match ends:

| Ending | How | Expected frequency |
|---|---|---|
| **Target score** | A team reaches the point target from kills and tower control | ⚠ The large majority |
| **Nexus destroyed** | A team breaks through and destroys the enemy nexus | ⚠ Rare, and usually dramatic |

**Being ahead on points is not safe, and that is the point.** A team at 90% of the target
still loses if it is aced and its nexus falls. This is the strongest anti-snowball
mechanism in the design — stronger than reversible towers — because it does not slow a
leading team down, it just refuses to let them stop playing.

**The comeback is a design goal, not a leak.** A team far behind on points that lands an
ace, capitalises efficiently, and burns the nexus down inside the respawn window *should*
win. That requires the nexus to be destructible within roughly one ace window — see the
calibration constraint below — and it is the reason a leading team's correct play is to
stop overextending rather than to keep pressing.

**Calibration constraint.** ⚠ Nexus durability should be set so that a full team,
unopposed, needs **most but not all** of one ace window to destroy it. Too durable and
the comeback never happens and the nexus is decoration; too fragile and every ace ends
the match, which makes the points race irrelevant. ▸ The actual HP number and the ace
window length are owned by **Objectives & Scoring** and **Death & Respawn**; this
document fixes only that the two must be sized against each other.

Note how this meshes with defender scaling: after an ace there is nobody adjacent to
anything, so the nexus takes damage at full rate. The window when the nexus is genuinely
attackable and the window when the enemy is dead are the same window, without either rule
being written to produce that.

**Termination is guaranteed without a round limit.** Towers tick score continuously, so
some team's total always rises and the target is always approached. A match cannot stall
indefinitely, which means no round cap is needed and turtling has a clock: a team playing
for one ace must land it *before* the enemy reaches the target.

**Defenders reduce structure damage but never stop it.** Damage dealt to a tower or
nexus is scaled down for each enemy champion adjacent to it, and the scaling never
reaches zero. The alternative — full immunity while defended — was considered and
rejected because a single parked champion could then veto a siege indefinitely and the
match could stall with neither side able to close. A siege under defence is slow, not
impossible, so pressure always converts eventually and the defenders' real job is to buy
the rounds their team needs.

This is also what makes a won fight convert. After an ace there is nobody adjacent to
anything, so structures fall at full rate for as long as the respawn timers hold — which
is exactly the capitalisation a decisive exchange should earn.

▸ The damage scaling curve, the nexus HP pool, tower capture rules and the scoring rate
are all owned by **Objectives & Scoring**. ▸ Structure damage magnitude is owned by
**Damage & Combat Resolution**. This document fixes only *where* the structures are,
*which* are reversible, and that defence scales rather than vetoes.

### 8. Jungle

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
        else Nexus   if h ∈ {(1,−4), (2,−4), (3,−4), (−1,4), (−2,4), (−3,4)}
        else Tower   if h ∈ {(0,0), (0,−2), (2,−2), (0,2), (−2,2)}
        else Front   if |rank(h)| = 4          (the two lane mouths)
        else Lane    if h ∈ Lane1 ∪ Lane2
        else Jungle  if |file(h)| ≥ 5
        else Open
```

**Output:** one of seven zones. Counts: 12 spawn, 6 nexus, 5 tower, 4 lane mouth, 12
remaining lane, 20 jungle, 14 open — 61 playable plus 12 off-board.
**Example:** `(3,−3)` is rank −3, file 3 → open ground. `(4,−2)` is rank −2, file 6 →
jungle. `(2,−4)` is rank −4 with file 0 → nexus. `(0,−4)` is rank −4 with file −4 → a
lane mouth, not nexus.

Lane membership is a *route* marker, not terrain: it changes nothing about movement,
targeting or cover. It exists so minion waves have somewhere to walk (rule 3).

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
| 14 | A champion stands on its own nexus hex | Permitted, and it is the strongest defensive position in the game — adjacency to all three nexus hexes reduces incoming structure damage (rule 7) |
| 15 | A tower is attacked with defenders adjacent on both sides | Damage is reduced by the scaling, never to zero, so the siege progresses. ▸ Whether reduction stacks per defender or caps is owned by Objectives & Scoring |
| 16 | The last nexus hex is destroyed | The match ends immediately at that point in the ladder, without waiting for round close. This is the one exception to round-close resolution, and it must be, because a destroyed nexus cannot be undone by a later action in the same round |
| 17 | Both teams' nexuses would be destroyed in the same round | Cannot arise. Ladder resolution is strictly sequential (ADR-0006), never simultaneous, and the match ends the instant the first nexus falls |
| 18 | A team reaches the target score in the same round its own nexus is destroyed | Whichever resolves first in the ladder wins. Score from kills accrues at round close, so a nexus destroyed mid-round beats a target reached at close — the comeback wins the tie by construction |
| 19 | A tower is captured while an enemy champion stands on it | ▸ Owned by Objectives & Scoring. This document notes only that towers are reversible and the nexus is not |
| 20 | Two champions attempt to move into the same hex in one resolution | Cannot arise. The initiative ladder resolves one action at a time, so the second mover sees the first already placed (ADR-0006). Noted here so it is not re-solved elsewhere |

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
| `nexus_size` | 3 hexes | 2–5 | At 5 the whole front line is nexus and the lane mouths vanish | At 2 a single tier-4 pattern can cover the entire nexus, so one lucky alignment ends the match |
| `lane_count` | 2 | 1–2 | Fixed by geometry — only two straight front-to-front lines exist on a hexagon, so 3 is unavailable without bent routes | At 1 the map has a single axis and the jungler's flanking role collapses |
| `tower_count` | 5 | 3–7 | At 7 the score ticks from too many sources and holding ground beats fighting | At 3 the map has a single flashpoint |
| `tower_rank` | ±2 | ±1 – ±3 | At ±3 a team's towers sit near the centre and are hard to defend, so leads snowball | At ±1 towers hug the front and are nearly uncontestable; the centre becomes the only real objective |
| `tower_file_spread` | ±2 | ±2 – ±4 | Above ±4 the two towers sit in jungle and stop being contestable in the open | At 0 both towers occupy the centre axis and a team can cover both from one position, so the team never splits |

> ▸ The defender damage-reduction curve is a knob, but it belongs to **Objectives &
> Scoring** along with nexus HP and capture rules. It is named here only because rule 7
> fixes its *shape* — scaling, never immunity.

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
- **`nexus_size` × tier-4 pattern size.** A tier-4 fixed pattern covers 4–6 hexes. If the
  nexus is smaller than that, a single well-aligned tier-4 ability covers all of it at
  once, and the game's most decisive moment becomes a geometry lottery. Three hexes is
  the smallest nexus that a 5-hex pattern cannot fully blanket while the attacker also
  stands clear of the front rank.

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

### Lanes and structures

18. **GIVEN** the board, **WHEN** straight front-to-front lines are enumerated, **THEN**
    exactly two exist, each 9 hexes long, and they intersect only at `(0,0)` (rule 3).
19. **GIVEN** the five towers, **WHEN** lane membership is checked, **THEN** all five lie
    on a lane — every tower is reachable by a minion route.
20. **GIVEN** each team's nexus, **WHEN** its hexes are counted, **THEN** there are
    exactly 3, they are the middle of the front line, and they are antipodal to the
    enemy's (rule 7).
21. **GIVEN** a nexus hex, **WHEN** lane membership is checked, **THEN** it is not a lane
    mouth — lanes begin at the two outer front-line hexes.
22. **GIVEN** a structure under attack with one or more adjacent enemy champions,
    **WHEN** damage resolves, **THEN** the amount applied is reduced but strictly greater
    than zero (rule 7). A siege must never be reducible to no progress.
23. **GIVEN** a structure with no adjacent enemy champion, **WHEN** damage resolves,
    **THEN** no reduction applies — an ace converts at full rate.
24. **GIVEN** simulated matches across the balance harness, **WHEN** their endings are
    classified, **THEN** the large majority end on target score and a **non-zero
    minority** end by nexus destruction (rule 7). Zero nexus endings means the nexus is
    decoration; a majority means the points race is irrelevant.
25. **GIVEN** the subset of matches ending by nexus destruction, **WHEN** the destroying
    team's score at that moment is examined, **THEN** *some* of them were behind. The
    comeback must be demonstrably reachable, not merely permitted by the rules.
26. **GIVEN** a full team attacking an undefended nexus, **WHEN** the rounds needed to
    destroy it are counted, **THEN** the count is at or just under the ace window, so
    that a comeback requires near-total capitalisation rather than a leisurely stroll.

### Cross-system

27. **GIVEN** the board and its tower positions, **WHEN**
    `tools/Augury.Tools applicability` is re-run using towers as the contested points,
    **THEN** the measured applicability table is regenerated and ladder F4's reference
    values are updated to match. **Blocking gate before any ability is authored.**
28. **GIVEN** an identical starting board, **WHEN** it is constructed twice, **THEN** the
    serialised hex ordering is byte-identical — construction must not depend on
    dictionary iteration order or any other unstable source.
29. **GIVEN** a tier-4 pattern owned by each team, **WHEN** both resolve from antipodal
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
| 7 | ~~**Is the front line the right starting position?**~~ **RESOLVED 2026-08-16.** The front line is now permanently load-bearing: its middle three hexes are the nexus and its outer two are the lane mouths. It is the thing you must reach to win, so it matters in every round of the match rather than only the first | — | — | Closed. See rule 7 |
| 8 | ~~**Does the match end on a round limit, a target score, or both?**~~ **RESOLVED 2026-08-17.** Target score is the primary ending; nexus destruction is a second, rare one. No round limit is needed — towers tick continuously, so the target is always approached and no match can stall | — | — | Closed. See rule 7 |
| 12 | **What stops a deliberate turtle-and-rush strategy?** A team could concede the points race, refuse fights, and play solely for one ace into a nexus rush. It is self-limiting — the enemy is scoring the whole time — but "self-limiting" is not the same as "not the best strategy" | If turtling is optimal, the points race stops mattering and every match becomes one all-in fight | Objectives & Scoring + Balance Harness | Before Objectives GDD is approved |
| 9 | **Do towers have a destroyed state as well as a captured one?** Capture is reversible and ticks score; destruction would be a lump sum that removes the tower from play. Both were raised as possible point sources | Two states per tower is more design surface but gives a held tower somewhere to go — a reason to keep pressing after capture | Objectives & Scoring | Before Objectives GDD is approved |
| 10 | **Do minion waves belong in the game at all?** Deferred to Vertical Slice. The lanes exist as geometry, but nothing walks them yet, and towers are capturable without them | Waves supply *tempo* — windows when a tower is takeable — which nothing else currently provides. If MVP play feels rhythmless, this is the first thing to add | Objectives & Scoring | Vertical Slice |
| 11 | **Can a siege actually finish under defence?** Reduction scales rather than vetoes, so progress is guaranteed — but if the curve is too steep, "slow" becomes "never" in practice within a 16-round match | The match-length target depends on structures actually falling | Objectives & Scoring + Balance Harness | Before Objectives GDD is approved |
