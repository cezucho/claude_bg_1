# Prototype: auto skirmish

**Hypothesis.** The game gets lighter, quicker and more rewarding if the player makes few,
simple choices and the champions fight on their own: draft, a short movement-only opening,
then one move per turn followed by an automatic "pulse" where every champion acts by
initiative, rolling visible percentage chances (poison, frost, crit, splash…). Position still
decides the fight through adjacency synergies and public targeting rules, and champions grow
along fixed two-option paths on a schedule. Inspiration: the mobile game Rogue Legend for the
growing chances and quick choices, Neuroshima Hex for initiative and adjacency, League of
Legends for roles and chaos. Owner's direction, 2026-11-17 (D-068).

**What it is.** One self-contained HTML file, `index.html`, no server, no libraries.

- **Draft.** Snake draft of 10 champions, one per role. Each has a tag; some give an
  **aura** to every ally beside them (+initiative, +attack, armour, +chance), some have a
  **bond** that switches on beside a named tag.
- **Opening.** Every champion has two movement cards. Teams alternate; each champion plays
  exactly one card; a card moves one or more roles one hex at a time in the team's direction.
  A blocked step is skipped. Hovering a card previews its steps and the synergies it makes or
  breaks.
- **Turns.** The team on turn moves one champion up to 2 hexes, or holds. Then the **pulse**:
  every champion acts once, highest initiative first (ties to the mover), attacking by its own
  rule (nearest, lowest HP, highest HP, back line, highest attack, most crowded) if anything is
  in range. A diver with nothing in range leaps beside its target. Two turns per round, the
  first mover alternates.
- **Chances.** Each action rolls the champion's chances; defenders roll dodge and thorns. The
  numbers on the cards are the real odds. A seeded random generator drives every roll, so a
  match can be replayed from its seed.
- **Preview.** Selecting a champion and hovering a destination shows the synergy links gained
  (green) and lost (red), the new initiative badges, and who it would attack and who would
  attack it. The fight itself is not previewed, by design.
- **Growth.** At the end of rounds 2, 4, 6 and 8 every champion levels up and the player picks
  one of two options from the champion's fixed path (shown on its card). A kill gives the
  killer +1 attack, up to 3.
- **Score.** Nexus 30; a death costs 4; each champion beside the enemy nexus at round end
  deals 2, but only once one of that nexus's side towers has fallen; after round 10 the higher
  nexus wins.
- **Holding a won fight (added 2026-11-17, D-069).** Side towers have 15 HP; a champion with no
  enemy champion in range attacks an enemy tower in range; a fallen tower stays fallen and its
  hex becomes walkable. The death timer grows: the dead return one round later in rounds 1–3,
  two in rounds 4–6, three from round 7. A champion not hit in the previous pulse regains 3 HP
  at the start of the next. All three are switches in `RULES` (`towerHp`, `gate`,
  `respawnStep`, `regen`).
