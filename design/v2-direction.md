# AUGURY v2 — Direction After the First Playtests

> **Date**: 2026-10-04 · **Status**: Agreed direction, not yet a GDD
> **Source**: the project owner's playtests of the browser build and the discussion that
> followed. Decisions marked **Owner** were made by the owner; **Proposed** are Claude's and
> open to change. This document precedes GDD revisions; each item names the GDD it will
> change.

## What playtesting showed

| Area | Owner's verdict |
|---|---|
| Draft | Works, but 10 champions with two per role leave no variety: the second pick of each role is forced |
| Opening | **Feels right.** Planning paths and end positions is the part that works |
| Combat | **Too many choices.** 20 abilities overwhelm a new player and stay hard to remember on the fifth match; it will get worse with a bigger roster |
| Champions | Too similar: mostly damage in different shapes |
| Board | Feels one dimension too large: the front-to-back depth |
| Victory points | Anticlimactic on a screen. On a table, moving the marker is physical; here a counter ticks |

The owner's framing: with a large board and many options, the game drifts toward chess,
where playing well means memorising openings and positions. **That is not wanted.**

## Decisions

### Victory — the nexus HP race · Owner · *Objectives & Scoring, Map & Terrain*

Points become damage to the enemy nexus. Each tower a team holds at round close fires at
the enemy nexus; each kill damages the victim's nexus; direct attacks on an open nexus
still work. **Every match ends with a nexus being destroyed.** Match length is set by
nexus HP.

### Towers are impassable · Owner · *Map & Terrain*

No champion may enter a tower hex by any means. Towers become obstacles and choke points,
and must be attacked from beside them.

### Champion kit: three abilities plus a summoner slot · Owner · *Champion & Ability Schema, Opening Phase, Draft*

- Each champion has **three abilities** and a **fourth slot** filled by a summoner spell.
- **Opening instructions stay with the champion**: four sets, one per slot, the fourth
  belonging to the champion and not to the spell. A spell never carries opening
  instructions. *Owner's reason: if the spell supplied instructions, the opening could be
  corrected after the draft; the draft should be where the opening is planned, and a
  draft that doesn't fit should cost work later.*
- **Summoner spells** come from one shared pool, the same for every champion, so they are
  learned once. They are chosen **after the draft**, one per champion.
- **Cooldowns are long relative to match length.** The exact number waits until match
  length is measured.
- **Duplicates within a team: probably not allowed.** Owner leans no, not strictly.

### Champions differ by verb, not shape · Owner · *Champion & Ability Schema*

Distinctness comes from **status and control effects** — poison, burn, immobilise, swap
places, barriers and similar — not from pattern shapes. Damage patterns may stay
rotatable. *Test (Proposed): each champion can be described in one sentence, and its
three abilities are three different verbs.*

### Match length · Owner

**About 15 minutes for an advanced player**, so a best-of-three fits in the time of one
League of Legends game. Fifteen minutes may hold more than six rounds; the round count is
to be measured, not assumed. A short match is acceptable; three rounds is probably too
few for a comeback.

### The feeling to aim for · Owner, 2026-10-04 · *not a requirement yet*

Mastery: each champion has a specific way to be played, and a player who uses it well —
plays the champion's part, cooperates with the champions it needs — gets visibly more out
of it than one who doesn't. Comebacks are welcome; they make stories and tension. Both
pull against a short match, and the owner chooses the short match. Keep the feeling in
mind when measuring: notice whether board, kit or rules changes produce it on their own,
or can with small tweaks. It is a direction, not a gate.

### Spells, verbs and opening attacks · Owner, 2026-10-04 · *Champion & Ability Schema, Opening Phase, Draft*

- **The status verbs are accepted** (root, burn, mark, wall, swap, pull ally, unstoppable,
  cleanse, exhaust). More will come later.
- **The eight-spell pool is accepted.**
- **Spell picking:** after the draft, both teams choose all five spells at once, hidden.
  They are revealed when the opening begins.
