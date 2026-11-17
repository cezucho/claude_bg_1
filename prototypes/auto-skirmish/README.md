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
  deals 2; side towers hit an adjacent enemy for 3 each pulse; the dead return at the end of
  the next round; after round 10 the higher nexus wins.
- **Boards.** Field 7 (59 hexes, the main game's layout, default) and a small 37-hex board.
- Watch mode, speed toggle, "show targets" toggle, rules sheet. `simulate(n, board, seed)` in
  the browser console runs headless AI-vs-AI matches and returns counts.

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

**Findings.** None from play yet.
