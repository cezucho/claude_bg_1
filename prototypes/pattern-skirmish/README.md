# Prototype: pattern skirmish

**Hypothesis.** The auto skirmish flows better if (1) where your five champions stand relative
to each other decides *which abilities fire*, so there is a shape to search for and a reason to
keep changing it, and (2) a decisive fight is followed by a short *regroup* phase in which both
teams re-place their champions, so the walk after a won fight is skipped and the winner makes a
clear choice: Baron, Dragon or tower. Owner's ideas, 2026-11-19, after a review of
`prototypes/auto-skirmish/` (which is left untouched and is still the main line).

**How to run.** Open `index.html` (keep the `art/` folder next to it). No server, no libraries.
`simulate(n, 'small', seed, rules)` in the browser console runs AI-vs-AI matches.

**Status.** In progress. First playable 2026-11-19. Owner played it 2026-11-20; readability, paths, regroup cards and the coach added the same day. 2026-11-21: a quieter pulse (passives in the Q slot, rare W/R, broken champions and a death check, 3 opening cards); see the section of that name.

## What changed from the auto skirmish

- **Elements instead of tags.** Fire, Frost, Earth, Storm, Light, Shadow; a champion has one or
  two. Auras and bonds are gone.
- **A basic attack and three abilities (Q, W, R).** An ability fires only while its *pattern* is
  complete: allies in given hexes around the champion, read in the team's own frame (front =
  toward the enemy nexus), sometimes of a given element. Q is easy (usually "an ally beside it"),
  W harder ("an Earth ally in front"), R hardest ("an ally behind left and one behind right").
  Viper is the lone wolf: its Q and R need *no* ally near, its W needs one behind.
- **Cooldowns.** When a champion acts it uses the hardest lit ability (pattern complete, off
  cooldown, has a target), else its basic attack. Q rests 2 pulses, W 3, R 4. A shape that just
  fired is spent, which is the reason to move one champion and light something else.
- **Chances and statuses live in abilities.** Each champion keeps one general chance on its
  basic attack. Freeze, root, blind, silence, knock-up, taunt and enrage now come from abilities,
  so they are set up, not rolled on every hit. Level-ups mostly choose which ability grows
  (e.g. "Q: cooldown −1" or "R: +1× damage").
- **Regroup.** When a pulse leaves one team two or more champions up, the winners place each
  champion up to *1 + the current death timer* hexes away (2 early, up to 5 late; the owner's
  rule: short timers, short moves), not beside an enemy and not at the enemy nexus. Then the
  losers move their survivors the same distance and choose, near their nexus, where their dead
  will return. Death timers are not changed. At most one regroup every two rounds.
- **Pits one hex inward.** Dragon and Baron have six hexes around them instead of three.
- **Dragon reward:** one champion of the team's choice levels up (the old reward raised chances,
  which barely exist now).
- **Removed:** gold, shop and items, stances, charged specials. Champions have 25% more HP,
  because abilities hit harder than the old basic attacks. Round limit 18.
- **Interface.** Q W R under each champion: bright = fires next pulse, dull = pattern complete
  but cooling down, grey = not complete; they update while you hover a destination. Selecting a
  champion marks the hexes where its patterns want an ally. Pattern diagrams on the cards, the
  hover card and the draft screen.

Switches are in `RULES`: `regroup`, `rgLead`, `rgBase`, `rgMax`, `rgGap`, `rgCost`, `hpScale`,
`dragonLevel`.

## Measurements (AI vs AI, small board, 400 matches per row unless noted)

| Rules | Rounds | Ended by a destroyed nexus | A wins | Comeback | Tower within 2 rounds of a won fight | Dragons / Barons per match | Steals | Regroups per match |
|---|---|---|---|---|---|---|---|---|
| Default, seed 11 | 13.9 | 79% | 55% | 15% | 52% | 1.63 / 1.00 | 18% | 3.7 |
| Default, seed 5000 | 14.0 | 81% | 48% | 15% | 50% | 1.60 / 0.97 | 17% | 3.8 |
| Regroup off | 16.2 | 54% | 53% | 12% | 40% | 2.36 / 1.31 | 14% | 0 |

- **Regroup does the job stances used to do.** Without it and without stances, 46% of AI
  matches run out the clock; with it, about 20%. A won fight turns into a tower within two
  rounds 52% of the time against 40%.
- **Abilities carry the fights.** Per match: Q 44, W 21, R 11 casts against 39 basic attacks,
  so about two actions in three are abilities, and R fires about once per champion per match.
- **Regroups happen 3.7 times per match**, more than the two the owner had in mind. Requiring a
  lead of three champions gives 2.6 (300 matches) with no other clear change. To decide by play.
- **Where the AI winners go:** tower 61%, Baron 29%, open nexus 10%, Dragon almost never.
- **Sides:** 55% and 48% for A on two seeds; treat as even until a bigger run says otherwise.
- **Comebacks are low (15%)**; the auto skirmish default measured 19% in the same harness. The
  Baron taker wins 82%. Not addressed in this prototype.
- **Regroup options tried (300 matches each):** dead return a round sooner (`rgCost` 1): 60% end
  by nexus, comebacks the same; fixed range 3 instead of growing with the death timer: no clear
  difference in AI play, so the growing range is kept for how it reads to a player.
- **Pattern rules, counted over all 32 teams and all formations** (from the review that led
  here): under the old "any neighbour" synergies a random connected formation got 54% of the
  best possible value; under pattern rules 18%.

**Checked without a person.** 25 full matches driven through the interface functions with random
legal choices (draft, opening, level-ups, moves with hover, regroup as winner and as loser):
no script errors.

## 25 champions (added 2026-11-19)

Owner: a pool of 25, five per role, so the draft takes part in the results. Fifteen champions
added: Cinder, Gale, Thorn (Top); Fang, Wisp, Shiver (Jungle); Pyra, Umbra, Quartz (Mid); Flint,
Hail, Dusk (Bottom); Brook, Ash, Moss (Support). Each has elements, a general chance, Q/W/R with
patterns, two opening cards, and a standard growth path built by `autoTree` (the first ten keep
their hand-written paths). Elements across the pool: Earth 7, Fire 6, Frost 5, Storm 5, Shadow 5,
Light 4. Art: one image each from `tools/asset_gen/gen_sprites.py --spec pattern_sprites.json
--out prototypes/pattern-skirmish/art` (briefs in `assets/data/art/pattern_sprites.json`), all
kept first try. The draft screen now scrolls.

AI vs AI, small board, 400 matches per row. The AI drafter prefers champions whose elements
complete its picks' patterns.

| Rules | Rounds | Ended by a destroyed nexus | A wins | Comeback | Regroups per match | Barons per match | Baron taker wins | Baron taken while behind | Steals |
|---|---|---|---|---|---|---|---|---|---|
| Default, seed 11 | 13.5 | 82% | 50% | 18% | 3.65 | 0.94 | 80% | 66 times, 53% won | 12% |
| Default, seed 5000 | 13.5 | 79% | 48% | 20% | 3.75 | 0.95 | 80% | 58 times, 55% won | 13% |
| Default, seed 90000 | 13.5 | 81% | 51% | 19% | 3.65 | 0.94 | 78% | 72 times, 47% won | 15% |
| Regroup lead 3 | 14.0 | 75% | 46% | 17% | 2.51 | 0.98 | 78% | 61 times, 62% won | 14% |
| Regroup off | 16.1 | 51% | 46% | 15% | 0 | 1.38 | 83% | 53 times, 38% won | 12% |

- **Regroups:** 3.7 per match, the same as with ten champions. A lead of three gives 2.5 and
  costs about six points of matches that finish.
- **Comebacks:** 18–20%, up from 15% with ten champions, and back to the auto skirmish's level.
- **Going for the Baron:** the winners of a fight regroup toward the Baron 26% of the time
  (tower 62%, open nexus 11%, Dragon under 1%). Just under one Baron falls per match; its taker
  wins 80%. A team takes it while behind in about one match in six and then wins about half of
  those. Steals are 12–15% of monster kills.
