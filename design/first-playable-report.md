# First Playable — Report

> **Date**: 2026-09-27 · **Build**: terminal client over the full simulation
> **Scope**: what was built, what 500 self-play matches say, and what only a human can
> answer. Decisions made without the project owner are in `claude-decisions.md`.

## What exists

| Piece | Where | State |
|---|---|---|
| Rules | `design/mvp-rules.md` | Complete for the MVP, numbers retuned once |
| Decisions log | `design/claude-decisions.md` | 33 entries, each reversible |
| Simulation | `src/Augury.Sim` | Every rule in the sheet; deterministic, integer-only, blittable state |
| Content | `assets/data/` | 10 placeholder champions, `rules_config.json` |
| AI | `src/Augury.Sim/AI` | Evaluation + one-reply search; ≤ 40 ms per decision |
| Clients | `src/Augury.Web`, `src/Augury.Cli` | Browser (broadcast layout) and terminal; vs AI, hotseat, watch |
| Harness | `tools/Augury.Tools selfplay` | Checks the GDDs' own acceptance criteria |
| Tests | `tests/unit` | 67 passing |

How to play: `PLAY.md`.

## Self-play: 500 AI-vs-AI matches, final rules

| Measure | Value |
|---|---|
| Team A / B wins | 48% / 52% — no structural first-mover edge |
| Endings | score 91.8%, **nexus 8.2%**, round cap 0% |
| Rounds per match | mean 13.4 (range 5–17) |
| Deaths per round | 1.21 |
| Tower captures per round | 0.49 |
| Ability resolutions per round | 10.3 |
| Chains per round | 0.50 (in 25% of halves) |
| Basics that are attacks | 54% |
| Team openings hitting the fallback | 23% |
| AI decision time | mean 0.2 ms, max 18 ms (budget 1500) |

### Against the GDDs' acceptance criteria

| Criterion | Target | Measured | |
|---|---|---|---|
| Movement #12 — ladder opened | ≥ 70% of halves | 100% | PASS |
| Sigils #14 — halves ending by a deliberate pass | ≥ 50% | 19% | **FAIL** (see below) |
| Map #24 — nexus endings | non-zero minority | 8% | PASS |
| Map #25 — nexus endings from behind | some | 12% of them | PASS |
| Sigils #15 — chains | minority of halves | 25% | PASS |
| Movement #11 — basic attacks | ≤ 80% of basics | 54% | PASS |
| Ladder F5 — resolutions | ≤ 16 per round | 10.3 | PASS |
| Opening #10 — fallback rate | 10–25% | 23% | PASS — not tuned for |
| AI budget | ≤ 1500 ms | 18 ms | PASS |

## What the harness found

**1. The nexus gate as first designed never opened.** Requiring both home towers meant a
nexus opened in 21–60% of matches but stayed open for only 2–7% of decisions. The
geometry explains it: a team's home towers sit right beside its own spawn row, so
respawning defenders retake them almost immediately. Loosening the gate to *either* tower
(D-025) and lowering nexus HP (D-026) produced 8% nexus endings, one in eight of them a
comeback from behind. **This is the finding most worth your attention**, because it is
where your comeback idea meets the map's layout.

**2. The AI was part of the problem.** Before the rule change, the AI counted nexus damage
only while the gate was open, so it forgot its own progress whenever the gate closed. That
looked like a rules failure and was partly a player failure (D-031). Worth remembering:
**every number here is a measurement of this AI playing these rules**, not of the rules
alone.

**3. Always-legal abilities weaken exhaustion.** A dash that can target any empty hex, or
a heal that can target yourself, means its owner always has an initiative-1 answer. The
ladder's rule that running out of answers ends the half without a Last Word rarely fires.
Kept for now (D-030).

**4. Placeholder champions span 42–58% win rate** when picked: Stalker 58%, Ranger 57%,
Juggernaut 56%, Oracle 55%, Lifeweaver 52%, Bastion 48%, Tempest 45%, Warden 44%, Gunner
43%, Shade 42%. Not tuned, because they exist to be replaced.

**5. The pass-rate criterion fails, probably because of the AI.** Champions reset every
half, so a greedy AI never has a reason to hold an answer back and rarely passes. The
ladder prototype's agents passed 61–68% of the time. Whether humans pass is the question,
and it matters: passing and the Last Word are the ladder's signature decision.

## What only playing will tell you

These are the questions the harness structurally cannot answer. They're the reason to
play it.

1. **Is the ladder a decision or a formality?** Do you find yourself weighing what your
   move lets the opponent do, or just playing your best ability each turn?
2. **Is the opening a puzzle or a chore?** Does sequencing five abilities feel like
   planning, or like reading instructions?
3. **Do chains feel like moments?** They occur in a quarter of halves. Is that a
   highlight, or noise?
4. **Does friendly blocking feel like formation or like clumsiness?** (D-017)
5. **Is the board readable?** The terminal board wasn't (2026-09-28), so there is now a
   browser client. Does it show you what you need to plan a ladder turn?
6. **Would you ever pass?** See finding 5.

## Suggested next steps

1. **Play five matches against the AI** and note answers to the questions above.
2. Replace or rewrite champions. They're JSON, and the validator will tell you when
   something breaks a rule.
3. Revisit D-025/D-026 once humans have played: the nexus tuning was done against an AI
   that may not play the way people do.
4. Amend ADR-0004, whose command set is now out of date (D-019).