- **Attacks in the opening are being tried with every champion.** One ability per champion
  gets an opening version that casts it. Rules in `design/v2-champions.md`.

### Rejected · Owner

- **Unlocking abilities over rounds** (two at first, then three, then four): a player
  must plan with every ability from the start.

## Open, to be decided with measurements

| Question | Leaning | Evidence needed |
|---|---|---|
| Board depth | Shallower: about 7 rows instead of 9 (Proposed) | Self-play: rounds to first contact, pattern applicability, match length |
| Terrain types (hills, swamp…) | Yes, after the board size is settled; few keywords if champions get terrain affinities (Proposed) | A board-size decision first |
| Role-flexible champions in the draft | Possible draft depth without a large roster (Proposed) | Roster growth |
| Best-of-three series | Natural fit for 15-minute matches (Proposed) | Match length |

## Build order (Proposed, agreed in conversation)

1. **Nexus HP race and impassable towers.** Small rules change, large change in feel.
   *Done 2026-10-04:* the nexus race is live (D-038). Solid towers are implemented but
   shipped off until the champion rewrite (D-039): the placeholder openings route through
   tower hexes, and with towers solid 83% of team openings hit the fallback.
2. **Board-size experiment** — compare the current board with a shallower one in self-play.
   *Measured 2026-10-04:* three candidates against classic in `design/board-layouts.md`.
   Claude recommended Field 7 + base. **Owner chose Field 7 "for now"** (front lines 6 rows
   apart, nexus on the start line). The shipped board stays classic until the champion
   rewrite gives Field 7 openings that fit it; today's openings fall back 85% of the time there.
3. **Three abilities plus summoner spells** — schema change, then the real work: rewriting
   the roster around distinct verbs. Claude drafts a spell pool for the owner to react to.
   *Done 2026-10-04:* all ten champions, eight spells, hidden spell picking and opening casts
   are in the game. Field 7 and solid towers are now the shipped board (`rules_config.json`).
   Roster and measurements: `design/v2-champions.md`.
4. **Terrain**, once the board is settled.

### Synergy strength · Owner, 2026-10-08

Synergy should be **significant but not decisive**: medium-sized payoffs, leaning smaller than
larger, but not very small. With a large roster (say 50), players will learn the best pairs.
**Bans in the draft** then keep combos interesting. Implemented as ×1.5 with a floor of +2
(D-052).

### The opening must shape the endgame · Owner, 2026-10-11 · *brainstorm, not decided*

**The feeling the owner wants.** After the draft, a player picks a strategy — a win condition, as
in League of Legends. The opening is where they start fulfilling it. Between two good drafts, the
player with the better opening should find the endgame much easier, because their champions
stand where they can do more.

**Measured today.** An AI that plays its opening at random wins **48.5%** against the same AI
choosing its opening (400 matches, sides alternate). The opening makes no measurable
difference. Also approved the same day: making marks and roots as dependable as burn, and
letting synergy affect towers.

**Why the opening doesn't matter (Claude's diagnosis).**
1. **Nothing the opening builds persists.** Positions are reshuffled within a round or two:
   - every half both teams move twice, and dashes and pushes move champions more;
   - the board is small;
   - the opening's beacons break after two hits.
2. **Nothing accumulates from position.** In League, position early earns gold and experience,
   which become power later. Here the only things that carry forward are nexus HP and stat
   molding, and molding depends on which abilities you use, not where you stand.

**Owner's decisions (2026-10-11).**
- **Champion quests: go.** Each champion has one positional task, met at round close, earning
  stacks; at 2 and 4 stacks it gets a champion-specific upgrade, its power spike. After the
  draft, a player chooses whose quests to feed: that is the win condition. The opening starts
  them.
