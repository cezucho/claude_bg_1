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

## The shipped roster — fifteen champions, three per role

Ten champions shipped on 2026-10-04. On 2026-10-08 the owner asked for fifteen, so the draft
has real choices, built in groups that work well together. The five new ones are marked 🆕.
The data is generated by `design/v2-champions/roster_v2.py`, which:
- checks every opening on Field 7 with solid towers;
- writes `assets/data/champions/*.json`, `spells.json` and `synergies.json`.

The roster follows the same rules as before:
- one sentence per champion;
- three different verbs;
- exactly one opening that casts — one cast usually, two sometimes (Viper, Ranger, Gunner,
  Talon), three rarely (Tempest).

| Champion | Role | How to play it | Q · W · E (effects) | Opening attack |
|---|---|---|---|---|
| **Anchor** | Top | Pins one enemy in place so the team can collapse on it. | Stand Firm (shield, unstoppable) · Chain Hook (damage, displace, root) · Groundswell (damage (+3 vs rooted)) | Chain Hook: move top forward-right → move top forward-right → cast top w |
| **Ember** | Jungle | Sets enemies alight so every action they take hurts — then cashes in. | Kindle (burn) · Switch (swap) · Wildfire (damage (×2 vs burning)) | Kindle: move jungle forward-right → move jungle forward-right → cast jungle q |
| **Lens** | Mid | Marks one target for the whole team, and reshapes the board with walls. | Mark (damage, mark) · Prism Wall (wall) · Lance (damage) | Lance: move mid forward-left → cast mid e → move mid back-right |
| **Oriel** | Support | Keeps one ally standing, and pulls it out of trouble. | Mend (heal, cleanse) · Tether (pullally, shield) · Sanctuary (heal, shield) | Mend: move support forward-left → cast support q → beacon support 1 |
| **Bulwark** | Top | Holds the ground in front of its tower: shoves enemies off it and walls the way. | Shove (damage, displace (slam 2)) · Rampart (wall) · Quake (damage, displace) | Quake: move top forward-right → move top forward-right → cast top e |
| **Viper** | Jungle | Hunts the isolated: poisons a target, then dives on it. | Lunge (dash) · Fang (damage, poison) · Ambush (damage (+3 vs poisoned)) | Fang: move jungle forward-left → cast jungle q → cast jungle w |
| **Tempest** | Mid | Throws the enemy line around: knockbacks that set up its allies. | Zap (damage) · Gust (damage, displace (slam 2)) · Eye of the Storm (damage, displace) | Eye of the Storm: cast top q → cast jungle q → cast mid q |
| **Ranger** | Bottom | Shoots from the back line and cashes in the marks its team sets. | Volley (damage (+2 vs marked)) · Pin Shot (damage, root) · Rain of Arrows (damage) | Volley: move bottom forward-right → cast mid q → cast bottom q |
| **Gunner** | Bottom | Bursts from safety, hardest on a target that can't move. | Snipe (damage (+3 vs rooted)) · Recoil (dash) · Buckshot (damage, displace (slam 2)) | Snipe: cast top w → cast bottom q → move bottom forward-right |
| **Bastion** | Support | Shields the team and drains the dive. | Aegis (shield) · Rally (heal, unstoppable) · Judgement (damage, exhaust) | Judgement: move support forward-left → move support forward-left → cast support e |
| **Briar** 🆕 | Top | Roots an enemy in thorns and lets the poison, and the team, finish it. | Entangle (damage, root, poison) · Thornwall (wall) · Bloom (damage (+3 vs poisoned)) | Entangle: move top forward-right → cast top q → move top back-left |
| **Talon** 🆕 | Jungle | Dives onto whatever its team has marked, and doubles down on it. | Rend (damage, mark) · Pounce (dash) · Execute (damage (×2 vs marked)) | Execute: move jungle forward-right → cast jungle q → cast jungle e |
| **Pyre** 🆕 | Mid | Spreads fire along the enemy line, then burns it down with Ember. | Scorch (damage, burn) · Smoke (damage, exhaust) · Inferno (damage (×2 vs burning)) | Scorch: move mid forward-right → cast mid q → beacon mid 2 |
| **Mortar** 🆕 | Bottom | Shells enemies from far back, into walls, towers and each other. | Shell (damage, displace (slam 3)) · Tripwire (wall) · Barrage (damage) | Shell: cast bottom q → move bottom forward-left → move support forward-left |
| **Seer** 🆕 | Support | Calls the target: marks and binds one enemy so the whole team knows where to hit. | Omen (mark) · Bind (damage, root) · Revelation (damage, mark, exhaust) | Omen: move support forward-left → cast support q → move support back-right |

