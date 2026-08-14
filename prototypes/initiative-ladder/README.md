# Prototype: Initiative Ladder

> **PROTOTYPE — NOT FOR PRODUCTION.** Throwaway code. Never import from `src/`,
> and never refactor this into production. If the mechanic ships, it gets written
> from scratch against a real GDD.

**Concept tested:** the descending initiative ladder from
`design/gdd/game-concept.md` — an ability at initiative *N* may be answered by any
ability at initiative ≤ *N*, from any champion, until someone passes.

**Verdict: PROCEED.** Full findings in [`REPORT.md`](REPORT.md).

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
| `mirror_probe.py` | Identical-draft matches: first-mover advantage and molding divergence |

## Running

```bash
python3 experiment.py econ       # action economy variants  (~1 min)
python3 experiment.py snowball   # early-lead → win correlation
python3 search_probe.py          # does a searching agent pass?  (~3 min)
python3 mirror_probe.py          # first-mover advantage, Pillar 3 test
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