- **Terrain: go, combined with quests.** Hills and swamps on the board; some quests use them.
- **Lane farm: no.** It would feel unnatural.
- **Neutral objectives** (the Dragon or Baron feeling, the highlight of a match): wanted, but not
  copied from League. To be thought through later.
- **Targets.** A good opening against a random one should win **60–65%** between equal drafts.
  A good draft against a tragic one should win about **65/35**. *"A better player — who drafts
  well, who knows how to play the opening — should have an easier endgame."*

**Built and measured (2026-10-11).** Both targets are met:
- a good opening beats a random one **61%**;
- a great draft beats a tragic one **71.5%**.

The levers were:
- quests, with the opening's end counting as a quest check;
- terrain;
- one basic action per half instead of two;
- +8% POW per quest stack;
- openings that can reach quest spots;
- an AI that plans its whole opening.

Details in `design/v2-champions.md` → "Quests and terrain"; decisions D-058 to D-061.

### Momentum: picks that shift the match · Owner, 2026-10-18 · *brainstorm, measured, not decided*

**The feeling the owner wants.** In League, a weaker team ambushes the enemy's carry and the
whole match turns: they take the Baron, the lead flips. "One team solved the riddle quicker",
or "one team made the error and the other used it". Today AUGURY feels like attrition: champions
trade damage, kills are delayed, nothing shifts in a moment. The owner is torn: the growth from
quests feels right, but the swings are missing. Is it necessary?

**Measured (400 matches, shipped rules).**
- 8.5 deaths a match, and only **14% are bursts** (killed from 60%+ HP within one round).
- A carry (3+ stacks) dies **3.5 times a match**. Two rounds after killing one, the killer's lead
  has grown only 47% of the time.
- **When only one team kills a carry, that team wins 43%.** Killing the carry is a non-event, if
  not a mistake.
- The lead changes hands 0.75 times a match; 17% of rounds swing by 5 or more.

