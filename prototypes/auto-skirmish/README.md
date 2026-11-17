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
