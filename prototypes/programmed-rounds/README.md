# Prototype: programmed rounds — hinted cards

**Hypothesis (current, 2026-11-09).** Draft → scripted opening → a few deadly rounds in which
each champion plays one ability per round as a **face-down card**. Teams put cards down one at a
time; every card has a public **open effect** that happens the moment it goes down (step, swap,
heal those around, guard, chip, shove, hit the nexus) and a secret **main action** that fires by
initiative once every card is down. The open effect is a hint about which card it was, and the
opponent answers it with their own cards. This should be easy to learn (one card per champion,
no budget), hard to master (reading and baiting hints, order of placement), quick (a match is
the opening plus about five rounds), and swingy (a round takes about a third of all HP on the
board and kills about two champions).

Earlier hypothesis (2026-10-21 to 10-24): secret orders under an energy budget — a move and up
to two abilities per champion, resolved simultaneously by tier. Replaced on the owner's
direction; the history is in git and `design/v3-programmed-rounds.md`.

**What it is.** One self-contained HTML file, no server, no libraries: `index.html`. Open it in
a browser (or the published artifact). 10-champion draft (two per role, give/want pairs), the
main game's Field 7 board (default) or a 37-hex board, the scripted opening from v2, then up to
6 card rounds.

## Rules in short

- **Opening** (unchanged): teams alternate scripted plays; each champion plays one of its three
  scripts; one script per champion casts (now at half damage, still can't kill). **B opens.**
- **Each round:** every alive champion plays exactly one of its three abilities as a card.
  Placing goes in a **snake** — first, then pairs (B A A B B A A B B A in round 1; the first
  team alternates each round). Put a card down → its open effect happens now (you pick the hex
  for a step/swap/shove) → you aim it in secret (target, hex or facing).
- **Hints:** two of each champion's three cards share the same open effect, so seeing it
  narrows the card to one or two. The enemy panel says "could be X or Y" and highlights them.
- **Resolution:** initiative 4 → 1; within an initiative both teams at once: control, shields
  and heals → damage → deaths. Dead before your initiative = card lost. Exhaust cancels the
  target's card if it hasn't fired. Area cards hit whoever is there when they fire, so a later
  open-effect step dodges them; tracking cards can't be dodged but fire last.
- **Lethality:** HP 12–18, main actions 3–7 damage, ×2 when the target has the status your
  champion wants, ×1.5 on a champion with no ally beside it. Open effects can't kill.
- **Score:** nexus 30; a death costs 5; centre tower 1 per round; beside the enemy nexus 3.
  The dead miss one round and return a row in front of their start. 6 rounds max.

## How to run

Open `index.html`; "New: play A vs AI" or "New: watch AI vs AI". Rules are under the Rules
button. Headless checks (Chrome, no server) through the URL hash:

- `index.html#sim=400,field7,22` — 400 AI matches, random drafts from seed 22; prints counters
  and per-champion win rates. Flags appended after the seed: `noreadA` / `noreadB` (that AI
  ignores open effects), `openA` (A opens), `tokenA` (A places first in round 1), `snakeopen`,
  `altplace` (strict alternation when placing), `revB` (B's cards first within an initiative).
- `index.html#log=7` — one match's full log, seed 7.
- `index.html#uitest=99` — plays team A through the real buttons to the end; `#uitest=2` stops
  mid-placement in round 2 (for a screenshot).
- `index.html#x=0,symtest` — 30 mid-round positions mirrored with the teams swapped; the AI must
  make the mirrored choice with the same score (30/30 SAME).

## Status

In progress. First playable 2026-10-21; opening 10-22; scripted opening 10-24; **hinted cards
2026-11-09**. AI-vs-AI numbers are indicative: the AI is greedy, one card at a time.

## Findings (2026-11-09, Field 7, 400–500 AI matches per run)

- **Lethality hits the brief.** About 33% of all HP on the board is lost each round, about two
  of nine or ten champions die per round, a third of kills are bursts (from 60%+ HP in one
  round). Matches last 4.9–5.3 rounds after the opening; about 90% end with a nexus destroyed.
- **Hints matter, modestly, for the AI.** An AI that ignores open effects loses about 7 points
  of win rate (measured mid-session, before the order fix: A 63% → 53% when A is blind, 67%
  when B is). The AI only uses hints to judge danger;
  a human reading them should get more.
- **Exhaust is huge when a round is one card per champion.** With Quake exhausting an area at
  initiative 4, Bulwark's team won 95%. Exhaust is now only on Bash (one hex, initiative 3) and
  Gust (a hex, initiative 2).
- **Turn order outweighs any single card.** Who opens first and who places last swings a match
  by 10–15 points, and the direction flips with small content changes (two bottom-lane damage
  tweaks moved the sides from 50/50 to 38/62). Strict alternation when placing gives the last
  placer about +10; the snake reduces that to about ±2. A snake *opening* breaks the scripts
  (written for alternation): Viper and Gunner fell to 25–33%. Current setup (B opens,
  alternating opening, snake placing): A 53–54%.
- **Champion spread** 36–64% after one tuning pass: Ranger about 62% and Gunner about 38% (the
  same lane), everyone else 43–57%. The small board is unbalanced (Anchor 79%, Bulwark 21%); its
  scripts were never written for it.
- **Dead cards.** About 0.6 cards per round are lost (caster died first or exhausted) and 1.6
  fizzle (nothing left in range), so roughly a fifth of cards do nothing. That is the price of
  dodging and burst; whether it feels bad needs a human.

## Not in it

Quests, spells, cooldowns, walls, swamps, hotseat, towers you can hit (siege hits the nexus).
The AI doesn't bluff: it doesn't pick a card *because* its open effect is ambiguous.
