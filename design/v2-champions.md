# v2 Draft — Summoner Spells and Sample Champions

> **Date**: 2026-10-04 · **Status**: **In the game** (2026-10-04) — all ten champions, the eight
> spells, hidden spell picking and opening casts ship on Field 7 with solid towers. Owner reviewed
> the verbs, the spell pool, spell picking and opening attacks (below). · **Picture**: `design/v2-champions/champion-sheet.png`
> **Data and checker**: `design/v2-champions/champions_draft.py`. It validates every opening on
> Field 7 with solid towers and regenerates the sheet.

Built on the owner's decisions in `design/v2-direction.md`:
- Three abilities plus a summoner slot.
- Openings stay with the champion; the spell brings none.
- Champions differ by verb, not by shape.
- Field 7, with solid towers.

## Summoner spells

One shared pool of eight, learned once.

| Spell | Init | Cooldown | Effect |
|---|---|---|---|
| Flash | 1 | 5 | Jump to an empty hex up to 2 away, over champions, walls and towers |
| Barrier | 1 | 4 | Shield 6 until round close |
| Ignite | 2 | 5 | Enemy within 3 burns for 2 rounds and its healing is halved |
| Heal | 1 | 5 | Heal yourself and the most wounded ally within 2 for 4 |
| Exhaust | 2 | 5 | Enemy within 2 deals half damage until the end of the next half |
| Teleport | 4 | 6 | Move next to a friendly tower or beacon, from anywhere, including spawn |
| Smite | 1 | 5 | 6 damage to a tower or open nexus within 2; defenders don't reduce it |
| Cleanse | 1 | 4 | Remove all statuses from yourself; can't be moved this half |

**Rules.**
- **Cooldowns: 4–6 rounds.** At about 12 rounds a match, that is two or three uses.
- **Picking — owner's decision.** After the draft, both teams choose all five spells at the
  same time, hidden from each other. Both choices are revealed when the opening begins. This
  is the only hidden information in the game. It lasts only until play starts, so Pillar 1
  (nothing hidden during play) holds.
- **No duplicates within a team**, as the owner leans. Both teams may take the same spell.
- **Spells sit on the ladder like abilities**, bound by the ceiling. They have no sigils.

**Why these eight.** Every spell has an answer inside the pool or in the kits:
- Cleanse answers Ignite and Exhaust, and the status-heavy champions.
- Root stops Flash.
- Teleport is initiative 4, so the opponent sees it coming.

Initiative-1 spells are the late-ladder answers; Teleport is a top-of-ladder commitment.

## New status vocabulary

These are the verbs that make champions distinct. Each needs a new effect kind in the
engine.

| Verb | Rule |
|---|---|
| **Root** | Can't move, dash or be moved until the end of the next half |
| **Burn** | Takes 2 each time it resolves an ability or basic attack; shields don't block it |
| **Mark** | The next hit it takes, from anyone, deals +3 |
| **Wall** | An empty hex becomes impassable (like a solid tower) until the end of the next round |
| **Swap** | Caster and an ally trade places |
| **Pull ally** | An ally moves up to 2 hexes toward the caster |
| **Unstoppable** | Immune to root, push and pull this round |
| **Cleanse** | Removes root, burn, poison, mark and exhaust |
| **Exhaust** | Deals half damage until the end of the next half |

Conditional bonuses also need support: "+3 to rooted targets" and "doubled on burning targets".

## The four samples

| Champion | Role | One sentence | Q / W / E verbs |
|---|---|---|---|
| **Anchor** | Top | Pins one enemy in place so the team can collapse on it | pull + root · shield + unstoppable · fixed sweep that punishes rooted targets |
| **Ember** | Jungle | Sets enemies alight so every action they take hurts | burn · swap with an ally · rotatable cone, doubled on burning targets |
| **Lens** | Mid | Marks a target for the whole team and reshapes the board with walls | mark · wall · rotatable line |
| **Oriel** | Support | Keeps one ally standing and pulls it out of trouble | heal + cleanse · pull ally + shield · fixed area heal + shield |

**They are built to need each other**, as the owner described with the mastery feeling:
- **A combo.** Anchor roots a target, Lens marks it, and Ember's Wildfire hits it while it
  burns.
- **An answer.** Oriel's Mend undoes exactly that combo when the enemy runs it.

Each champion's card on the sheet says how to play it, who it works with, and what answers it.

## Attacks in the opening — owner's decision: try it with every champion

Each champion turns **one** ability's opening into one that **casts the ability**. The cast
resolves its combat effect partway through the opening, and the ability then starts the
match on cooldown.

