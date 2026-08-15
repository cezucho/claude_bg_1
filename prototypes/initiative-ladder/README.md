# Prototype: Initiative Ladder

> **PROTOTYPE — NOT FOR PRODUCTION.** Throwaway code. Never import from `src/`,
> and never refactor this into production. If the mechanic ships, it gets written
> from scratch against a real GDD.

**Concept tested:** the descending initiative ladder from
`design/gdd/game-concept.md` — an ability at initiative *N* may be answered by any
ability at initiative ≤ *N*, from any champion, until someone passes.

**Verdict: PROCEED.** Full findings in [`REPORT.md`](REPORT.md).

**Round 2 (2026-08-14)** — `ladder_v2.py` implements the rules the GDD settled on
(two halves per round, the Last Word) and answers two further questions:

- **Action economy is per HALF, not per round.** Under per-round, a team entered the
  half it opens with zero available champions in **54.9% of rounds**, 54% of halves
  ended by exhaustion rather than choice, and the median half was 3 resolutions.
  Under per-half: 0.8% combos cut short, 68% of halves end by a deliberate pass,
  median half 9 resolutions. Cost: 16.2 resolutions per round vs 8.9, so ~17 rounds
  fit a 15-minute match instead of ~21.
- **Passing survives the Last Word.** Strategic passing held at 7.7% (was 8.6%).
**Round 3 (2026-08-14) — the "70% first-mover advantage" was two harness bugs.**
`asymmetry_hunt.py` isolated both. **There is no first-mover advantage in the ladder.**

1. `targets_for` capped move options with `out[:6]`. `NEIGHBORS` begins
   `[(1,0), (1,-1), (0,-1), ...]`, so only +q / -r moves were ever generated. Team 0
   spawns at q=-3 and needs +q to reach the objectives; team 1 spawns at q=+3 and
   needs -q. **Team 1 could not walk toward the centre.** Worth ~24 points of win rate.
2. `State.winner()` tested team 0 first, so every round where *both* teams crossed the
   point threshold was awarded to team 0. Points are scored at round close, so
   simultaneous crossings are common. Worth ~15 points of win rate.

After both fixes, mirror matches run **43-50%** across every spawn layout and both
action economies. Side-swapping the teams changes nothing.

Lesson for the Balance Simulation Harness: **never cap an action set along an ordered
axis, and never resolve a win condition by player index.** Both bugs were invisible in
aggregate — points, damage and action counts were near-identical between the teams
while one side won 65% of matches.

*(Round 2's economy comparison was unaffected: the bias applied to both arms equally.
Re-measured after the fixes, per-round is worse than first reported — a team enters
the half it opens with zero available champions in 61.8% of rounds, and 62% of halves
end by exhaustion.)*

## Why Python, in a C# project

This container has no .NET runtime. Prototype code is never refactored into
production anyway, so the language is irrelevant to the questions being asked —
all of which are about *rules*, not about implementation. Measurements are
therefore reported as algorithmic quantities (nodes searched, branching factor,
ladder length) which transfer across languages. Wall-clock timings are not
reported, because they would not.

## Files

| File | Purpose |
|---|---|
| `sim.py` | The model: hex board, champions, abilities, ladder resolution, death check, status phase, scoring |
| `agents.py` | Random, greedy, and depth-limited negamax policies |
| `experiment.py` | Action-economy variants, random baseline, snowball probe |
| `search_probe.py` | Samples mid-ladder positions and asks a search agent whether it passes |
| `mirror_probe.py` | Identical-draft matches: win asymmetry and molding divergence |
| `ladder_v2.py` | **GDD rules**: two halves per round, the Last Word, and per-round vs per-half action economy |

## Running

```bash
python3 experiment.py econ       # action economy variants  (~1 min)
python3 experiment.py snowball   # early-lead → win correlation
python3 search_probe.py          # does a searching agent pass?  (~3 min)
python3 mirror_probe.py          # win asymmetry, Pillar 3 test
python3 ladder_v2.py             # per-round vs per-half economy under GDD rules
```

No dependencies beyond the standard library.

## What this could not answer

Whether the ladder is fun, whether it is legible, or whether a player can hold
"what can I answer with right now" in their head. Those need a playable build and a
person. The report says so rather than implying otherwise.

## Assumptions baked in

- Movement is an initiative-1 ability (an open question in the systems index)
- Draft and opening phases are absent
- All balance numbers — damage, HP, poison, scoring, respawn timers — are invented
  to make the sim run, not to be correct. Findings that depend on them are labelled
  tuning-dependent in the report.