- **Sides** are even over three seeds (48–51%).
- **Drafting for patterns is worth little yet:** a pattern-seeking drafter against a random one
  wins 53% and 55% from the two sides. Which champions you get matters more than how their
  elements fit.
- **Champions are far from balanced** (1,200 matches, win rate of the teams they were on): Ash
  73%, Pyra 72%, Cinder 69%, Umbra 64%, Ember 64% at the top; Quartz 32%, Moss 34%, Lens 36%,
  Wisp 37%, Gale 39%, Fang 39% at the bottom. Four of the top five are Fire champions that stack
  poison. Not tuned.
- **Some abilities almost never fire** (casts per game): Gale W 0.0 and R 0.2, Bulwark W 0.0,
  Fang R 0.1, Ember W 0.2, Quartz R 0.2, Wisp R 0.2, Lens R 0.3. Front-line champions rarely
  have an ally on each side or in front. These patterns need redrawing or a smarter AI.

Checked again without a person: 25 scripted matches through the interface, no script errors.

## Balance, easier patterns, planning AI (added 2026-11-19)

Owner: balance the champions, make the hard patterns easier to reach, and make the AI smarter
(spot patterns, look two turns ahead).

**Easier patterns.** Twenty patterns changed, mostly W and R that fired under once per game:
"an ally on each side" became "an ally at its side"; exact pairs of hexes became pairs of
directions ("one in front and one behind"); element needs "in front" of melee champions became
"beside". A new kind of pattern, "two (or three) allies beside it", is used by Fang, Wisp, Thorn
and Ash. R casts per match went from 10.7 to 13.5; the abilities that fired 0.0–0.3 times per
game now fire 0.5–2.3 times.

**Poison** deals 1 per stack per pulse (was 2): the top of the table was all poison stackers.

**Planning AI** (`aiTurn`, switch `RULES.aiPlan`: 0 old, 1, 2 default). It plays its moves out
instead of judging the position: every legal move is tried on a copy of the match and the pulse
is really run; for the best five it then gives the enemy its best answer and runs that pulse
too, twice with different dice; it keeps the move that leaves it best off (champions alive and
healthy, buildings, monsters, complete patterns at the end). Level-ups go to the ability the
champion has actually been firing. Measured, 1000 matches per side:

| | Wins against the old one-move AI |
|---|---|
| Planning AI, both pulses played out | 74% and 78% from the two sides |
| One pulse played out (600 matches) | 64% and 68% |
| Old AI with only the smarter level-ups | 56% |

A first attempt that looked ahead by judging positions, without playing the pulse, gained nothing
over smarter level-ups and was dropped. The planning AI does not fire more W and R than the old
one; it wins by trading better, not by building shapes.

**Balance.** One factor per champion (`TUNE` in the file) scales its HP and what its abilities
deal, heal and shield. Found automatically: seven passes of 1000 random-draft matches with the
planning AI, each pass nudging every champion toward 50%. Win rates went from 33–76% to 43–56%.
Strongest cuts: Umbra ×0.70, Anchor ×0.70, Ash ×0.75, Oriel ×0.80, Pyra ×0.80. Biggest boosts:
Moss ×1.45, Quartz ×1.35, Shiver ×1.35, Lens ×1.25, Seer ×1.25. It is a blunt tool: it does not
change what a champion does, only how much.

Final numbers, 2000 matches, pattern-seeking drafter, planning AI on both sides:

| Rounds | Ended by a destroyed nexus | A wins | Comeback | Regroups per match | Barons per match | Baron taker wins | Baron taken while behind | Steals |
|---|---|---|---|---|---|---|---|---|
| 12.8 | 86% | 48% | 16% | 3.2 | 0.84 | 78% | 13 per 100 matches, 58% won | 18% |

- Champion win rates with the pattern drafter: 43–59%. The top five are all Frost (Hail, Brook,
  Bulwark, Shiver, Rime): the drafter picks them together and their patterns feed each other.
- **Drafting for patterns now matters:** against a random drafter it wins 57% and 62% from the
  two sides (it was 53% and 55% before the balance pass).
- Regroups 3.2 per match; with a lead of three, 2.1. Winners regroup toward a tower 60%, the
  Baron 28%, the open nexus 12%.
- Comebacks 16%: the stronger AI closes games it is winning.
- Still rare: Anchor R 0.5, Gale W 0.5 and R 0.6, Cinder W 0.3, Ember W 0.4 casts per game.
  Melee champions cast little overall because they are often out of reach of a target.

Checked again without a person: 34 scripted matches through the interface, no script errors.

## Second review: is it more controllable, does it have phases? (2026-11-19)

The same measurements on the auto skirmish and on this prototype (400–600 AI matches per row).

| | Auto skirmish | Pattern skirmish |
|---|---|---|
| One-move AI against a player who moves at random | wins 65% | wins 96% |
| One-move AI against a player who never moves | wins 72% | wins 88% |
| Spread of the next pulse's result between legal moves | 3.1 | 3.2 |
| Spread of the next pulse's result from dice alone (best move) | 3.1 | 0.7 |
| Chance rolls that fire per match | 79 | 20 |
| First kill | round 1.6 | round 1.3–1.4 |
| Quiet pulses (under a quarter of average damage) | 15% | 14–16% |
| Share of all damage dealt in the busiest fifth of pulses | 38% | 42–43% |

(Spread is in points of material, where a healthy champion is worth 16.)

- **Control: clearly better.** In the old game the dice moved a pulse as much as the choice of
  move did, and a player moving at random still won a third of matches. Now the choice of move
  outweighs the dice almost five to one and random play loses nearly always.
- **Phases: not better.** The fight still starts in round 1 and runs at an even intensity. The
  first pulse is the most violent of the match (43–53 damage against an average of 30). After a
  regroup, damage dips only slightly (about 25 per pulse for three pulses, then back to 31–33).
  Regroup gives a decision point, not a quiet phase.
- Still open from the first review: no way to choose a target (a monster is attacked only when
  nothing else is in range), winners almost never regroup toward the Dragon, comebacks 16%.

## Geometry abilities and a staggered start (added 2026-11-19)

Owner: more abilities that change geometry (swap, push, attack then step aside); phases may not
be a problem. Built:

- **Five kinds of forced move**, all certain (never a chance), on 13 abilities:
  push an enemy back (Anchor Q, Gale Q, Gunner R, new on Bulwark W "Glacial Shove" and Brook R);
  pull an enemy to your side (Cinder W "Magma Hook", Thorn W "Vine Whip");
  swap places with an ally, then act (Anchor R "Intercept", Quartz W "Refraction");
  hit, then step aside or back (Gale W "Slipstream", Viper W "Hit and Fade", Flint W "Kickback
  Shot", Dusk W "Smoke Step"); pull a wounded ally to safety (Oriel W "Guiding Light").
  Ember W "Flame Dash" and Wisp W "Blink Strike" became leaps.
- **Preview:** dashed arrows in the caster's colour show every move the lit abilities will force
  in the next pulse, also while hovering a destination.
- **Staggered start** (`RULES.stagger`): Q fires in the first pulse, W from the second, R from
  the third.
- **Rebalanced** with six more automatic passes: 45–56% with random drafts.

Measured (2000 matches for the headline, 400–1000 for the rest):

| | Before | Now |
|---|---|---|
| Forced moves per match | about 3 (pushes and leaps only) | about 10 |
| One-move AI against a random mover | 96% | 95% |
| One-move AI against a player who never moves | 88% | 86% |
| Spread from dice / spread between moves | 0.7 / 3.2 | 0.7 / 3.1 |
| Damage in round 1, per pulse (old AI) | 49 | 42 |
| Kills per pulse in round 1 (old AI) | 0.45 | 0.37 |
| Ended by a destroyed nexus | 86% | 86% |
| Comebacks | 16% | 16% |
| Regroups / Barons per match | 3.2 / 0.84 | 3.2 / 0.84 |

- **Control held.** Three times as many forced moves did not make the game more random or less
  dependent on the player's moves.
- **The staggered start softens the opening a little**, not a lot: round 1 is still the most
  violent round.
- **With the pattern drafter the spread is wider, 39–62%.** The five Frost champions are on top
  (56–62%) because the drafter picks them together; Oriel (39%) and Flint (40%) are at the
  bottom. Oriel's new rescue fires only once per game.
- Checked without a person: 31 scripted matches through the interface, no script errors.

## Bans in the draft (added 2026-11-19)

Owner: with 25 champions it is time for bans; how do they change element drafting? Built: a ban
step in the draft (`RULES.bans`: `none`, `1`, `2`, `1+1`, `2+2`; switch on the draft screen). The
first number is bans per team before the picks, the second is bans per team once both teams
have two picks. A banned champion is out for both teams; at most two bans per role, so every role
keeps three. The AI bans the champion that would best complete the enemy's picks (early on, the
one whose elements fit the most others), unless it wants it itself.

