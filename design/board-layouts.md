# Board Layouts — v2 Board-Size Experiment

> **Date**: 2026-10-04 · **Status**: Candidates measured; owner to choose
> **Why**: the owner's playtests found the board "one dimension too large" — the depth,
> front to back (`design/v2-direction.md`). Three shallower boards were built as data and
> played against the current one.
> **Picture**: `design/board-layouts/board-layouts.png` (all four at the same scale).

## The candidates

| | Classic — today | Field 7 | Field 7 + base | Field 5 |
|---|---|---|---|---|
| Shape | Hexagon, radius 4 | 9 across × 7 rows | 9 across × 7 rows | 9 across × 5 rows |
| Hexes (per champion) | 61 (6.1) | 59 (5.9) | 59 (5.9) | 43 (4.3) |
| Front lines apart | 8 rows | 6 rows | 4 rows | 4 rows |
| Nexus | On the start line | On the start line | **In a base row behind the team** | On the start line |
| Home towers | 2 rows ahead of the line, inner | 2 rows ahead, on the side lanes | 1 row ahead, on the side lanes | 1 row ahead, on the side lanes |
| File | built in | `board_field7.json` | `board_field7base.json` | `board_field5.json` |

The three fields share one idea taken from League: **three lanes**. The two home towers stand
on the top and bottom lanes, the centre tower on mid, and top faces top. Every layout is
mirror-symmetric (enforced on load). Field 7's start line falls on a half-offset row, so its
five start hexes sit half a hex toward the top side. That affects top-side against
support-side room, not team fairness.

## Measurements — 400 AI-vs-AI matches each

Towers **solid** (the owner's rule), walkable in brackets.

| | Classic | Field 7 | Field 7 + base | Field 5 |
|---|---|---|---|---|
| Contact: hexes to the nearest enemy when the opening ends | 3.0 (1.9) | 1.6 (1.4) | **1.4 (1.3)** | 1.4 (1.2) |
| Rounds per match | 13.1 (12.1) | 11.7 (10.1) | 12.1 (10.1) | 10.1 (8.5) |
| Decisions per match, both teams | 239 (224) | 218 (192) | 227 (192) | 193 (164) |
| Rotatable/fixed abilities with a target, when ready | 62% (66%) | 76% (70%) | 78% (66%) | **84%** (70%) |
| Matches ended by a direct nexus attack | 12% (31%) | 33% (50%) | 28% (47%) | 48% (68%) |
| Comebacks (winner trailed by 5+ at a round close) | 19% (23%) | 23% (22%) | **28%** (22%) | 26% (19%) |
| Basics that are attacks | 56% (55%) | 62% (61%) | 64% (64%) | 72% (72%) |
| A / B wins | 47/52 (50/48) | 45/54 (50/49) | 52/48 (44/56) | 49/51 (44/56) |

Command: `dotnet run -c Release --project tools/Augury.Tools selfplay 400 board=<name> towersBlock=<true|false>`.

## What the numbers say

- **Shallower boards deliver what the owner pictured: everyone in contact after the
  opening.** Classic with solid towers leaves the teams 3 hexes apart; every field brings
  them to about 1.4.
- **Fixed and rotatable patterns find targets far more often on a shallow board**: 62% on
  classic, up to 84% on Field 5. This supports the plan to lean on fixed patterns for
  distinct champions; on classic they would sit idle a third of the time.
- **The nexus position decides what a match is about.** With the nexus on the start line of
  a shallow board (Field 5), half the matches end in a direct nexus attack, and the fight
  is about the nexus from round one. With the nexus in a base row behind the team, tower
  siege stays the main route and the nexus is something you break through to.
- **Comebacks are highest on Field 7 + base** (28%), the owner's "rich stories".
- **Match length barely moves.** It is mostly set by nexus HP, which stays a separate dial.
- **Watch item**: a few walkable-tower runs show B ahead 56/44 (about 2.4 standard errors
  at 400 matches). It vanishes with solid towers on the same boards, so it is probably the
  AI's opening collisions rather than geometry. Re-measure after the champion rewrite.

## Caveats

- **The openings were not compared.** Today's placeholder openings were written for the
  classic board: each moves its team about three rows forward. On a board whose front lines
  are 4–6 apart, the teams collide mid-opening and most openings hit the fallback (81–100%),
  walkable towers or not. The champion rewrite will write openings for the chosen board.
  Expect fewer forward moves, and more sideways shaping of the formation.
- **Every number is this AI playing placeholder champions.** It measures geometry and
  pacing, not fun.

## Recommendation (Claude's)

**Field 7 + base, with solid towers.** It has the closest contact after the opening, the
most comebacks, patterns live 78% of the time, and a base to defend behind your own line.
That last point is a natural home for the "use the champion's full potential" feeling:
someone has to hold the base while others push. Field 5 is the most compact, but at 4.3
hexes per champion there is little room to manoeuvre (72% of basics are attacks), and the
nexus is in the fight from the first move. Field 7, without the base, sits between them.

## How to try one in the browser

Set `"board": "field7base"` (or `field7`, `field5`) in `assets/data/rules_config.json` and
restart the server. Expect the opening to fall back often until the champions are rewritten.