| Champion | Opening attack | Instructions | What it does in the opening |
|---|---|---|---|
| Anchor | Chain Hook | top forward-right → top forward-right → **cast** | Walks up the top lane and hooks an enemy that came forward. The root pins it for the rest of the opening, so any of its team's plays that would move it can't be played |
| Ember | Kindle | jungle forward-right → jungle forward-right → **cast** | Sets an enemy alight before round 1: every action it takes in the first round burns, opening casts included |
| Lens | Lance | mid forward-left → **cast** → mid back-right | Poke and retreat: steps in, fires the line, steps back |
| Oriel | Mend | support forward-left → **cast** → beacon II | A support's cast is the answer: cleanse and heal the ally the enemy just hooked or burned |

**Owner's revision (2026-10-04): casts are less strict, and may fire other champions' abilities.**
- **One** ability per champion has an attack opening. Its instructions cast **one** ability
  usually, **sometimes two**, **rarely three**. A cast names a role and a slot ("mid casts
  Q"), so it may fire another champion's ability — whichever champion was drafted into that
  role. This ties the opening to the draft.
- **A cast never makes the opening unavailable.** With nothing to hit, it **fizzles**: no
  effect, but the ability **still goes on cooldown**. Movement instructions stay strict.
- *This supersedes D-041, which made a targetless cast block the opening.*

**Rules (Claude's, each reversible):**
1. **Opening casts aim themselves (D-043)** by fixed, visible rules, and the preview shows
   the result before you commit:
   - enemy abilities: the nearest enemy champion, ties to the lowest HP;
   - ally abilities: an ally with a status to cleanse, else the most wounded;
   - rotatable patterns: the facing that hits the most enemies;
   - dash or wall: the empty hex in reach nearest the closest enemy.
   An opening stays one decision: which play, in what order.
2. **The ability starts the match on cooldown.** This is the owner's trade, and it reverses
   D-011 for cast openings only.
3. **Opening damage can't kill.** It leaves a champion at no less than 1 HP. This guards
   against an alpha strike deciding the match before round 1, which would also kill
   comebacks.
4. **Statuses applied in the opening run their normal course into round 1.** Burn
   triggers on opening casts too.

**What the checker says (opponent ignored, then included):**
- All 16 openings are still playable from the starting line.
- The team puzzle now completes in **944 of 6,144 sequences (15%), reaching 68 end
  formations.** That is up from 8% and 61, because a cast step doesn't move anyone.
- **Every cast starts on our own half** (rows −1 to −2) and reaches row +1 at most, so it
  only finds a target once an enemy has advanced.
- If the opponent has finished its opening, Chain Hook and Kindle find a target in 97% of
  its possible end formations, and Lance in 71%. **An opening attack wants to be played late,
  after the opponent has committed.** The team that places second (A, per D-013) has the
  edge here, which may need watching.
- The sheet shades each cast's reach in red (green for Mend) on its opening diagram.

**Change made for it:** Mend's cooldown went from 0 to 1, so casting it in the opening costs
something.

## Openings on Field 7

Each champion has four openings: three tied to its abilities, plus its own fourth.

- **All 16 can be played from the starting line**, with solid towers and the team on the line.
  Field 7's start hexes share the hexes in front of them, so two moves in one opening must
  not claim the same hex. Nine of the first versions failed this and were rerouted.
- **As a team puzzle:** four sample champions plus a bottom who stays on the line, every
  choice of opening in every order. **462 of 6,144 sequences (8%) complete without a
  fallback**, and they reach **61 different end formations**.
  - Order matters a great deal, and there are many good plans.
  - This is the puzzle the owner likes, now on the board he chose.
  - The opponent's plays are not included, so the real number is lower.
- **Each opening is mostly one or two steps forward plus shaping.** On a board with front
  lines 6 rows apart, three steps forward collides with the enemy.

## Not decided — for the owner

1. ~~The verbs~~ — accepted; more to come later.
2. ~~The spell pool~~ — accepted.
3. ~~Spell picking~~ — simultaneous and hidden, revealed at the opening.
4. **Numbers.** Damage multipliers follow today's pricing by initiative, discounted where an
   ability also applies a status. They are first guesses; self-play will tune them once the
   verbs exist in the engine.
5. ~~Attacks in the opening~~ — trying it with every champion (above).

## The shipped roster of ten

Built by Claude on the owner's instruction ("you can design them and build on your own"). The
data is generated by `design/v2-champions/roster_v2.py`, which checks every opening on Field 7
with solid towers before writing `assets/data/champions/*.json` and `assets/data/spells.json`.
The four samples above keep their design; the six new champions follow the same rules:
- one sentence each;
- three different verbs;
- exactly one opening that casts — one cast usually, two sometimes (Viper's Fang, Ranger's
  Volley, Gunner's Snipe), three rarely (Tempest's Eye of the Storm).

| Champion | Role | How to play it | Q · W · E (effects) | Opening attack |
|---|---|---|---|---|
| **Anchor** | Top | Pins one enemy in place so the team can collapse on it. | Stand Firm (shield, unstoppable) · Chain Hook (damage, displace, root) · Groundswell (damage) | Chain Hook: move top forward-right → move top forward-right → cast top w |
| **Ember** | Jungle | Sets enemies alight so every action they take hurts — then cashes in. | Kindle (burn) · Switch (swap) · Wildfire (damage) | Kindle: move jungle forward-right → move jungle forward-right → cast jungle q |
| **Lens** | Mid | Marks one target for the whole team, and reshapes the board with walls. | Mark (damage, mark) · Prism Wall (wall) · Lance (damage) | Lance: move mid forward-left → cast mid e → move mid back-right |
| **Oriel** | Support | Keeps one ally standing, and pulls it out of trouble. | Mend (heal, cleanse) · Tether (pullally, shield) · Sanctuary (heal, shield) | Mend: move support forward-left → cast support q → beacon support 1 |
| **Bulwark** | Top | Holds the ground in front of its tower: shoves enemies off it and walls the way. | Shove (damage, displace) · Rampart (wall) · Quake (damage, displace) | Quake: move top forward-right → move top forward-right → cast top e |
| **Viper** | Jungle | Hunts the isolated: poisons a target, then dives on it. | Lunge (dash) · Fang (damage, poison) · Ambush (damage) | Fang: move jungle forward-left → cast jungle q → cast jungle w |
| **Tempest** | Mid | Throws the enemy line around: knockbacks that set up its allies. | Zap (damage) · Gust (damage, displace) · Eye of the Storm (damage, displace) | Eye of the Storm: cast top q → cast jungle q → cast mid q |
| **Ranger** | Bottom | Shoots from the back line and cashes in the marks its team sets. | Volley (damage) · Pin Shot (damage, root) · Rain of Arrows (damage) | Volley: move bottom forward-right → cast mid q → cast bottom q |
| **Gunner** | Bottom | Bursts from safety, hardest on a target that can't move. | Snipe (damage) · Recoil (dash) · Buckshot (damage, displace) | Snipe: cast top w → cast bottom q → move bottom forward-right |
| **Bastion** | Support | Shields the team and drains the dive. | Aegis (shield) · Rally (heal, unstoppable) · Judgement (damage, exhaust) | Judgement: move support forward-left → move support forward-left → cast support e |

**Cross-champion casts** (the owner's revision) appear in four openings:
- Tempest's Eye of the Storm fires the top's, the jungle's and the mid's Q.
- Ranger's Volley fires the mid's Q, then its own.
- Gunner's Snipe fires the top's W before its own Q.
- Viper's Fang fires its own Q (Lunge) and then W.

Which ability fires depends on who was drafted into that role. That ties the opening to the
draft, as the owner wanted.

**A cooldown-0 ability fired in the opening still costs round 1 (D-044).** Otherwise Zap and
Volley would be free.

### Every opening plays from the starting line

`tools/Augury.Tools openings` reports **40 of 40** playable on Field 7, with towers walkable and
with towers solid.

## First measurements (self-play, 200 matches, random drafts, after the balance pass D-047)

| Measure | Value | Note |
|---|---|---|
| Rounds per match | mean 13.6, median 14 (9–18) | The owner wants about 15 minutes for an advanced player; 14 rounds looks about right but needs a timed human game |
| Endings | siege 77%, direct nexus kill 23%, round cap 0% | Every match ends with a nexus falling |
| Comebacks | 25% | |
| Champion win rates | 42–58% | Before D-047: 32–67% |
| Side | the team that opens round 1 wins 54–59% | Accepted for now, like White in chess (D-045) |
| Opening fallback | 44% of team openings | Usually only the **last** champion (1.2 on average) steps one hex. The criterion (10–25%) was written for the classic board; on Field 7 the teams meet within the opening |
| Distance after the opening | 2.1 hexes to the nearest enemy | |
| Ready tier-3/4 abilities with a target | 77% | |
| AI decision time | max 79 ms | Budget 1,500 ms |

## Open questions for the owner

1. **The round-1 opener's edge** (D-045): accept it like White in chess, or compensate it?
2. **The opening fallback** now catches the last champion in almost half the team openings. Is
   that a cost of shallow Field 7 to accept, or should the last play get more freedom?
3. **Champion feel.** The numbers are first guesses, balanced only against the AI. Your play
   will tell which verbs feel good.