2000 AI matches per row with the pattern drafter on both sides, plus 800 per side against a
random drafter:

| Bans per team | Champion win rates | Typical distance from 50% | Frost champions, average | Teams with all five Frost | Teams with 4+ of one element | W casts per match | Pattern drafter against a random one |
|---|---|---|---|---|---|---|---|
| none | 39–62% | 6.3 | 58% | 14% | 76% | 21.9 | 59% / 58% |
| 1 | 42–58% | 4.5 | 56% | 7% | 67% | 20.0 | 56% / 58% |
| **1+1 (default)** | 40–57% | 3.8 | 54% | 0% | 33% | 18.9 | 57% / 57% |
| 2 | 41–57% | 4.2 | 53% | 2% | 54% | 19.1 | 54% / 57% |
| 2+2 | 42–56% | 2.7 | 50% | 0% | 8% | 17.3 | 56% / 58% |

- **Bans break the all-Frost team.** Without bans one team in seven was all five Frost champions
  (one per role exists). With a second ban after two picks it never happens.
- **The later ban does the work.** Two early bans (row "2") leave more mono-element teams than
  one early and one late, because the late ban answers what the enemy has shown.
- **Drafting for patterns stays worth the same**, about 57% against a random drafter in every
  row. Bans flatten which champions win, not the value of fitting elements.
- **The cost is fewer element abilities:** W casts fall from 21.9 to 18.9 per match with 1+1 and
  to 17.3 with 2+2, because the banned champion is usually the one a pattern needed.
- 2+2 gives the flattest table but removes a third of the pool and nearly ends element teams
  (4+ of one element: 76% to 8%). 1+1 is the default.
- **The AI bans by element fit, not strength:** its most banned champion is Flint (two elements,
  fits many), who is one of the weakest. A human will ban differently.
- Other numbers did not move: 13 rounds, 86–89% end by a destroyed nexus, comebacks 16–18%.
- Checked without a person: 30 scripted matches with bans, no script errors.

## Readability, paths, regroup cards, coach (added 2026-11-20)

Owner's notes after playing: draft should show every portrait at once (details on hover) with
broadcast-style ban/pick slots; Q W R hidden behind sprites and towers; AI vs AI should stop at
the end; AI moves are hard to follow; MARLOW should coach (a setting / difficulty); levelling every
champion every few rounds is tiring (proposed: two paths per champion, then one champion grows at
a time, with icons); the regroup phase shows nothing changing.

Built:
- **Draft.** One grid, a row per role, all 25 portraits; hover = full card (abilities, patterns,
  chances, both paths, what it completes in your team). Slot bar: red ban slots and five pick
  slots per team that fill with portraits; a BAN PHASE / PICK PHASE banner; the slot being filled
  pulses. Fits 1366×768 without scrolling.
- **Paths.** After the draft, one screen: each champion has two paths of four steps (Power: each
  ability hits harder / Tempo: W and Q cooldown −1, +2 initiative, R cooldown −1; a champion that
  hits and heals picks Striker or Warden; Oriel picks Healer or Tempo). The suggestion is
  preselected, one click to start. Paths are built by `buildPaths` from each kit; the old
  hand-written `tree` arrays are no longer used.
- **Level-ups: one champion at a time.** A team grows ONE champion after opening cards 2, 3, 5,
  at the end of every second round, and per Dragon: either the next path step or a heal
  (❤️ +6 max HP, heals 8 now). The choice is a strip under the board (board stays visible), every
  option with an icon. Every round was tried first and measured slower (see below).
- **Regroup cards.** Before placing, the winners play one card, the losers one answer. Each card
  states its exact effect and the effect shows on the board as numbers. Winners: Siege (nearest
  building takes 2 per living champion, ×2 under Baron), Dragon / Baron (the monster takes the
  same; if it dies the reward is theirs), Rest (25% of max HP, cleanse). Losers: Rush back (dead
  return a round sooner), Fortify (+6 to the most damaged building), Rest.
- **Readable board.** Q W R, HP, initiative and statuses are drawn on a top layer above every
  sprite, with a dark backing. Every move slides; an AI move holds the board 0.9 s before the
  pulse; the last move of each team (and every regroup move) stays as a ghost ring and a dashed
  arrow in team colour; a tag top right says "Red moves Viper 2 hexes". Dead champions show a
  portrait and "† back R5" where they will return.
- **End of match.** No automatic restart; the board stays; Replay last pulse works there.
- **Difficulty + coach.** Easy: the AI plans one step (`aiPlan` 1), MARLOW tips every turn
  (which ability is one move from lighting and what it needs, without naming the hex; a kill on
  offer; a champion in danger), recommends a regroup card and a level-up, and in the draft lights
  champions that complete your picks (+fit badge) and names a ban. Normal: full planning AI and a
  💡 Hint button. Hard: Normal's AI, no coach. Tips come from the planning AI's own choice for
  your team, run inside `dry()`, so the match's dice are untouched.

Measured (AI vs AI, small board):

| | rounds | by nexus | comeback | regroups |
|---|---|---|---|---|
| before today | 12.8 | 86% | 16% | 3.2 |
| level-up every round, Rest 40% (200) | 13.7 | 79% | 22% | 3.0 |
| same, regroup cards off (200) | 14.7 | 66% | 21% | 3.4 |
| same, Rest 25% (200) | 13.2 | 82% | 24% | 2.9 |
| **final: every 2nd round, Rest 25% (3 × 200)** | **13.4–13.7** | **82–86%** | **18–27%** | **3.0** |

- Final cards played: Rush back 1427, Siege 1372, Rest 699, Baron 48, Dragon 21, Fortify 6
  (600 matches). Fortify and the monster cards are almost never worth it to the AI.
- Heal chosen in 31% of level-ups. Paths (win rate when taken, AI picks Power/Tempo by coin flip):
  Power 52%, Tempo 47%, Striker 51%, Warden 48%, Healer 44% (88 games). Tempo was clearly behind
  before it got +2 initiative and Q cooldown in step 2.
- Difficulty: Easy's AI as team A vs Normal's as B won 37% (A usually ~45–50%), 150 matches. A
  deeper search (beam 8, 3 samples) won 46%: not stronger, so Hard is not a stronger AI yet.
- AI turn time: Normal average 0.1 s, max 0.19 s (budget 1.5 s).
- Scripted human matches through the interface on all three difficulties: no page errors. One
  bug found and fixed: pressing Done while a regroup card's effect was still playing ran the
  regroup twice and froze the match.

### Regroup cards, second version: the card counts the formation (2026-11-20)

Owner: the first cards (played before placing, flat effects) traded control of the geometry for an
easy choice. Now each side **places first, then ends its regroup by playing a card that counts
where the champions stand**, with the card's number updating live as you place (hover a card: the
champions it counts get a gold ring, its target a dashed hex):
- Siege: the enemy building with most of your champions in range takes 3 per champion in range,
  +1 for each whose pattern is complete (×2 under Baron).
