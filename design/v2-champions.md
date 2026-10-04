# v2 Draft — Summoner Spells and Sample Champions

> **Date**: 2026-10-04 · **Status**: Draft for the owner to react to. Nothing here is in the
> game yet. · **Picture**: `design/v2-champions/champion-sheet.png`
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

**Proposed rules.**
- **Cooldowns: 4–6 rounds.** At about 12 rounds a match, that is two or three uses.
- **Picking.** After the draft, teams pick one spell at a time, B first (A drafted first).
  Picks are visible.
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

1. **The verbs.** Are root, burn, mark, wall, swap and pull-ally the right vocabulary, or are
   there verbs you want instead?
2. **The spell pool.** Is eight right, and are any of these wrong for this game?
3. **Spell picking.** Alternating after the draft, B first? No duplicates within a team?
4. **Numbers.** Damage multipliers follow today's pricing by initiative, discounted where an
   ability also applies a status. They are first guesses; self-play will tune them once the
   verbs exist in the engine.
5. **Parked ideas.** Anchor's fourth opening, Iron March, is a natural first test of
   *attacks in the opening* if you want to try that idea.

## Next, once you've reacted

Add the new verbs to the engine, with tests. Rewrite all ten champions on this pattern. Ship
Field 7 with solid towers. Re-measure with self-play.