- **Boards.** The small 37-hex board (default since 2026-11-17, the owner's preference: the
  champions stay in reach, so more happens) and Field 7 (59 hexes, the main game's layout).
  Both have two side towers per team.
- Watch mode, speed toggle, "show targets" toggle, rules sheet. `simulate(n, board, seed)` in
  the browser console runs headless AI-vs-AI matches and returns counts.

- **Pulse pace (added 2026-11-17, D-070).** A champion with nobody in range gets no beat.
  Plain hits play fast (0.22 s); a beat with a chance firing or a kill gets a full 0.7 s.
  Champions with the same initiative show together as one beat (they still resolve in order).
  The "Pulse" button switches to **once per round**: both teams move, then one pulse, ties to
  the team that moved first. That mode plays longer matches to make up for half the fighting:
  up to 16 rounds, level-ups at rounds 3, 5, 8 and 11, siege 3, death timer step 4.

- **Gold, items, control, opening level-ups, specials (added 2026-11-17, D-071).** Each round
  every living champion earns 1 gold for its team; a kill pays 4, plus 2 per bounty star on the
  victim (shutdown); a tower pays 4. After every round the shop offers 3 of 15 items (stats or
  chances, a Phoenix Feather that survives one death, a Sage Sash against freeze and knock-up);
  up to 3 per champion; unspent gold carries over. Six control statuses join frost: taunt,
  root, blind, silence, airborne, berserk; a champion that just skipped a turn is steadfast
  (cannot be frozen or knocked up again until it acts). A team levels up one champion of its
  choice after its 2nd, 3rd and 5th opening card; past the end of a path a champion takes
  "veteran" levels (+1 attack or +6 HP). Every champion has a charged special: after 2 or 3
  attacks its next action is the special (Anchor Slam, Quake, Wildfire, Execute, Lance,
  Blizzard, Volley, Cannon Blast, Sanctuary, Mind Twist). Switches in `RULES`: `gold`, `cc`,
  `openLevels`, `specials`.

- **Nexus: destroy it (switch, added 2026-11-17, D-072).** The nexus becomes one building at the
  back centre of each side (Mid starts one hex in front of it on Field 7). It is locked until one
  of its side towers has fallen; then a champion standing beside it attacks it, and a champion
  with no enemy champion in range attacks it from range. It hits an adjacent attacker for 3 at
  the start of each pulse. Deaths and round-end siege no longer damage it. At the round limit:
  more nexus HP, then more towers standing, then more gold earned. The switch also sets nexus
  30 HP, towers 10 HP, 14 rounds, and a death timer that grows every 2 rounds. The switches are
  on the draft screen as well as the toolbar.

- **Comeback rules (added 2026-11-18, D-073), part of "Nexus: destroy it", now the default.**
  *Stances:* each turn a team also sets Push, Hold or Fall back for free; at the start of each
  pulse every champion steps one hex by it (Push: champions not yet fighting step toward the
  nearest enemy or open structure; Fall back: champions outside their own half step home). The
  board previews your team's steps as dashed arrows. *Base towers:* two towers just past the
  back edge behind each nexus (nobody can stand there), 8 HP, hitting for 5 at range 2; they
  can be attacked once a side tower has fallen, and the nexus opens only once one of them has.
  *Home regeneration:* no regeneration in the enemy half. *Home zone:* within 2 hexes of its own
  nexus a champion takes 1 less per hit. *Respawning together:* when a team's first dead
  champion is due back, all its dead return together. The preset also sets nexus 25, towers 10,
  16 rounds, death timer step 2. Each part is a switch in `RULES`.

- **Respawn window and monsters (added 2026-11-18, D-074), now in the default.** *Window:* a dead
  champion also returns with a teammate who is due back now, if its own timer would end within
  one more round (replaces "respawning together", still a switch). *Dragon* (left end of the
  river, 16 HP, hits an adjacent champion for 3 each pulse, back 3 rounds after it dies):
  +10% to every chance for the whole team, stacking to 3, plus 3 gold. *Baron* (right end, from
  round 7, 30 HP, hits for 5, back after 4 rounds): for 2 rounds the team deals double damage
  to towers and the nexus and regenerates anywhere, plus 6 gold. A champion with nothing else
  in range attacks a monster in range; the team landing the killing blow takes the reward,
  even when the other team did most of the damage (a steal). Switches: `respawnWindow`,
  `monsters` and the monster numbers in `RULES`.

**Not in it.** Item shop, kill rewards other than bounty, objectives, more than 10
champions, a smarter AI (it judges one move by expected damage of the next pulse; level-ups are
coin flips), sound, real art.

**How to run.** Open `index.html`, or the published artifact link from the session.

**Status.** In progress, first playable 2026-11-17.

**First measurements** (300 AI-vs-AI matches per board, seed 11, before any tuning):

| Board | Avg rounds | Ended by nexus | Deaths per match | Chances fired per match | A wins |
|---|---|---|---|---|---|
| Field 7 | 9.1 | 60% | 10.5 | 55 | 49% |
| Small | 6.6 | 94% | 8.8 | 41 | 54% |

Both boards are roughly fair between the sides. On the small board matches are about a third
shorter and almost always end by destroying a nexus; on Field 7 four matches in ten go the full
ten rounds. Which pace feels right is the owner's call after playing.

**Finding: the reset after a won fight (owner, 2026-11-17).** The owner wins a team fight,
pushes, and the respawned enemies, back at full HP on their own nexus line, beat the wounded
winners; the advantage keeps resetting. Measured with a new AI setting that pushes toward the
enemy nexus like a human does (the first AI barely pushed after winning, so it never showed
the problem). A "won fight" is a round won by two or more kills. 1000 AI-vs-AI matches per
board, seed 11:

| Board | Rules | Kills in the 2 rounds after a won fight, winners vs losers | Losers win one of the next 2 rounds' fights | Winners take a tower next round | Team that wins a fight wins the match | A wins | Rounds |
|---|---|---|---|---|---|---|---|
| Field 7 | before | 1.18 vs 1.03 | 9% | – | 78% | 44% | 8.5 |
| Field 7 | after | 1.22 vs 0.84 | 6% | 32% | 82% | 48% | 8.7 |
| Small | before | 1.16 vs 1.34 | 17% | – | 74% | 45% | 8.0 |
| Small | after | 1.37 vs 0.86 | 7% | 33% | 84% | 49% | 8.1 |

Before, on the small board, the team that lost a fight out-killed the winners in the next two
rounds: the owner's swing. After, the winners keep the upper hand and convert a third of their
won fights into a tower the next round. Tower HP 12–20, regeneration 2–4 and a death-timer step
of 3–5 all gave similar results; the chosen values (15, 3, 3) held best on the small board. The
side imbalance with a pushing AI (A 44–45%) is gone. Risk to watch: a won fight now decides the
match 82–84% of the time, so comebacks are rare; tower HP 20 lowers that to about 80%.

**Finding: pulse length (owner, 2026-11-17).** The owner worried that one move followed by ten
champions acting takes too long. Measured over AI-vs-AI matches on the small board:

| Version | Beats per pulse | Seconds per pulse | Watching per match | Rounds | Ended by nexus | A wins | Won fight wins the match |
|---|---|---|---|---|---|---|---|
| Before (one beat per champion, 0.48 s) | 8.2 | 3.9 | about 60 s | 8.1 | 85% | 49% | 84% |
| Pulse after every move, new rhythm | 5.3 | 3.0 | 47 s | 8.1 | 85% | 49% | 84% |
| Once per round, ten rounds | 6.1 | 3.5 | 35 s | 9.9 | 18% | 50% | 83% |
| Once per round, preset (16 rounds max) | 6.0 | 3.4 | 43 s | 12.7 | 90% | 54% | 76% |

About 1.6 beats per pulse were champions doing nothing, and only 3.7 had a chance or a kill.
The total watching was never long; the issue was the ratio, four seconds of watching for every
click. The new rhythm cuts a pulse by a quarter without touching the rules. Once per round
gives two decisions per pulse, but with the same ten rounds most matches ran out the clock,
so it needs the longer preset; the total watching then is about the same as before. With the
preset a won fight decides the match less often (76%), so comebacks are more likely. A at 54%
over 600 matches may be noise and needs a bigger run.

**Measurement: gold, control, opening level-ups, specials (2026-11-17).** 600 AI-vs-AI matches
per row, small board, pulse after every move:

| Rules | Rounds | Ended by nexus | A wins | Won fight wins the match | Gold leader after round 5 wins | Items per match | Specials per match | Skipped turns per match | Seconds per pulse |
|---|---|---|---|---|---|---|---|---|---|
| All four off | 8.0 | 89% | 50% | 84% | – | – | – | 2.9 | 2.8 |
| Gold only | 7.7 | 90% | 47% | 88% | 84% | 9.4 | – | 2.8 | 2.9 |
| Control only | 7.9 | 86% | 50% | 85% | – | – | – | 3.8 | 3.0 |
| Opening level-ups only | 7.8 | 88% | 49% | 85% | – | – | – | 3.0 | 3.0 |
| Specials only | 7.8 | 86% | 51% | 86% | – | – | 15.7 | 5.1 | 3.0 |
| All on | 7.7 | 88% | 49% | 83% | 81% | 9.3 | 15.2 | 5.3 | 3.1 |

- Gold behaves like League's: the team ahead in gold after round 5 wins about four times in
  five. Changing income and kill gold (1–3 and 2–4) moved that by at most two points, because a
  gold lead mostly records the fights already won. Gold alone made fights a little more
  decisive (84% to 88%); with everything on it is back to 83%.
- Control statuses did not shorten pulses. Most of them change an attack rather than cancel
  it, and only frost and airborne skip a turn: about 5 skipped turns per match with
  everything on, one every three pulses. Skipped turns are now quick beats; a pulse with
  everything on is 3.1 s against 2.8 s, because more beats now carry a chance or a special.
- Sides stay even (49–51%) in every configuration.

**Measurement: destroying the nexus (2026-11-17).** Small board, pulse after every move, all
other features on; 600 matches per row (1000 for the last four):

| Rules | Ended by a destroyed nexus | Rounds | A wins | Won fight wins the match |
|---|---|---|---|---|
| Score (current default) | 88% | 7.7 | 49% | 83% |
| Destroy, nexus 40, towers 15, 10 rounds | 25% | 9.9 | 53% | 84% |
| Same, plus "beside the nexus attacks it" | 27% | 9.8 | 54% | 83% |
| Same, nexus does not shoot | 37% | 9.7 | 52% | 84% |
| Same, 14 rounds | 67% | 12.4 | 54% | 86% |
| Towers 10, nexus 30, 14 rounds | 74% | 11.5 | 52% | 84% |
| Towers 10, nexus 25, 14 rounds | 78% | 11.1 | 51% | 84% |
| Towers 10, nexus 30, 16 rounds | 85% | 11.8 | 52% | 84% |
| **Towers 10, nexus 30, 14 rounds, death timer step 2 (chosen)** | **84%** | **10.8** | **52%** | **85%** |

- First try stalled: a push spends its pulses fighting the defenders who respawn beside their
  nexus, and champions always preferred champions as targets. "A champion beside an open nexus
  attacks it" makes standing at the gates a deliberate siege move, but on its own it barely
  helped.
- The real bottleneck was time: a side tower must fall before the nexus opens, and with 15-HP
  towers and 10 rounds there was rarely time left. Weaker towers, a longer limit and death
  timers that grow faster (a won late fight leaves the defenders down long enough) fixed it.
- Matches are about three rounds longer than with the score rules (10.8 against 7.7).
- Not measured: destroy mode together with "Pulse: once per round". Each switch applies its own
  round limit, so combining them uses whichever was switched last.

**Measurement: comebacks (2026-11-18).** The owner: a won fight decides the match 85% of the
time, there are no comebacks, and with one move per turn a team can neither push nor fall back
together. "Comeback" = the team that lost a won fight (a round lost by two or more kills) still
wins the match. Small board, destroy mode, AI-vs-AI, 600–1000 matches per row:

| Rules | Comeback | Tower taken within 2 rounds of a won fight | Ended by a destroyed nexus | Rounds | A wins |
|---|---|---|---|---|---|
| Destroy, none of the new rules | 16% | 54% | 85% | 10.9 | 53% |
| + stances | 13% | 61% | 98% | 9.0 | 54% |
| + base towers | 15% | 63% | 49% | 13.0 | 49% |
| + home regeneration | 16% | 52% | 85% | 11.0 | 53% |
| + home zone | 14% | 49% | 76% | 11.4 | 51% |
| All four | 16% | 69% | 76% | 11.9 | 52% |
| All four + respawning together, death step 2 | 22% | 59% | 57% | 12.8 | 51% |
| Same, no base towers | 17% | 49% | 88% | 10.9 | 53% |
| Same, stronger home (towers hit 8, home armour 2), step 3 | 22% | 54% | 22% | 13.6 | 54% |
| **All five, base towers 8 HP, nexus 25, 16 rounds (default)** | **22%** | **58%** | **79%** | **13.1** | **49%** |

- None of the four changes alone moved comebacks. Stances made pushes faster and matches
  shorter. The lever was **respawning together**: defenders who return one at a time lose to the
  push one at a time, which was the owner's diagnosis. It only works with base towers (17%
  without them): the regrouped defenders need something to defend under.