### Synergy groups

Each group is a plan a team can draft toward. Most groups span three or more roles, so a team
can actually assemble one. Every champion belongs to at least one group, and some sit in two
— Briar, Seer, Lens, Ranger, Anchor, Bulwark, Mortar, Talon and Viper — so drafts can pivot.

| Group | Members | What they do together |
|---|---|---|
| **Lockdown** | Anchor (Top), Briar (Top), Seer (Support), Ranger (Bottom), Gunner (Bottom) | Root a target, then hit what can't move: Snipe and Groundswell add 3, and a rooted target slams when pushed. |
| **Called Shot** | Lens (Mid), Seer (Support), Talon (Jungle), Ranger (Bottom) | Mark one enemy and spend the mark: Execute doubles on it, Volley adds 2, and every hit adds the mark. |
| **Wildfire** | Ember (Jungle), Pyre (Mid) | Set the line alight; Wildfire and Inferno deal double to burning targets. |
| **Venom** | Viper (Jungle), Briar (Top) | Poison first; Ambush and Bloom add 3 to poisoned targets. |
| **Crush** | Bulwark (Top), Tempest (Mid), Mortar (Bottom), Lens (Mid) | Walls and pushes: shove enemies into walls, towers and each other — a stopped push slams for extra damage. |
| **Dive and Retrieve** | Viper (Jungle), Talon (Jungle), Oriel (Support) | Divers go deep for the kill; Oriel's Tether and Mend bring them back out. |
| **Hold the Line** | Bastion (Support), Bulwark (Top), Anchor (Top), Mortar (Bottom) | Stand in front of your towers: shields, unstoppable front-liners and long-range fire behind them. |

**New mechanics added to make the groups real** (D-049):
- **Slam.** A push that is stopped short deals extra damage. It can be stopped by a wall, a
  tower, a champion, the edge of the board, or a root. Shove, Gust and Buckshot slam for 2;
  Mortar's Shell slams for 3. Unstoppable targets aren't moved and never slam. This is what
  makes walls (Rampart, Prism Wall, Thornwall, Tripwire) and pushes a team plan. It also links
  Crush to Lockdown: a rooted target always slams.
- **Bonuses against marked and exhausted targets.** Talon's Execute doubles on a marked
  target, and Ranger's Volley adds 2. The bonus is checked before the hit spends the mark.

**Synergy score** (data, not rules): for each group, count the pairs of its members on one
team. For example, Anchor + Seer + Gunner in Lockdown is 3 pairs. The draft screen shows each
team's score. The AI drafter builds toward groups, and the harness uses the score to ask
whether the better draft wins.