**Why (Claude's diagnosis).** Four rules smooth every swing: a kill is delayed to the round's
death check; respawn takes 1–2 rounds; a death costs 1 of 4 stacks; a kill is 3 of 60 nexus HP.
And nothing converts a pick: no objective becomes available when the enemy is a champion down.

**Levers tried, behind flags in `rules_config.json` (shipped off, D-062):**
- `respawnPerStack` — respawn rounds per stack the victim had (a carry's death is an event);
- `deathLosesHalfStacks` — a death costs half the stacks, rounded up (the spike is lost);
- `isolationBonus` — ×1.5 damage on a champion with no ally within 1 ("caught alone");
- `deathCheckEachHalf` — a death check at the end of half 1 too (a burst kills at once).

| | Burst kills | Carry deaths/match | Team that alone killed a carry wins |
|---|---|---|---|
| Shipped | 14% | 3.5 | **43%** |
| All four levers | 17–20% | 2.5–2.9 | **59–70%** |

Comebacks stay at 20–24%; rounds 12.0–12.4. The AI neither hunts carries nor protects them, so
self-play understates the feeling; the levers change what a pick is *worth*, which is what the
numbers show.

**Owner's decision (2026-10-20): switch the four levers on (D-063), and build the shrine.**
Measured with them on: good opening 61%, great draft 72%, a team that alone kills a carry wins
60–66%, comebacks 22–24%, 12.3 rounds.

### The shrine — a drafted rule (to react to, not built)

*The thing a pick is converted into: the Baron moment without copying it.*

- **Where:** one neutral **shrine** on the river row, on the hill at its east end on odd
  rounds and the west end on even rounds? No — simpler: **two shrines**, one on each river hill
  (the hexes at (−4,0) and (4,0)). Mirror-symmetric, so fair.
- **Awakening:** a shrine is dormant until **round 4**, then awake until taken; it reawakens
  **4 rounds** after it was last taken. A countdown shows on the board.
- **Taking it:** at round close, a team takes an awake shrine if one of its champions stands
  **on** it and **no enemy champion is adjacent**. (Standing on a hill: +1 reach, so the
  holder is also a sniper's nest.)
- **The prize, lasting two rounds:** every champion of the taking team gains **+1 quest stack**
  (upgrades fire), and the team's **siege is doubled**. Both are visible: pips light up, towers
  glow.
- **Why it makes picks matter:** a shrine can only be taken with no enemy adjacent, so a team
  that is a champion down can't contest both the shrine and its towers. The sequence "catch
  the carry alone → take the shrine → towers pound the nexus" is three rounds of visible
  momentum.
- **Comeback guard:** a shrine's stacks are quest stacks, so dying costs them like any other.
- **Open:** one shrine or two; the prize (stacks vs siege vs both); whether the AI needs a
  shrine term in its evaluation (yes: value standing on an awake shrine ≈ a quest met).

### Marvel Snap, and quicker kills · Owner, 2026-10-20 · *an idea, not a decision*

**The owner's idea.** Kills should be quicker; a good combo could wipe three champions at once;
rounds could be much faster. Marvel Snap, a card game, gives the feeling of building combos and
positioning characters at a fast pace, and the draft already gives AUGURY the "build a deck for
one game" pleasure without the deck-building chore. Could Snap's feeling and pace live in this
hexagonal world, making the game quicker and smoother, with momentum shifts easier to reach?

**What makes Snap feel the way it does (Claude's reading).**
1. **Few turns, each a big simultaneous reveal.** Six turns; both players commit in secret,
   then everything resolves at once. The last turns decide the match.
2. **Escalating resources.** Energy 1 → 6, so turn 6 plays are three times the size of turn 2.
   Combos are *inside* a turn: this card, then that card, in order.
3. **Lethality.** Power swings of 10+ against locations held by 5; a single reveal can flip two
   locations.
4. **Readable positions.** Three locations, each a visible contest.

AUGURY today has 4 (hexes, nothing hidden) and half of 2 (quests escalate power, but a turn is
still one ability). It lacks 1 entirely — the ladder alternates single actions, which is what
makes it feel like trading blows — and 3: hits of 4–8 against 25–35 HP.

**Lethality alone, measured** (`abilityBase` 3→5, `basicBase` 2→3, 120 matches): bursts rose to
29% of kills, deaths to 10.4 a match, rounds fell to 11.2 — but carries stopped existing (1.3
carry deaths a match, because everyone dies before 3 stacks) and a carry kill paid nothing
(50%). Everyone dying is not momentum; it's noise. Lethality needs structure around it.

**Three ways to bring the Snap feeling in, smallest first.**

- **A. Bigger hits, fewer of them (a dial).** Raise damage but also raise the value of a pick
  (the shipped levers), so kills are fast *and* meaningful. Cheap; probably not enough on its
  own, see above.
- **B. Combos that grow with the round.** In round *r*, a team's ladder action may be a
  **chain of up to min(r, 3) abilities** (today: one, or two with a shared sigil). Round 1 is
  pokes; round 4 onward, a team can fire three abilities as one unanswerable sequence: root,
  mark, execute — the three-champion wipe the owner imagines. The ladder, ceiling and Last
  Word stay. The opponent sees the chain coming (initiative is public) and can answer *after*
  it, which keeps the "ambush vs error" story. Medium change; measurable in the harness.
- **C. Every round is an opening.** The owner's favourite phase, generalised. Each round, both
  teams **secretly program** one play per champion (move, cast, or hold — the opening's
  instruction language already exists), then **everything reveals and resolves in initiative
  order**. The ladder goes away. Rounds become fast (one commit, one reveal), combos are
  planned sequences, and the "they read our plan" and "we solved it first" feelings are built
  in. It is the Snap turn in hex form. Big change: it replaces the ladder (ADR-0004, a pillar),
  and it adds hidden information within a round — Pillar 1 ("nothing hidden during play")
  would bend the way it already bent for spell picks. The AI would need a new planner (the
  opening planner is a start).

**Claude's opinion.** The owner's complaint at the very start was that the game "drifts toward
chess"; the alternating ladder is the chess in it. The opening — scripted, simultaneous in
spirit, geometric — is what the owner keeps saying works. C is the honest conclusion of that,
and it would deliver pace and swings together. But it is a rewrite of the core phase, so the
right path is: **B first** (keeps everything, gets the wipe), measured against the momentum
metrics; then a **prototype of C** on a branch, played at the table, before any decision.

### Programmed rounds · Owner, 2026-10-21 · *brainstorm, see `design/v3-programmed-rounds.md`*

The owner's idea: each round both teams secretly program one ability per champion, then a
descending-initiative ladder resolves them, with the starting team choosing where the ceiling
opens and higher programs discarded. Three specialists (core loop, pace, Neuroshima Hex)
analysed it; the synthesis, the disagreements and a streamlined prototype rule set are in the
v3 document. Nothing built; v2 is untouched by it.

## Idea parking lot

### Bans in the draft (owner, 2026-10-08)

Once the roster is large enough that the best combinations are common knowledge. Not
scheduled; it needs a roster well past 15.


Ideas the owner raised but has not committed to. Recorded so they are not lost; none is
scheduled.

### Overwatch (owner, 2026-10-04)

*From XCOM and similar turn-based games: hold back part of your turn, and act in the
opponent's turn when a condition is met.*

- **The idea.** A champion can go on Overwatch instead of acting. If a condition is met
  during the opponent's turn — an enemy comes into view or reach, or the champion is
  attacked — it acts for free.
- **Where it might fit.** A fallback **if three abilities per champion still prove too
  many**: one of the three becomes an Overwatch ability, a fixed action much like a
  passive (a fixed-hex attack or a fixed move), which you arm instead of acting.
- **Notes for later (Claude).**
  - It fits the existing machinery. Passives already fire on conditions such as
    `OnEnemyEntersReach`, and the ladder already has a give-up-your-turn decision in Pass /
    Last Word. Arming Overwatch would be a third way to spend a champion's action on the
    ladder.
  - It is deterministic and readable if the armed champion and its trigger are shown on
    the board (Pillar 1: nothing hidden).
  - The risk is reaction chains. An Overwatch shot that triggers another Overwatch needs a
    depth limit; passives use depth 1.
  - It could also help the failing pass-rate criterion (Sigils #14): holding something back
    becomes a real option.

### Attacks in the opening (owner, 2026-10-04) — *promoted: now being tried, see `design/v2-champions.md`*

*Some champions' opening plays include using the ability itself, not only movement.*

- **The idea.** The opening still takes five plays, one per champion. Some abilities'
  opening versions include their **combat effect** as well as movement. A team might spend
  four plays shaping its formation and the fifth on an attack — or two or three attacks
  in a row — before the opponent can answer on the ladder.
- **The trade.** An ability used this way **goes on cooldown**, so that team starts the
  first ladder with fewer tools, but its opponent may start with less HP.
- **Notes for later (Claude).**
  - It reverses D-011 ("the opening ability does not start on cooldown") for the abilities
    that attack. Movement-only openings could stay free.
  - It is not quite unanswered. The teams still alternate opening plays, so the opponent
    replies with its own next play — move away, shield, or strike back. Who places first
    (D-013) matters more: the second placer sees the formation and can punish it.
  - It only works on a shallow board. On Field 7, teams stand about 1.5 hexes apart
    after the opening; on the classic board an opening attack would rarely reach anything.
  - The risk is a decisive alpha strike before the match really starts, which works against
    comebacks. Possible limits: opening attacks cannot kill, deal reduced damage, or only one
    per team.
  - It gives the draft another axis — champions whose openings attack, against champions
    that shape the formation — which fits the owner's wish that the draft and the opening
    be planned together.
  - The engine supports it: the opening already resolves ability-linked instructions, and
    previews would show the exact damage before you commit.