- Dragon / Baron: the same on the monster; if it dies the reward is yours.
- Rest: only champions beside an ally heal, 20% of max HP, 35% inside a complete pattern.
- Losers: Rush back (unchanged) or Rest. Fortify (+3 per survivor near a damaged building) was
  cut on the owner's call after it was played 2 times in 400 matches. After the cut (200 matches):
  14.6 rounds, 70% by nexus, comeback 19%.
- `RULES.rgGeo = false` keeps the flat values for comparison. The AI tries one formation per
  objective (each Siege target, each monster, a huddle for Rest; losers: home or around a damaged
  building) and keeps the best card value plus pressure on the objective plus shape.

| 200 matches each | rounds | by nexus | comeback | winner's lit abilities after regroup |
|---|---|---|---|---|
| formation cards (2 seeds) | 14.6–14.7 | 69–72% | 19–24% | 9.0–9.1 |
| flat cards, placed first | 13.6 | 80% | 22% | 9.2 |
| no cards | 15.3 | 62% | 17% | 9.3 |
| formation, Siege 4 or shape bonus 2 (2 seeds each) | 14.5–14.7 | 70–71% | 20–24% | 9.0 |

- The price of geometry is pace: a free Siege every regroup ended ~10% more matches by nexus.
  A bigger Siege does not buy it back; the AI often cannot get enough champions in range, or rests.
- Cards played (formation, 400 matches): Rush back ~990, Siege ~770, Rest ~780, Baron ~120,
  Dragon ~27, Fortify 2. Baron is now played 8× more than with flat cards; Fortify still never.
- "Lit abilities after a regroup" does not separate the versions: the AI builds shapes either way.
- A first attempt at the AI regroup valued clumping over objectives and slowed matches to 15.8
  rounds / 54% by nexus even without cards; fixed by adding the old objective weights.
- Scripted human matches on Easy and Normal after the change: no page errors.

## Tactic points and the final fight (added 2026-11-21, after Esports Godfather)

Owner asked to borrow two ideas from Esports Godfather (MOBA coach card game, 2024; research
summary in the session of 2026-11-21): a banked point budget for cards, and a final fight.
- **Tactic points.** +1 per round per team, at most 8. On your own turn, before moving, one card:
  Siege 2, Dragon 3, Baron 3, Rest 3 (all count the formation, as the regroup cards did), Rush
  back 2, Final fight 6. Costs are `RULES.tcost`. Regroup keeps its placing, without cards
  (`rgCards: false`). The AI plays a card worth at least `tpWorth` (4) per point, saves when a
  Final fight is in sight, and plays the Final fight only when a played-out copy says it wins.
- **Final fight.** After round 18 there is no score: everyone returns and a final fight decides
  the match. A Final fight card (6 points, 3+ enemies dead, who stay dead) forces one earlier.
  In a final fight nobody is moved by hand: champions with nobody in reach step toward the nearest
  enemy, then everyone acts; last team standing wins (after 12 pulses: more standing, then HP).

| 150 matches each (seed 1) | rounds | by nexus | final by card | final at time | comeback |
|---|---|---|---|---|---|
| neither (no regroup cards either) | 15.1 | 61% | – | (39% on score) | 19% |
| final fight only | 15.1 | 61% | – | 39% | 22% |
| tactics, tpWorth 3 (first try: Rest 2) | 17.3* | 45%* | 0* | 55%* | 18%* |
| tactics, tpWorth 3 | 15.0 | 58% | 7% | 35% | 21% |
| **tactics, tpWorth 4 (default)** | **14.4** | **55%** | **19%** | **25%** | **20%** |
| tactics, Rest 4, Final 5 | 14.3 | 43% | 31% | 27% | 16% |

\* 20 matches. Default on a second seed (200): 14.7 rounds, 47% nexus, 23% Final by card, 30% at
time, comeback 22%.

- The final fight does its job: the 39% of matches that ended on a score now end in a fight.
- The Final fight card is won by whoever plays it 97% of the time (the AI only plays a won one):
  it is an "end it now" button for a won position, which shortens the endgame. Making it cheaper
  cut comebacks to 16%.
- **No phases yet:** first kill still in round 1.5 with or without points; the points shape when
  cards are played, not when fighting starts. Rush back is the most-played card (3–7 per match).
- With the cheap first Rest the AI healed every other round and matches dragged (17.3 rounds).
- Interface: watched AI matches through a timed final fight and scripted human matches: no page
  errors.

## A quieter pulse: passives, rare abilities, a death phase (added 2026-11-21)

