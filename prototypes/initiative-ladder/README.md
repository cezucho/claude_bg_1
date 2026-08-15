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
- **Correction to REPORT.md's "70% first-mover advantage":** the mirror-match bias
  does *not* move when the match opener is alternated (77.3% either way), and damage
  dealt is symmetric. It is therefore **not** a first-mover effect — it is an
  unisolated asymmetry in this harness. Do not trust absolute balance numbers from
  it until that is found.

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