**Cross-champion casts** (the owner's revision) appear in these openings, among others:
- Tempest's Eye of the Storm fires the top's, the jungle's and the mid's Q.
- Ranger's Volley fires the mid's Q, then its own.
- Gunner's Snipe fires the top's W before its own Q.
- Viper's Fang fires its own Q (Lunge) and then W.

Which ability fires depends on who was drafted into that role. That ties the opening to the
draft, as the owner wanted.

**A cooldown-0 ability fired in the opening still costs round 1 (D-044).** Otherwise Zap and
Volley would be free.

### Every opening plays from the starting line

`tools/Augury.Tools openings` reports **60 of 60** playable on Field 7, with towers walkable and
with towers solid.

## First measurements — ten champions (self-play, 200 matches, random drafts, after the balance pass D-047)

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

## The draft experiment — fifteen champions (2026-10-08, 400 matches per scenario)

The owner's question: with more champions and groups that work together, how much does the
draft decide? Three scenarios, all with the same AI playing the match, on Field 7:

| Scenario | How each team drafts | What it tests |
|---|---|---|
| **random** | both at random | the spread of champion strength; whether a higher synergy score wins |
| **synergy** | both build toward groups | both drafts "good" and roughly equal — what decides then |
| **mixed** | one team builds toward groups, the other drafts at random (sides alternate) | a clearly better draft against a worse one |

Run it with `tools/Augury.Tools selfplay 400 draft=random|synergy|mixed`.

### Results

| | random | synergy | mixed |
|---|---|---|---|
| A / B wins | 53 / 47% | 56 / 42% | 57 / 43% |
| Mean synergy score A / B | 3.1 / 3.2 | 7.2 / 5.9 | 5.1 / 4.8 |
| Rounds (mean) | 13.8 | 13.1 | 13.8 |
| Champion win rates | **28–65%** | 36–64% | 41–56% |

**1. Champion strength spreads out with fifteen, as the owner expected.** In random drafts:
- the range is 28–65%, against 42–58% for the ten after the balance pass;
- Mortar is the outlier at 28%: it starts at SPD 1 and its own Barrage molds speed down, so it often reaches SPD 0 and can't move at all;
- Gunner is the strongest at 65%.

Adding champions undid the balance pass, so tuning numbers now would be wasted work. The
owner is right about that.

**2. The synergy score alone does not predict who wins.** In random drafts the team with more
synergy pairs won *less* often: +2 → 35%, +3 or more → 42%. In the mixed scenario the synergy
drafter won only **48%**, despite an average edge of +4.5 pairs.

**3. Which group a team builds matters, not how many pairs it has.** In both scenarios where
teams build toward groups:

| Group | random | synergy | mixed |
|---|---|---|---|
| Lockdown | 51% | 56% | 52% |
| Called Shot | 51% | 54% | 49% |
| Venom | 43% | 61% | 58% |
| Wildfire | 52% | 60% | 42% |
| Dive and Retrieve | 55% | 36% | 53% |
| Crush | **42%** | **43%** | 48% |
| Hold the Line | **42%** | **42%** | 44% |

- **Lockdown and Called Shot win.** Seer is the hub of both and wins 56% when both teams draft
  for synergy.
- **Crush and Hold the Line lose.** Both contain Mortar, and Bulwark–Mortar counts as a pair in
  both groups. A team that drafts into them scores many pairs and still loses, which is what
  turns the overall synergy score upside down.

**4. Equal drafts.** When both teams have the same synergy score, the result is about
50/50 (49% in both scenarios). Side bias is small with this roster: A wins 53% with random
drafts, and team B no longer has the round-1 opener's edge measured with ten champions. Small
samples, so treat this as noise-level.

### What this means

- **The draft now decides matches, but through which plan a team picks, not how tightly it
  picks.** That is a healthier draft than "always take the pairs", provided the weak plans are
  fixed: a weak plan is a trap for new players.
- **These numbers measure the AI as much as the game.** The AI plays one ply deep:
  - it takes a "+3 against rooted" hit when one is in front of it;
  - it never sets up a two-step combo on purpose, such as rooting with one champion so another
    can Snipe;
  - it never walls a hex so a later push slams.

  Groups whose payoff needs a setup — Crush above all — are undervalued. A human who plans
  combos will get more out of them.
- **The payoffs are small.** +2 or +3 damage, or a slam for 2, against hits of 4–8. The doubling
  groups (Wildfire, Called Shot via Execute) show the biggest swings. If the owner wants a
  drafted plan to *feel* decisive, payoffs need to be closer to doubling than to +2.

## Medium payoffs, Mortar fix, combo-playing AI (2026-10-08, second round)

**The owner's answers.**
- Synergy should be *significant but not decisive*: medium payoffs, leaning smaller.
- Bans will matter once the roster is large (parked in `design/v2-direction.md`).
- Teach the AI to set up combos.
- Fix Mortar now — a champion that can't move is worthless.

**Changes.**
- **Payoffs:** every status payoff is now ×1.5 with a floor of +2 (D-052). The old doublings
  are gone. Slam is 3, or 4 for Mortar.
- **Mortar:** base SPD 2, and Barrage molds VIT down instead of SPD. **Speed can never drop
  below 1** for any champion (D-053).
- **Hold the Line** swapped Mortar for Briar, so Bulwark–Mortar is no longer counted in two
  groups.
- **The AI looks one step further** (D-054): our move, the opponent's most damaging replies,
  then our best follow-up. That's what lets it play a root and then the Snipe that cashes it
  in.

**How the AI change was measured** (150 matches per side, so side bias cancels out):

| Matchup | Combo-playing AI wins | Synergy hits per match |
|---|---|---|
| First try, pricing setups only, against the old AI | 25% | — |
| Setup pricing fixed (each champion counts its best follow-up once) | 46% | 4.4 vs 4.1 |
| **Depth-3 search** against the old AI | **62%** | 4.8 |
| Depth-3 with setup pricing against depth-3 without | 48–50% | 5.2 |

So the deeper search is what plays combos; pricing setups adds nothing measurable. It stays in
the code, off by default (`setups` agent in the harness). The AI's slowest decision is 446 ms,
against a 1,500 ms budget. A synergy hit is a payoff on a marked, rooted, burning, poisoned or
exhausted target, or a slam; the harness now counts them.

**The draft experiment again, with the combo-playing AI** (300 matches per scenario):

| | random | synergy | mixed |
|---|---|---|---|
| A / B wins | 49 / 50% | 50 / 49% | 53 / 45% |
| Synergy hits per match | 4.9 | 5.9 | **7.0** |
| Champion win rates | 33–64% | 39–59% | 38–56% |
| Group-building drafter against a random drafter | — | — | **51%** (synergy edge +5.0) |

| Group | random | synergy | mixed |
|---|---|---|---|
| Dive and Retrieve | 62% | 57% | 48% |
| Venom | 52% | 56% | 54% |
| Lockdown | 51% | 51% | 52% |
| Called Shot | 50% | 48% | 53% |
| Crush | 48% | 46% | 48% |
| Hold the Line | 38% | 43% | 43% |
| Wildfire | **33%** | **34%** | 40% |

**Reading it.**
- **Side bias is gone** (49–53%) now that both sides play combos.
- **Synergy is real but small.**
  - Drafting toward groups produces more payoffs: 7.0 per match against 4.9.
  - It turns into only about +1 to +3 points of win rate: 51% for the group-building drafter,
    53% at an edge of +3 or more.
  - On the owner's scale that is still *very small*, not medium. Champion strength (33–64%)
    swamps it.
- **Mortar can always move now and rose from 28% to 33–39%.** It is still the weakest, but a
  real champion. Further tuning waits for the roster (D-051).
- **Weak plans to watch:** Wildfire (Ember and Pyre, 33–40%) and Hold the Line (38–43%). Both
  hold up badly against random teams, so they are traps until tuned.

## Champion statistics inside the match (2026-10-09, 400 matches, random drafts)

**How win rates are counted.** A champion's win rate is its team's: of the matches where it was
drafted, how many its team won. To see *why*, the harness now also records per champion:
- damage dealt to enemy champions, and damage taken;
- healing and shields given, and tower damage;
- kills (the last enemy to damage the victim) and deaths;
- synergy payoffs and slams;
- how often it used an ability, a basic attack or a move.

It also adds:
- **mates:** its teammates' usual win rate;
- **drag:** the gap between the two;
- a **lane table:** its record against each enemy in the same role.

The simulation reports these through an observer (`IMatchObserver`), passed only for the real
moves, never for the AI's search. Run it with `tools/Augury.Tools selfplay N`; the full table is
in `production/qa/evidence/v2/round3-champion-stats-400.txt`.

| Champion | Win | Drag | Dealt | Taken | Heal/shield | Tower | K / D | Lane |
|---|---|---|---|---|---|---|---|---|
| Viper | 62% | +13 | 21 | 76 | — | 36 | 0.5 / 2.5 | beats Ember 65%, Talon 60% |
| Ranger | 61% | +12 | 79 | 47 | 15 heal | 21 | 2.5 / 1.2 | beats Mortar 70% |
| Tempest | 59% | +10 | 58 | 44 | — | 15 | 2.0 / 1.3 | beats Pyre 63%, Lens 56% |
| Oriel | 58% | +9 | 2 | 28 | 42 heal | 1 | 0.1 / 0.1 | beats Bastion 62% |
| Bulwark | 57% | +8 | 21 | 36 | — | 10 | 0.7 / 0.6 | beats Anchor 60% |
| … | | | | | | | | |
| Pyre | 40% | −9 | 54 | 46 | 8 heal | 10 | 2.0 / 1.2 | loses to Tempest 37% |
| **Mortar** | **32%** | **−17** | **66** | 29 | 16 heal | 11 | **2.2 / 0.6** | loses to Ranger 28%, Gunner 37% |

**What it says about Mortar.**
- It is not idle. It deals the third-most damage of all fifteen champions, gets 2.2 kills a
  match and rarely dies.
- Yet its team wins 32%, 17 points below what its teammates usually manage. So its damage
  doesn't turn into winning.
- Likely reasons, to check next:
  - **Range:** it fires from far back, at whoever is in reach rather than at whoever decides the
    tower fight.
  - **Pushes:** its Shell pushes enemies away, which may push them out of range of its own team.
  - **No objective pressure:** it barely moves (2.5 moves a match against about 4–5 for others)
    and deals little tower damage.
- Damage is not the measure of a champion here; towers and the nexus are. The owner's test,
  "its team loses even with good teammates", flags Mortar clearly, and Pyre, Talon, Briar and
  Bastion more mildly.

**Caveat.** "Mates" counts teammates' win rates including the matches they played with this
champion, so drag is slightly understated for the weakest and strongest.

## Synergy rebuilt: give and want (2026-10-09)

**The owner's target.** The draft should matter, but not win the match on its own:
- a good draft against a tragic one (no synergy at all) should win about **65/35**;
- one or two working pairs should be enough to compete;
- a great draft should still be beatable by a decent one played well.

**What was wrong.** The groups overlapped so much that every possible team had at least one
pair, and many champions combo'd with themselves: Ember both burned and paid off on burning. A
"tragic" draft could not exist, and pairs barely mattered.

**The new model (D-056).**
- **Every champion gives one status** with its abilities, and **wants another**.
  - A champion's "want" is a trait: **everything** it deals to an enemy carrying that status
    hits harder, basic attacks included.
  - No champion wants what it gives, so combos need a partner.
- **A pair** is two teammates where one gives what the other wants. The groups in
  `assets/data/synergies.json` list each status's givers and wanters.
- **Oriel gives and wants nothing**, a strong solo pick by design.
- **How many pairs teams get:** a random team averages 1.8, and **12% of teams have none**. The
  best teams have 4–5.

| Status | Givers | Wanters |
|---|---|---|
| Rooted (Lockdown) | Anchor, Briar, Ranger | Viper, Gunner |
| Marked (Called Shot) | Lens, Seer | Anchor, Talon |
| Burning (Wildfire) | Ember, Pyre | Lens, Mortar |
| Poisoned (Venom) | Viper | Briar |
| Exhausted (Smother) | Talon, Tempest, Bastion | Ember |
| Pinned by walls (Crush) | Bulwark, Mortar | Tempest (Gust's slam) |

**Other changes.**
- Kits trimmed so each champion gives exactly what the table says:
  - Seer's root became Curse (wound);
  - Pyre's exhaust became Wound;
  - Briar's wall became Snare;
  - slam is only on Gust;
  - roots and exhausts last 2 halves.
- **Payoffs are now ×2, at least +3** (`wantBonus`, `wantFloor` in `rules_config.json`). The
  harness's `payoffScale` dial sweeps them.
- **Balancing pass (D-057).** Five automated iterations nudged each champion's POW by its
  distance from 50% in random drafts. The spread went from 32–59% to **43–55%**, about the noise
  floor for ~450 picks.

**Results** (200 matches each, sides alternate, combo-playing AI):

| Matchup | Wins | Synergy hits per match |
|---|---|---|
| Great vs tragic, **payoffs switched off** | 45% | 0 |
| **Great vs tragic** | **52%** | 12.9 |
| Decent vs tragic | 54% | 8.8 |
| Great vs decent | 47% | 15.6 |

**Reading it.**
- **Synergy now happens:** about 13 synergy hits per match, up from about 1. It is worth about
  **+7 points**: 45% with payoffs off against 52% with them on.
- **That is still far from 65/35.** Two reasons:
  1. **Matches are decided by towers, not fights.** About 80% end by siege. More damage in fights
     moves the result only a little; see `design/match-reports/2026-10-09-two-ai-matches.md`.
  2. **Pairs are not equal.**
     - **Wildfire carries almost all the payoffs.** Lens and Mortar land about 8 each per match,
       because burn lasts two rounds, spreads to many targets, and the Ignite spell gives anyone
       burn.
     - **Called Shot and Lockdown rarely fire.** A mark is used up by the first hit from anyone,
       and a root lasts a round.
     - So a "5-pair" mark draft can lose 0–30 to a "2-pair" fire draft.

## Open questions for the owner

1. **How should synergy reach 65/35?** Options, roughly in order of how much they change the game:
   - **Make the weak pairs as reliable as Wildfire.** Marks last a round instead of being spent
     by the first hit, and roots last longer. Cheap, and probably worth +3–5 points.
   - **Let synergy reach the objectives.** For example, an enemy carrying a status your team
     wants doesn't count as a tower defender. Draft choices then move towers, which decide
     matches.
   - **Make fights matter more.** A kill costs the nexus more than 3. This changes the pace of
     the whole game.
2. **Spells.** Ignite and Exhaust give every team burn and exhaust, which dilutes the draft.
   Should spells stay generic, or be removed from the give/want web?
3. The opening fallback on Field 7 is still open.