Owner, after playing: Q fires every round for almost everyone (23 of 25 Q's needed only "an ally
beside it", cooldown 2 pulses = once per round), so a pulse is a wall of casts and nothing stands
out. Wanted: abilities that are harder to set up and hit harder, so a cast is an event; passives;
dying at the end of the round with finishers that can end a broken champion early; less fighting
in round 1. Every change has its own switch in `RULES`.

- **Q becomes the passive** (`passives`). The Q slot holds one passive per champion (`PASSIVE`),
  never cast. Quiet ones change numbers (Bedrock: allies beside take 1 less; Headshot: +3 on a
  full-HP enemy; Ambush: +3 on an enemy with no ally beside it). Others move or decide things:
  Tailwind (Gale moves 3), Recoil (Gunner's hit pushes an adjacent enemy), Elusive (Wisp steps back
  after acting), Intervene (Thorn swaps in front of an ally about to break), Lifeline (Oriel heals an
  ally about to break to 5 HP), Coup de Grâce (Viper's basic attack finishes broken enemies).
  Growth paths lost their Q steps (replaced by +8 max HP).
- **Rare W and R** (`rare`). New patterns in `RARE`: a formation plus, often, one extra condition:
  ⚔ enemies close (`foes`), ≈ standing in the river (middle row), ➚ standing in the enemy half,
  ♥ an ally at half HP or less. W and R deal, heal and shield twice as much (`rareMult`) and rest
  4 and 6 pulses (`rareCd`; were 3 and 4). A rare cast is a headline in the caster feed and holds
  the board while it plays.
- **Death phase** (`deathPhase`). At 0 HP a champion is *broken*: it stays on its hex (still counts
  for its allies' patterns), takes no more damage, acts with half attack and casts nothing. At the
  end of the round (after the second pulse; in the final fight after every pulse) every broken
  champion still at 0 HP dies. An ally's heal before then saves it. Seven abilities are
  *finishers* (Ember W, Viper R, Gale W, Fang W, Wisp W, Flint W, Dusk R): they go for a broken
  enemy in reach and kill it on the spot, and a finisher that breaks a champion kills it outright.
- **Three opening cards** (`openCards`, `openBack`). Each team plays 3 cards; the two champions that
  played none start the fight beside their own nexus.

### Measured

AI vs AI, Small board, 840 matches per row (12 browsers × 70), same harness and seeds for both rows.
"Old" = all five switches off (the game as committed on 2026-11-21).

| | old | new |
|---|---|---|
| ability casts per pulse | 3.03 | **0.81** |
| pulses with no cast at all | 7% | **45%** |
| casts / basic attacks per match | 101 / 52 | 23 / 108 |
| rounds per match | 15.3 | 13.7 |
| deaths per match | 15.7 | 13.8 |
| damage in round 1 | 82 | **62** |
| first kill (round) | 1.51 | 1.66 |
| side A wins | 56%* | 49–50% |

\* the old row's 56% is one 420-match run; other old runs were near 50%.

- **Broken champions:** 13.5 per match. 1.5 are saved by a heal (about 1 in 9), 2.6 are finished
  early by a finisher, 11.2 die at the death check.
- **Each W fires 0.8–2.9 times per match, each R 0.6–1.5** (per champion that plays). No ability is
  dead. Getting there took two easing passes: the first rare set fired only 0.62 casts per pulse,
  and anything needing the enemy half fired under once per match because the AI seldom goes deep.
  Divers' W now need a close enemy instead. Most frequent: Thorn W, Wisp W, Oriel W, Cinder W (2.2–2.9).
- **Passives per match (all champions):** Spring 5.8, Omen 4.6, Refract 4.2, Bark 3.8, Cold Snap 3.4,
  Frostbite 3.3, Burning Core 2.3, the damage bonuses 1.0–1.5 each, Elusive 1.4, Lifeline 1.0,
  Dark Harvest 1.0, Intervene 0.6, Recoil 0.4, Coup de Grâce 0.3, Wildfire 0.2, Shatter 0.2.
  Shatter was "double damage against frozen" and fired 0.0 times (a frozen champion skips its action
  and the freeze clears right then); it is now "+2 against frozen or rooted".
- **Opening:** 3 cards alone changed nothing (round-1 damage 80 → 79: on the Small board the start row
  is two hexes from the middle anyway). Starting the champions with no card beside their nexus
  (`openBack`) is what brought it to 62.
- **Shields:** doubled W/R shields plus Bark stacked to +45 on one champion (seen in a scripted match).
  `shieldCap` keeps a shield at half max HP or less.
- **Balance:** four automatic passes on `TUNE` (840 matches each). Champion win rates 45–56%
  (were 33–68% right after the change).
- 12 scripted human matches (draft, 3-card opening, moves, regroup cards, level-ups, tactic cards),
  animations on: no errors.

**Not known yet.** Whether a broken champion reads clearly while the pulse runs, and whether a
pulse with one or two casts now feels like events or like nothing happening. The AI does not plan
around the death phase: it values a broken champion as nearly lost but does not move a healer in to
save one. "Comeback" as counted by `simulate` (swingBack) is 1–3% in both rule sets, so it cannot show
a change.

## Second playtest: Q back, a cinematic pulse, terrain, formations (added 2026-11-21)

Owner, after playing the quieter pulse: 0.8 casts per pulse sounded bad but plays well (teams group
up and boost each other's shapes). Asked for: a bigger board (slimmer side panels, a shorter top
bar), a fix for the level-up hover card that would not go away, Q back as a real ability (the
passive stays), more help from the interface while a pulse plays, a second element for weaker
champions, visible river / hills ("jungle" turned out to be only a role name), and an Easy-mode
helper that shows formations. Choices the owner made: medium Q, second element for the supports
plus the champions no pattern asked for, terrain for the zones that exist, formations computed for
the drafted team.

- **Layout.** The title is gone, the buttons share one short row with the meters, side panels are
  220 px wide (were 300) with thin HP bars and smaller portraits, and the board may fill the
  height of the window.
- **Level-up hover card.** Clicking a choice now also clears the "hovered" mark; the side panel's
  redraw used to bring the card back.
- **Q is cast again** (`qCast`, `qCd: 3`, patterns in `QPAT`). Each Q needs one specific spot or one
  ally of an element beside it, rests 3 pulses and keeps normal strength (W and R stay double). The
  passive moved in front of Q W R (✦). About half the Q's ask for an element, two per element,
  because the earlier easing had left only 8 element requirements in W and R.
- **Second elements** (`moreEls`, `SECOND_EL`): Oriel +Storm, Brook +Light, Ash +Shadow,
  Moss +Frost, Shiver +Storm, Pyra +Light, Umbra +Fire.
- **A cast plays in two beats.** Wind-up: the board leans in toward the caster (W and R only), the
  caster grows, its ability's name appears above it and the ability's chip glows in the side
  panel. Then the effect and its numbers. Basic beats are a little slower too (380 ms, were 220).
- **Status feedback.** Poison ticks are dark-green ☠ numbers; the side panel's poison, shield,
  freeze, knock-up and blind icons flash when they act (a tick, an absorbed hit, a skipped action, a
  miss).
- **Terrain.** Three generated textures (`assets/data/art/pattern_terrain.json`): water on the river
  row, rocky ground on the hills, a dark lair floor in the Dragon and Baron pits; each half is
  tinted in its team's colour; labels RIVER and HILL. Rules panel: a short "The board" section.
- **Formations (Easy).** After the draft the game searches shapes for your five champions
  (formation only, extra conditions named) and opens "Three shapes for your team": diagrams with
  portraits, lit letters, who stands in front / middle / behind. A 📐 Formations button in the
  opening hand reopens it; in Easy the opening card that lights the most right now is gold-bordered.

### Measured

AI vs AI, Small board, 840 matches. "Quiet" = the version before this section.

| | quiet | now |
|---|---|---|
| ability casts per pulse | 0.81 | **1.44** |
| pulses with no cast | 45% | 24% |
| casts / basic attacks per match | 23 / 108 | 42 / 84 |
| rounds per match | 13.7 | 13.8 |
| damage in round 1 | 62 | 67 |
| side A wins | 49–50% | 51% |
| champion win rates | 45–56% | **43–55%** |

- **Q per champion per match: 1.0–2.8** (W 0.7–3.0, R 0.4–1.4). The first medium Q set gave 1.67 casts
  per pulse with Q ranging 0.8–5.1: champions behind the line nearly always have someone in front,
  front-line melee rarely has anyone behind or beside. Front-line melee went back to "any ally beside
  it"; the back line got single hexes (front-left only, right only) or an element at a given side.
- Broken champions 13.7 per match: 1.65 saved, 2.5 finished early, 11.2 die at the death check.
- Six balance passes on `TUNE`. One pass was spoiled: a harness browser crashed (it printed an error
  in a character set Windows could not write), the tuner ran again on the previous pass's numbers
  and overshot (32–63%); the harness now stops a pass when any browser fails, and three clean passes
  followed.
- 6 scripted human matches (3 Easy with the formations panel, 3 Normal): no errors. The old rules
  (every new switch off) still run.

**Not known yet.** Whether the wind-up rhythm (≈0.85 s wind-up + 1.3 s effect for W/R) is too slow
once you know the champions; Speed: fast halves it. Whether the formation shapes are reachable in a
real match: they ignore the board edge, towers and the enemy.

## Look and feel: from web page to game (added 2026-11-21)

Owner: make the interface stop looking like a generic website; free hand on fonts, colours and
images. Direction chosen to match the champion sprites (chunky chibi cartoon): a painted twilight
valley behind everything, panels framed in dark bronze with a gold edge, Lilita One (a chunky
title face) for names, numbers and buttons, Nunito (rounded) for text. Team colours stay sky blue
and ember red.

- **New art** (`assets/data/art/pattern_ui.json`, all first try except one re-cut): the backdrop
  (a valley with a stone arena in the middle), a crest (gold medallion with a violet eye), six
  element gems that replace the element emoji everywhere. The generator learned a `backdrop`
  kind (16:9, 1600 px JPEG).
- **Title screen** on load: crest, AUGURY logo, Play / Watch / How to play, difficulty.
- **Scoreboard** instead of the meter row: crest, Blue and Red nexus HP as bars around a round
  medallion (which also names the phase), tactic points as diamond pips.
- **Drawn stat icons** (attack, range, initiative, armour, leap, HP) replace the emoji on champion
  cards and hover cards; chance and status icons are still emoji.
- **Result banner** at the end: Victory (gold) / Defeat (silver) / "Red wins!" when watching, how
  the match ended, kills per team, Play again / See the board / Main menu.
- Chunky bevelled buttons, gold-trimmed panels, broadcast-style caster lines, Rules in two columns.
- Checked by screenshots at 1500×900 and 1280×720; 4 scripted human matches with no errors; the
  rules are untouched (headless numbers unchanged).

## Spread patterns: abilities that pull a team apart (added 2026-11-22)

**Owner's diagnosis.** With a good draft the most profitable shape is a ball: nearly every pattern asks
for an ally in one of the six touching hexes, so five champions standing together light most abilities
and never need to move. Wanted: harder, more exact patterns (a specific hex, two out, of an element)
so the best shape lights only 5–6 abilities, cooldowns force the team into a different shape, and
abilities and passives that move champions make that rotation a series of decisions.

**What is in** (switch on the draft screen, "Patterns: spread (10 champions)"; `RULES.spread`):
- Twelve new named cells two hexes out (`A2` straight ahead, `FR2`, `FL2`, `FRR`, `FLL`, `R2`, `L2`,
  `BRR`, `BLL`, `BR2`, `BL2`, `H2` straight behind), usable in any pattern, plus `free`: hexes that
  must be empty. The board guide marks wanted hexes with ability letters and must-stay-empty hexes
  with ✕; the small pattern diagrams grow a second ring when a pattern reaches two out.
- Ten champions, two per role (Anchor, Bulwark, Ember, Viper, Lens, Rime, Ranger, Gunner, Oriel,
  Seer) with new W and R patterns in `SPREAD`. Q keeps its medium pattern. W: one exact neighbour
  (often of an element) and one or two neighbours left empty. R: one exact ally two hexes out (often
  of an element) and one empty hex. Bans are off in this mode (two champions per role).
- `pullTo`: a cast that calls the nearest ally within 3 into a named cell, to set up someone
  else's pattern (Anchor W to the hex two behind it, Bulwark W two to its right, Seer W two front
  left); some W/R also step after casting (Ember R forward, Lens W back, Ranger W aside).
- Measuring tools: `patternAudit()` (for every team of the current roster, on an empty board: the
  formation that lights the most abilities, then how many new ones one, two and three moves light
  once those are spent); `simulate()` now reports patterns complete per team per pulse, abilities
  ready, allies beside each champion and mean distance between teammates.
- Two switches for measuring: `RULES.movesPerTurn` (champions moved before each pulse) and
  `RULES.aiProgress` (the AI values a half-built W or R pattern).

**Measured.** Audit: 32 teams each; matches: AI vs AI, small board, 120–200 per row.

| | Best formation lights (of 15) | Q / W / R in it | Patterns complete per team, per pulse | Allies beside a champion | W and R casts per match | Ended by nexus | Rounds |
|---|---|---|---|---|---|---|---|
| Standard patterns, 25 champions | – | – | 3.2 | 1.30 | 23.4 | 72% | 14.0 |
| Standard patterns, same 10 | 12.0 (10–14) | 4.0 / 3.7 / 4.3 | 3.3 | 1.27 | 25.0 | 70% | 14.2 |
| Spread, first draft (two exact allies two out per R) | 6.9 (6–8) | 4.0 / 2.1 / 0.8 | 2.1 | 1.08 | 13.4 | 34% | 16.5 |
| Same, two moves per turn | – | – | 2.2 | 1.06 | 14.3 | 71% | 14.4 |
| **Spread, final (one exact ally two out per R), two moves** | **7.8 (7–9)** | **3.9 / 1.6 / 2.2** | **2.4** | **0.95** | **17.6** | **82%** | **13.5** |

- The diagnosis holds on paper: with standard patterns the best shape lights 12 of 15 (up to 14).
  In AI play, though, teams complete only about 3 patterns per pulse: the AI never builds the ball,
  a human would.
- The first spread draft met the 5–6 target almost (6.9) but six R's need two exact allies two out,
  and those almost never came together in play (0.0–0.1 casts per match for Anchor, Bulwark, Ember,
  Gunner, Oriel, Rime R; Oriel W never). W and R casts halved, damage fell, matches stalled (34% end
  by nexus). Teaching the AI to value half-built patterns spread teams out further but completed
  fewer (2.1 → 1.8 at the strongest weight): with one move per turn and enemies pushing and killing
  in between, a pattern that needs two or three exact hexes rarely finishes.
- Two moves per turn fixed the pace (71–82% by nexus) but not the patterns on its own.
- The final draft (one exact ally two out per R) has no dead R (every R 0.5–2.1 casts per match;
  R casts 10.0 per match against 9.5 standard), teams stand further apart (0.95 allies beside a
  champion against 1.27), sides even (52%), comebacks 19%. The ceiling rose to 7.8 because R's got
  easier, and W's are now the harder ability in practice (7.6 casts per match against 15 standard),
  the wrong way round.
- Rotation: once the best formation's abilities are spent, one move lights about 1.3 new ones, two
  moves 2.3, three 2.8 (standard: 1.0 / 1.8 / 2.1).
- Champion win rates in the final draft spread 31–69% (Gunner 69%, Ranger 31%): no balancing yet.
- The interface works with the new cells (draft switch, board guide, diagrams); one watched match
  through four rounds, no errors.

## R patterns that loosen as champions level (added 2026-11-22)

**Owner's idea.** A level-up can also loosen a requirement: an R that is nearly impossible at the start
becomes easy at level 3 or 4, so a champion you invest in turns from unusable to devastating, and the
growth shows in the geometry. Owner's answers to the previous round: a best position of about 8 lit
abilities is fine (towers make the real board harder); keep two moves per turn for now (owner's
intuition: one move was simpler and more rewarding, and movement passives could replace the second
move later; some champions, like tops, could be slower); measure loosening first, then balance.

**What is in** (spread mode, `RULES.loosen`, `loosenAt: [2, 4]`): each R has three stages in `R_STAGES`.
Strict at the start (two allies, one of them two hexes out, the first spread draft), medium at
champion level 2 (one exact ally two out and an empty hex, the version that played well), loose at
level 4 (a plain ally two out). The AI's level choice values a level that loosens an R
(`loosenValue`). The hover card says when the R loosens and what it will need; the draft card says
"R loosens at levels 2 and 4"; the log announces each loosening. Balance factors for the ten in
`RULES.spreadTune`.

**Measured** (AI vs AI, small board, two moves per turn; audit over 32 teams):

| | Best formation lights (of 15) | R casts early / mid / late, per match | W / R casts per match | Ended by nexus | Rounds | Champion win rates |
|---|---|---|---|---|---|---|
| R fixed at medium (previous) | 7.8 | 4.9 / 3.6 / 1.6 | 7.7 / 10.1 | 81% | 13.6 | 33–68% |
| **Loosening at levels 2 and 4** | **7.0 strict · 7.8 medium · 9.0 loose** | **2.4 / 3.1 / 2.1** | **9.3 / 7.7** | **73%** | **14.3** | **42–58%** |
| Loosening at levels 3 and 5 | | 0.8 / 1.5 / 1.6 | 11.9 / 3.9 | 65% | 15.3 | 41–60% |

Early = rounds 1–6, mid = 7–12, late = 13 and later (fewer matches reach the late rounds, so the
late figure understates the rate per round). About 8 of the 10 champions reach the medium stage in a
match, about 1.4 the loose one.

- The arc the owner wanted shows: R casts move from the start of the match toward the middle and end.
- Loosening narrows balance on its own (33–68% to 42–58%): the champions whose R was too easy early
  are held back until they grow.
- The ceiling grows with the stages (7.0 → 7.8 → 9.0); fully loosened teams are rare.
- Levels 3 and 5 are too slow: R becomes rare and matches stall.

**Balance** (three passes of 450 matches, each champion's factor nudged toward 50%): 42–58% → 46–54%.

| | Anchor | Bulwark | Ember | Viper | Lens | Rime | Ranger | Gunner | Oriel | Seer |
|---|---|---|---|---|---|---|---|---|---|---|
| Factor before | 0.95 | 1.30 | 1.00 | 1.30 | 1.20 | 1.35 | 0.75 | 1.20 | 0.65 | 1.60 |
| Factor after | 0.92 | 1.34 | 1.06 | 1.23 | 1.01 | 1.59 | 0.87 | 1.02 | 0.69 | 1.51 |
| Win rate after | 48% | 52% | 54% | 46% | 50% | 50% | 46% | 54% | 50% | 50% |

With two champions per role, every champion plays every match, so each pass measures all ten at once.
Team A won 43–57% between seeds of 150 matches: noise at that size.

## W's that loosen with time, and maneuvers instead of a second move (added 2026-11-22)

**Owner's ideas.** R loosens only through the level-ups a player chooses; W's could loosen by themselves
as the match goes on. And instead of a second move per turn (too much elasticity: one move was
simpler and more rewarding), a passive-like reward that lets an ally move or two champions swap.

**What is in** (spread mode, switches in `RULES`):
- `wLoosen`, `wLoosenRounds: [5, 10]`: every W loosens for both teams at round 5 (its empty hexes are
  no longer needed) and round 10 (any ally will do, no element). `wStart: 1` would start at the middle
  stage. The log announces each loosening.
- `maneuver`: a team that fired a W or R in a pulse gets a maneuver on its next turn, after its move:
  one champion steps one hex, or two allies side by side swap places ("Skip the maneuver" is allowed).
  During a maneuver, clicking the ally beside the selected champion swaps them. The AI plans
  maneuvers like moves.

**Measured** (AI vs AI, small board, R loosening on, 200 matches each, same seeds):

| | W casts early / mid / late | W / R casts per match | Patterns complete per pulse | Maneuvers per match | Ended by nexus | Rounds | Comeback | Win rates |
|---|---|---|---|---|---|---|---|---|
| A. Two moves per turn | 4.1 / 3.8 / 2.1 | 10.0 / 7.6 | 2.43 | – | 74% | 14.8 | 30% | 47–54% |
| B. Two moves + W loosening | 5.3 / 7.8 / 4.3 | 17.4 / 6.8 | 2.86 | – | 77% | 14.5 | 22% | 37–64% |
| C. One move + maneuver | 3.4 / 4.0 / 2.3 | 9.7 / 7.9 | 2.37 | 25 offered, 20 used, 4 swaps | 62% | 15.6 | 27% | 46–54% |
| **D. One move + maneuver + W loosening** | **4.6 / 8.2 / 5.5** | **18.3 / 7.0** | **2.90** | **26 offered, 20 used, 4 swaps** | **70%** | **15.2** | **25%** | **47–53%** |

- A maneuver gives about the same pattern building as a second move (patterns complete 2.37 against
  2.43; W and R casts equal) with less raw movement: matches run about a round longer (62% end by
  nexus against 74%). About four in five maneuvers are used, mostly as steps; swaps are about one in
  five.
- W loosening makes W the mid-game ability: W casts nearly double (10 → 17–18 per match), most of the
  rise in rounds 7–12. It also raises the pace again (D: 70% by nexus).
- D (one move, maneuvers, W loosening) keeps the balance (47–53%) and comebacks (25%); B threw the
  balance off (Ember 64%, Viper 37%), so loosening W with two moves would need a new balancing pass.
- Not yet measured: maneuvers from passives tied to specific champions (here every W or R earns one),
  and slower tops.

## The new default: spread patterns for all 25 champions (added 2026-11-22)

**Owner's call.** Make spread patterns, one move per turn, maneuvers, W loosening (rounds 5/10) and
R loosening (levels 2/4) the default, and give the other fifteen champions spread abilities too.

**What changed.**
- All 25 champions have spread W/R patterns in the `SPREAD` table, and their R's have three stages in
  `R_STAGES`. Each champion's patterns lean toward one shape family, so different drafts want different
  geometry: line (Cinder, Shiver, Flint, Ash), spread or lane (Gale, Wisp, Umbra), tight (Thorn, Hail,
  Brook, Moss), wedge (Fang, Pyra), column (Quartz), alone (Dusk).
- `RULES` defaults: `spread`, `movesPerTurn: 1`, `maneuver`, `wLoosen`, `loosen` are all on. The full
  25-champion roster and bans are back (`spread10` keeps the old ten-champion pool).
- The draft button "Patterns: spread/standard" still switches back to the old patterns.

**Do drafts want different shapes?** I searched for the best formation in 200 random drafts
(`patternAudit`), at each loosening stage, and classified it. Ball means most champions touch two
allies, spread means almost none touch one, line means one row three or more wide:

| Best formation is a… | Standard patterns | Spread, early | Spread, mid | Spread, late | Spread + Q family switch |
|---|---|---|---|---|---|
| ball | 96% | 61% | 59% | 48% | 49 / 42 / 49% |
| line | – | 11% | 5% | 6% | |
| spread | – | 7% | 10% | 7% | |
| mixed | 4% | 21% | 25% | 39% | |

So the answer is yes, partly. A ball is still the most common best formation, but four drafts in ten
want a line, an open spread or a mixed shape, and the share grows as patterns loosen. Q's are still
"ally beside", and that is the main thing pulling teams back into a ball. `RULES.qFamily` gives each
Q its champion's family shape. It cuts the ball share by about ten points, but it is off for now.

**Balance.** I ran five balancing passes (600 matches each) that nudge a per-champion factor
(`spreadTune`), then averaged the last three passes to cancel out noise. A check run of 1,000 matches
on new seeds:
- Win rates are 41–56%, and 24 of 25 champions fall between 45% and 56%. Seer is the outlier at 41%,
  even with the largest factor (1.95). Its spread patterns need a redesign; more stats will not fix it.
- Sides are 48–53%. 69–79% of matches end by nexus, in 13.7–14.6 rounds.
- Per match: 25.6 W and 8.0 R casts, and 28 maneuvers offered, 21 used and 6 of them swaps (30-match smoke test).

## Reflexes, Q shapes and geometry icons (added 2026-11-22)

**Owner's direction.** Q requirements stay easy but not all the same, so the Q-shape switch is now on. Build
maneuvers that fire on conditions, and invent many more triggers than the examples given. Show each player
the geometry their abilities want, with icons, highlighted numbers and ghosts. Slower tops are left for later.

**Q by shape family** (`RULES.qFamily`, now on). Every Q is still one easy condition, but it points the
same way as its champion's W and R:
- Line champions want an ally at the side.
- Tight champions want any ally beside them.
- Spread champions want nobody beside them.
- Column champions want an ally in front or behind.
- Wedge champions want one exact corner.

**Seer redesigned.** Its W and R needed a Frost and a Fire ally, which few drafts had, and it won 41% at the
largest balance factor. It is now a back-line column champion:
- W needs an ally in front and both back hexes empty. It calls an ally two hexes ahead.
- R needs an ally two hexes ahead (strict: also one at front right; loose: nothing else).

**Reflexes** (`RULES.reflex`, `REFLEX`). Each champion has one reflex: when its trigger happens, it moves
by itself in the middle of the pulse, as soon as the action that set it off is over. A reflex move can set off
other reflexes, which makes chains: Gale's Slipstream reacts to any reflex beside it. Reflexes loosen with the
W's. At round 5 the radii grow by one and the HP thresholds rise. At round 10 "fires a W" becomes "fires any
ability", and a champion can react twice per pulse. Each reflex move plays as its own beat: the board zooms,
the reflex name shows large, and the champion's reflex chip glows.

Trigger types built (19), with the champion that has each:

| Trigger | Champions |
|---|---|
| an ally nearby fires a given ability (or an ability of an element) | Lens (any R, anywhere), Seer (a W beside it), Cinder (a Q beside it), Pyra (a Fire ability) |
| an enemy nearby fires W/R | Quartz, Dusk |
| it breaks an enemy · an ally nearby breaks one · an enemy nearby is broken | Ember · Flint · Viper |
| an ally nearby is broken | Oriel (goes to it), Thorn (goes for whoever broke it) |
| an enemy ability hits it · it loses 6+ HP in one hit · it drops under 30% HP | Wisp · Gunner · Umbra |
| an enemy ability hits an ally nearby · an ally nearby drops under half HP | Bulwark · Anchor, Brook |
| an enemy nearby drops under half HP | Fang |
| it freezes/roots an enemy · an ally nearby does | Rime · Shiver |
| an ally heals or shields it | Ash |
| an ally nearby makes a reflex move | Gale |
| a pulse starts with an enemy beside it · with 2+ enemies within 2 · with no ally beside it | Ranger · Hail · Moss |

Responses (6): step to the hex where the team lights the most, swap with that ally, step toward it, dash two
hexes toward it, step away from the enemies, call the ally whose step lights the most. Where the response
picks a hex, it picks the one that lights the most for the team, so reflexes build patterns. The AI plays out
the pulse when it chooses a move, so it already counts the reflexes its move sets up.

**More triggers for later** (not built; the design space is much larger than 19):
- *Abilities:* an ally's ability hits 3+ enemies; an ally's R finds no target; two allies fire at the same
  enemy; its own R fires; its W comes off cooldown; an ally's pattern becomes complete; two abilities of one
  element fire in the same pulse; an enemy is silenced.
- *HP and damage:* it took no damage for a whole pulse; it starts a pulse at full HP; an ally's shield breaks;
  it dodges; it crits; it misses (blinded); poison ticks on it; it is the lowest-HP champion on the board.
- *Kills:* first blood; a shutdown (a bounty champion falls); two breaks in one pulse; an ally is saved from
  broken; a tower falls; the nexus is hit; the Dragon or Baron is hit or slain.
- *Statuses:* it thaws; its root ends; an enemy beside it is frozen; an ally is knocked airborne.
- *Position:* an enemy enters its range; an enemy steps behind it; it starts a pulse on the river or a hill;
  it is left alone after a death; an enemy makes a reflex move (counter-reflexes); it is pushed or pulled.
- *Timing:* it acts first or last in the pulse; a pulse with no cast; the last pulse of a round; it levels
  up; its team is two champions down; its team lights 6+ abilities.
- *More responses:* push or pull an enemy, rotate the allies around it, shield the ally that set it off,
  shave a cooldown, fire its Q out of turn, leap over a champion.

**Geometry icons.**
- *Shape family badge* next to every champion's name on the draft tiles, team cards and hover cards (line,
  tight, spread, column, wedge), with the sentence "Wants: …".
- *Numbers on reachable hexes:* with a champion selected, each hex it can move to shows how many abilities the
  team would light there: +2 green, +1★ gold (a ready W or R lights), −1 red (fainter).
- *Ghosts:* every pattern the champion has not lit yet marks its needed hex with a dashed outline in the
  element's colour and the ability letter (W⚡ = a W that needs a Storm ally). If an ally of the right element can
  reach that hex this turn, its ghost stands there with a dashed arrow from where it is now. A hex that must be
  empty and is not gets a red ✕. Hovering a destination shows the ghosts as they would be from there.
- *Hovering an ability chip* on any team card shows that one ability's ghosts, for any champion.
- *Reflex chip* (trigger icon › response icon) on each card. Its hover text gives the rule as it stands now and
  how it loosens.

**Measured** (AI vs AI, small board, 200 matches per row, same seeds):

| | Ended by nexus | Rounds | Comeback | W / R per match | Reflexes per pulse (early / mid / late) | Chains per match |
|---|---|---|---|---|---|---|
| A. Before (Q mostly "ally beside", no reflexes) | 75% | 13.9 | 19% | 22.3 / 6.7 | – | – |
| B. Q by shape family | 77% | 14.3 | 17% | 24.2 / 7.5 | – | – |
| **C. Q shapes + reflexes (default)** | **85%** | **13.7** | **17%** | **22.6 / 7.6** | **0.62 / 0.93 / 0.99** | **2.8 (longest 4)** |
| D. Q shapes + reflexes, no generic maneuver | 73% | 14.7 | 21% | 23.6 / 7.6 | 0.53 / 0.89 / 1.00 | 3.1 |

- About 22 reflexes per match. They grow toward the end, as intended: 0.6 per pulse early, about 1 late.
  Two or three chains per match, the longest 4.
- Reflexes speed matches up (85% end by nexus, against 77% without), because they finish what a move started.
- Q shapes changed the balance: the spread champions (Umbra, Dusk) and the old Seer fell to 26–34%. After the
  Seer redesign and four balancing passes (gain lowered to 0.6 to cut the swinging), averaged, a check run of
  1,000 matches on new seeds gives win rates of **42–57%** (Bulwark lowest, Dusk and Moss highest), Seer 48%,
  sides 47–57% per 250-match seed, 80–84% by nexus, about 13.6 rounds.
- D (reflexes replacing the generic maneuver) gives slightly better comebacks (21%) but slower matches.
  The default keeps both, because the maneuver is the player's own choice and reflexes are automatic.

## What an ability does, in icons and diagrams (added 2026-11-22)

**Owner's note.** Most of the icons said what an ability *needs*; what it *does* was only in the text. The owner
wants icons (attack shown as a sword), placeholder ability graphics for now, and to see at a glance whether an
ability hits one enemy or several, and how far it reaches compared with the champion's range.

**What is in.**
- **Two columns per ability** on every hover card (team cards and draft): *NEEDS* (the pattern diagram and a
  short sentence) and *DOES* (an effect diagram and an icon strip). The full sentence stays below, in small print.
- **Icon strip:** who it hits (🎯 one enemy, ✸ one plus its neighbours, ◎ everyone around it, ➶ a beam,
  ⁂ up to N, ⇉ a dash, ⤺ a pull, ✚ allies), ↔ reach in hexes, 🗡️ damage as a number (not "×attack"),
  ✚ heal, 🛡 shield, status icons with chances, ☠ poison, 💨 knock-back, 👣 a step after, ⇠ calls an ally,
  ⇄ a swap first, 💀 finishes broken enemies. Each icon has a hover text.
- **Effect diagram:** the champion (white) in a radius-3 hex field facing the enemy; tinted hexes = what it can
  reach; red = enemies hit (orange = splash, green = allies); a yellow arrow = a dash, a violet arrow = a pull.
- **Ability emblems** (placeholders): a coloured disc with a glyph per kind of ability (fire, frost, lance, slam,
  quake, execute, volley, cannon, twist, sanctuary). Painted art can replace them later.
- **Chips** on the team cards add a small "who it hits" glyph after the pattern.
- **Hovering a chip** on the board: the hexes it reaches are tinted, and the champions it would hit right now
  get a dashed target ring (or "no target in reach now").

**How reach works today** (for the owner's question):
- A basic attack and a single-target ability hit within the champion's range only. A range-1 champion hits
  only the hexes next to it.
- Dashes (Ember W, Viper R, Fang W, Wisp W, Dusk R) reach range + 2–3. Hooks (Cinder W, Thorn W) pull an enemy
  from range + 2. Areas (Bulwark R, Ember R, Cinder R, Shiver R) hit everyone within 2.
- **Anchor, Gale and Moss cannot reach past the hexes next to them at all** (Gale's R hits "up to 4 in range",
  which at range 1 means only neighbours).

## What the numbers cannot tell

- Whether finding shapes is fun or fiddly. The AI looks one move ahead and values a complete
  pattern, but it does not plan a formation.
- Whether 15 patterns per team are readable in play.
- Whether the regroup phase feels like a welcome reset or an interruption.

## Open questions for the owner

1. Regroup lead: two champions (3.7 regroups per match) or three (2.6)?
2. Should the losing team get more than a retreat, e.g. a reason to contest the Baron?
3. Patterns are mirrored for team B (its "left" is the mirror image of team A's). Fine for a
   human playing A; needs a decision before two humans play.
4. Gold is out. If it returns, as what?
5. Hard difficulty: a stronger AI needs a different idea (deeper look-ahead, better evaluation),
   or an honest handicap (the AI drafts better, extra level). Which?
6. ~~Fortify~~ cut 2026-11-20 (owner). The losers now choose Rush back or Rest.
7. Formation cards cost ~10% of nexus finishes: owner accepts the slower pace for now (2026-11-20).
8. ~~Final fight card~~ cut 2026-11-21 (owner): it ended won positions (97% won by its player).
   `RULES.finalCard` keeps it for measuring. Only the timed final fight remains.
   After the cut (150 matches): 14.8 rounds, 67% by nexus, 33% timed final fight, comeback 18%.
9. Phases (a quiet opening): owner decided not to pursue for now (2026-11-21).
