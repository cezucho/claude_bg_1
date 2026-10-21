# Brief: "programmed rounds" for AUGURY — a design brainstorm (no prototype yet)

You are consulting for a one-person indie studio. The owner wants an honest, expert think-through
of a new core-loop idea. Nothing is to be built. Your output is a written analysis.

## The game today (AUGURY, v2, playable in a browser against an AI)

A deterministic, turn-based, two-player hex tactics game in the shape of a MOBA (League of
Legends): 5 champions a side, three lanes, a 59-hex board ("Field 7", 7 rows, front lines 6 rows
apart), five towers (one centre, two home towers each side; solid, impassable), a nexus each
(60 HP). Pure integer simulation, no randomness, everything visible except one thing (see below).

**Match flow.**
1. **Draft** — snake pick of one champion per role from 15 (3 per role). A champion picked is
   gone for both sides. Champions are built in "give/want" synergy pairs: each champion's kit
   applies one status (root, mark, burn, poison, exhaust, or walls) and *wants* a different one
   (everything it deals to an enemy carrying its wanted status hits ×2, at least +3). A pair =
   one gives what the other wants. Teams with no pairs are "tragic" and lose ~70/30.
2. **Summoner spells** — each team secretly gives each champion one of 8 shared spells (Flash,
   Barrier, Ignite, Heal, Exhaust, Teleport, Smite, Cleanse). Revealed when the opening starts.
   The only hidden information in the game.
3. **Opening** — the phase the owner loves. Teams alternate: each play is one champion's chosen
   "opening" (each champion has 4 openings: one per ability slot, each a scripted set of three
   instructions like "top forward-right, mid forward-left, beacon under jungle" or "mid casts E").
   Every champion plays exactly one. Moves are strict (blocked = unplayable); casts auto-aim and
   may fizzle; opening damage can't kill. The opening's end counts as a quest check (below).
4. **Rounds** (≈12 per match, target ≈15 min). Each round has two halves. Each half: one
   **basic action** per team (move up to SPD hexes, or a basic attack), then the **ladder**:
   teams alternate playing one ability at initiative ≤ the current **ceiling** (starts 4); the
   ceiling drops to the initiative just played; each champion acts once per half; **pass** gives
   the opponent one unanswerable **Last Word**; two abilities sharing a lit sigil may **chain** as
   one step. Initiative tiers: 1–2 free aim at a target in range; 3 a rotatable pattern; 4 a big
   fixed pattern aimed at the enemy.
5. **Round close** — death check (also at the end of half 1), status phase (poison, tower shots),
   siege (each held tower deals 1 to the enemy nexus; each death costs the dead champion's nexus
   3), quests, upkeep, win check. Nexus at 0 = loss.

**Champions.** Three abilities (Q/W/E, initiative 1–4, cooldowns 0–3) plus the spell in slot R.
Stats: HP 23–35, POW ~1.0, hits of 4–8. A passive. A **quest**: a positional task ("ends the
round on a hill", "next to the centre tower while we hold it", "in the enemy half with no ally
beside it") that earns a stack at each round close; +8% POW per stack; upgrades at 2 and 4
stacks (a stat, then a stronger ability). Dying costs half the stacks and lengthens respawn by a
round per stack. A champion with no ally adjacent is "caught alone" and takes ×1.5 damage.
Terrain: hills (+1 reach), swamps (slow).

**Measured.** ~12 rounds, ~270 decisions with a real choice per match, 8–9 deaths/match, only
~18% of kills are bursts (from 60%+ HP within a round); a good opening beats a random one 61%;
a great draft beats a tragic one 72%; comebacks ~22%.

## The owner's pillars and tastes (in their words, paraphrased)

- Deterministic; all state inspectable. ("Nothing hidden during play" — bent once for spells.)
- Not chess: no memorising openings and positions. Each champion is distinct and *geometric* —
  a good opening should be required for them to be effective; the game should feel like
  choosing a strategy after the draft and fulfilling it.
- The draft must matter (65/35 good vs tragic); the opening must matter (60–65%).
- Momentum: League-style swings — a weaker team ambushes the carry and the match turns; "one
  team solved the riddle quicker"; "one team made the error and the other used it".
- Pace: ~15 minutes for an advanced player; a best-of-three in the time of one League game.
- Complaint from early playtests: *"too many choices in the endgame"* (20 abilities on screen);
  and the ladder's alternating single actions feel like "trading damage", kills feel delayed.
- Recent inspiration: **Marvel Snap** — few turns, secret simultaneous commits, big reveals,
  escalating energy, combos inside a turn, positioning across locations. The owner dislikes
  deck-building (so the draft stays) but loves Snap's combo-and-positioning feeling and pace.

## The new idea (the owner's, rough — "not well thought through")

> Each round, both teams **secretly program one ability per champion** (five abilities). Then
> a resolution "ladder" starts. Each round one team **starts** (alternating), and the starting
> team has certainty that its highest-initiative programmed ability goes first. But *which one
> is first matters, and which ones follow also matters*: if the starting team's first ability is
> initiative 2 and the opposing team programmed initiative 3 or 4 abilities, those are
> **discarded** (the ceiling has already dropped below them). The algorithm then **alternates**
> between teams, with the initiative level going down. So a round can be a sequence of up to
> 5 abilities per team, or only 3 or so if the ceiling cuts some off.
>
> Five abilities per team per round "is a lot". And it contradicts an earlier wish: that a
> champion could use *all* its abilities if the initiative level permits. The owner knows this
> conflicts and wants to think it through.
>
> A **much smaller map** would suit this prototype, so that after the opening all champions are
> close and many programmed abilities actually hit.

Things to keep in mind:
- The opening already uses a scripted-instruction language ("move top forward-right", "cast
  mid e") and auto-aim rules for casts (nearest enemy, lowest HP tiebreak; allies: statused
  then most wounded; rotatable patterns face where they hit most). A programmed round could
  reuse it, or let the player pick targets at programming time (targets may move before
  resolution — what then?).
- The owner wants a *prototype* later, built beside the current game, not replacing it.
- The AI must be able to play it (a deterministic search over hidden programs is harder than
  the current depth-3 ladder search; the opening planner exists as a start).

## Your deliverable

Write a report in Markdown to the path you are given. Be concrete and opinionated; prefer
rules you can state in one sentence over vague principles. Where you propose a variant, state
the rule, what it fixes, and what it risks. Cover, at minimum:

1. **The good parts** of the owner's idea, and how to make each one *better*.
2. **The bad parts** — what breaks, what feels bad, what is unsolvable — and whether each is
   fixable.
3. **Streamlining** — what to delete so the round is quick but keeps strategic flavour. Assume
   4 abilities per champion is too many to program every round; propose how many decisions a
   round should hold and why.
4. **A recommended rule set** for the prototype, in ≤ 12 numbered rules, including: how
   programs are written, what is hidden and when revealed, how resolution order is decided
   (and what "discarded" should mean), how targets are chosen, how movement fits in, what a
   round's end does, and the map size.
5. **How to measure** whether it works (what the AI harness should count).
