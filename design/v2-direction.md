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
4. **Terrain**, once the board is settled.

## Idea parking lot

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