- Comebacks rose from 16% to 22%, short of the 25–35% aimed for. A won fight still converts into
  a tower within two rounds 58% of the time, so fights still pay. The cost is length: 13.1
  rounds against 10.8.
- Punishing greed, tested with a "greedy" AI on one side that pushes whenever it is not
  outnumbered, whatever its HP: it wins 51% against the careful AI under the default rules, and
  53–54% under the old ones. Reckless pushes are now even rather than profitable, but not yet
  punished. The careful AI rarely overextends, so AI-vs-AI understates comebacks: a human who
  pushes too long will meet a regrouped team under two towers.
- In score mode the same rules gave 21% comebacks but a 55% side imbalance, so they belong to
  destroy mode only. Score mode is unchanged.
- Stances move about 0.7 champions per pulse; a pulse is 3.2 s.

**Measurement: respawn window and monsters (2026-11-18).** Small board, destroy mode with the
comeback rules, AI-vs-AI, 800 matches per row (rows marked * after making the AI value monsters
more when it is not outnumbered):

| Rules | Comeback | Tower within 2 rounds of a won fight | Ended by destruction | Rounds | A wins | Dragons / Barons per match | Baron taker wins | Baron taken while behind | Steals |
|---|---|---|---|---|---|---|---|---|---|
| Respawning together (previous default) | 23% | 58% | 79% | 13.1 | 50% | – | – | – | – |
| No grouping | 16% | 69% | 89% | 11.9 | 53% | – | – | – | – |
| Window of 1 round | 15% | 70% | 89% | 11.9 | 53% | – | – | – | – |
| Window 1 + monsters * (default) | 15% | 66% | 89% | 12.2 | 54% | 0.58 / 0.44 | 89% | 51 times, 67% won | 14% |
| Window 2 + monsters * | 15% | 64% | 88% | 12.4 | 52% | 0.60 / 0.45 | 89% | 61 times, 69% won | 13% |
| Window 3 + monsters * | 18% | 60% | 86% | 12.7 | 53% | 0.62 / 0.56 | 80% | 95 times, 53% won | 16% |
| Respawning together + monsters * | 21% | 56% | 78% | 13.2 | 52% | 0.66 / 0.63 | 72% | 117 times, 44% won | 20% |

- How respawns worked: champions killed in the same round always shared a timer and returned
  together. Under "respawning together", every dead champion returned with the first one due,
  so a champion killed one round earlier could skip most of its timer.
- The one-round window behaves like no grouping. The comeback gain of "respawning together" came
  precisely from its long shortcuts, the part that felt awkward. The comeback rate depends on
  how early defenders return; there is no free lunch.
- Baron works as a comeback tool: a team that takes it while behind (lower nexus HP plus
  towers) wins 67% of those matches. But it is rare in AI play (about one match in sixteen),
  because the AI only fights monsters when it is not outnumbered and nothing else is in range.
  Baron is also decisive in the leader's hands (taker wins 89%); weaker or shorter Baron is the
  knob if it closes games too fast.
- Monsters are fought about once per match in AI play, far below what a human going for them
  would do. Steals happen 13–20% of the time.
- A wins 52–54% in the default rows: worth a bigger run before trusting.
