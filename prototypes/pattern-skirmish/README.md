# Prototype: pattern skirmish

**Hypothesis.** The auto skirmish flows better if (1) where your five champions stand relative
to each other decides *which abilities fire*, so there is a shape to search for and a reason to
keep changing it, and (2) a decisive fight is followed by a short *regroup* phase in which both
teams re-place their champions, so the walk after a won fight is skipped and the winner makes a
clear choice: Baron, Dragon or tower. Owner's ideas, 2026-11-19, after a review of
`prototypes/auto-skirmish/` (which is left untouched and is still the main line).

**How to run.** Open `index.html` (keep the `art/` folder next to it). No server, no libraries.
`simulate(n, 'small', seed, rules)` in the browser console runs AI-vs-AI matches.

**Status.** In progress. First playable 2026-11-19. Not yet played by the owner.

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
