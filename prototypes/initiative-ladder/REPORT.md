# Prototype Report: Initiative Ladder

*Date: 2026-08-14 · Prototype: `prototypes/initiative-ladder/` · Throwaway Python*

---

## Hypothesis

The descending initiative ladder — an ability at initiative *N* may be answered by
any ability at initiative ≤ *N*, from any champion, until someone passes — produces
a **real decision** rather than a spend-everything ritual, stays **bounded** enough
for a 10–15 minute match, and leaves a search space an AI can actually handle.

## Approach

A headless simulation in Python (this container has no .NET; prototype code is
never refactored into production, so the language is irrelevant to the questions).
Built: hex board, 5v5, eight champion templates with deliberately contrasting
initiative curves, four abilities each plus a Reposition, cooldowns, molding deltas,
poison, the death-check-then-status ordering with the dying round, respawns on a
scaling timer, and objective scoring.

Three agents — random, greedy, and a depth-limited negamax over the ladder.
Roughly 1,100 simulated matches across five action-economy variants, plus a
targeted probe that samples real mid-ladder positions and interrogates a search
agent at each one.

**Shortcuts taken:** all balance numbers are invented. Damage, HP, scoring rates,
poison strength, and respawn timers were chosen to make the sim run, not to be
correct. The draft and opening phases are absent. Movement is modelled as an
initiative-1 ability — one of the open questions, assumed rather than tested.

**A distinction this report holds to throughout:** findings that follow from the
*rules* are robust; findings that depend on *my invented numbers* are not, and are
labelled as such.

---

## Result

### Structural findings — these follow from the rules and should hold

**1. Action economy: one action per champion per round is the right rule.**

| Variant | Ladder length mean/max | Rounds/match | Pass-with-options |
|---|---|---|---|
| per-champion / pass ends round | 8.2 / 10 | 11.8 | 6.8% |
| per-champion / both must pass | 8.7 / 10 | 11.4 | 11.2% |
| **uncapped** / pass ends round | **31.3 / 41** | 6.9 | 1.1% |
| **uncapped** / both must pass | **40.0 / 41** | 6.8 | 16.6% |
| per-team cap of 5 | 10.0 / **10.0** | 11.1 | **0.3%** |

Uncapped is unusable: the median ladder hits the 41-resolution runaway guard, every
round becomes an everything-fight, and matches end in seven rounds because both
teams are wiped. A per-*team* cap is worse than useless — 100% of ladders run to
exactly the cap and passing effectively never happens, because there is no reason
to hold anything back. Only the per-champion rule produces a *distribution*
(4 through 10, mean 8.2), which means length is emerging from decisions rather than
from the rule. **XCOM's discipline is the correct answer, and this is the evidence.**

**2. "A pass ends the round" is the load-bearing rule — and it's the one you
already specified.** This is the single most important result.

| Pass rule | Search depth | Strategic passes (search declined where greedy acted) |
|---|---|---|
| Pass ends the round | 2 | **60.4%** of positions |
| Pass ends the round | 3 | **6.0%** of positions |
| Both must pass | 3 | **0.0%** of positions |

Under "both must pass," a searching agent **never once** declined to act across 199
sampled positions. That makes sense: if declining doesn't deny the opponent
anything, there is no reason to decline, and the ladder degenerates into
spend-everything. Under your rule, passing is a weapon — it closes the round and
takes the opponent's remaining actions with it — and agents genuinely use it.

**Your instinct was right and the alternative I offered was worse.**

**3. The AI is tractable — substantially better than the systems index feared.**

| Search depth | Nodes per decision (mean) | Nodes per full round (~10 decisions) |
|---|---|---|
| 2 | 314 | ~3,100 |
| 3 | 1,932 | ~19,300 |
| 4 (extrapolated) | ~12,000 | ~120,000 |

Mean branching is 27 legal actions (max 83), but the descending rule shrinks the
legal set with every response, so the tree collapses rather than explodes. Depth 4
at roughly 120k nodes per round is comfortably inside the 1.5 s AI budget in C#.
This was flagged as the project's highest-severity technical risk; it is now
materially smaller. **The action economy question and the AI question were the same
question, and capping at one action per champion answers both.**

*Caveat: the search shortlists to the 6 best-looking actions per node. That prunes
hard, and search quality — as opposed to search cost — is unvalidated.*

**4. Damage cannot deny a response.** Confirmed by construction. Because the death
check happens at round end, a champion reduced to 0 HP mid-ladder still answers.
Alpha-strike — the degenerate strategy that dominates most tactical games — does
not exist here. This is a genuinely strong property and it falls straight out of
your delayed death check.

**5. Agents systematically avoid high-initiative abilities.** Mean initiative
chosen: **1.9–2.4** on a 1–4 scale. Low-initiative abilities are safer because they
open narrow answer windows, and deeper search prefers them *more* (2.41 at depth 2 →
1.92 at depth 3). **This is a real design problem.** As specified, the initiative
curve collapses toward the bottom, and the "committer" archetype — the champion
built at initiative 4 who must open and eat the response — is not merely weak but
unplayable. High-initiative abilities need to be *substantially* stronger than
their answer window costs, or the design needs a structural reason to open high.

**6. First-mover advantage is severe: 70%.** In 150 mirror matches — identical
drafts, identical policies, opener alternating every round — the team that opens
round one won **70.0%** of the time. With random agents it was 66.7% of decided
matches. This is not draft asymmetry; it is structural, and it is far too large for
a competitive game.

### Tuning-dependent findings — directionally interesting, numerically meaningless

**7. The dying round almost never fires.** Across ~1,100 matches: 1,088–1,525
deaths from direct damage versus **1–6** from status damage. The delayed death check
grants a dying round in roughly 0.3% of deaths. The mechanic you invented is, as
tuned here, vestigial. This is *my* poison tuning being too weak relative to direct
damage — but it does show the mechanic only matters if damage-over-time is a
genuinely competitive way to kill, not a garnish.

**8. Snowballing is real.** Leader at round 3 won 74% of matches; leader at round 5,
91%. The concept flagged this risk and it is confirmed in direction, though my
scoring rates (2 points per kill, first to 20) make kills dominate and shorten
matches to ~12 rounds rather than the intended ~25.

**9. Pillar 3 holds up.** Identical drafts, played out, produced **different molded
teams in 99–100% of comparisons**, with a mean attack-stat spread of 7.7 (max 20)
between two playings of the same five champions. "Champions Arrive Unfinished" is
not just an aspiration — the mechanic does diverge.

---

## Metrics

- Matches simulated: ~1,100 across 5 action-economy variants + 300 mirror matches
- Positions interrogated by search agent: 563
- Ladder length (per-champion rule): mean 8.2, median 8, max 10
- Branching factor: mean 27.5, max 83
- Nodes/decision: 314 (d2) · 1,932 (d3)
- First-mover win rate, mirror drafts: 70.0% (n=150)
- Same-draft molding divergence: 99–100% (n=402 pairs)
- Iterations to working sim: 2 (one action-economy bug, one pass-value bug in the
  search agent — it initially mis-valued passing under the "pass ends round" rule,
  which inverted finding #2 until fixed)

**Not measured, and not measurable this way:** whether the ladder is *fun*, whether
it is legible, whether a player can hold "what can I answer with" in their head.
Those need a playable build and a human being. No headless simulation can speak to
them, and this report does not.

---

## Recommendation: **PROCEED**

The ladder survives its two hardest questions. Passing is a genuine strategic
decision rather than ceremony — but *only* under the pass-ends-the-round rule you
specified, which is a sharper piece of design than it first appeared. The search
space is tractable, which downgrades the project's largest technical risk. And
damage cannot deny a response, which structurally forecloses the alpha-strike
degeneracy that would otherwise dominate a game like this.

Two problems need solving before the GDD is finalised, and neither is fatal: the
70% first-mover advantage, and the collapse of play toward low-initiative abilities.

### If Proceeding — what must change

**Design changes required:**

1. **Fix the first-mover advantage.** Options worth modelling: compensating the
   responder (they open the next round after being closed out), a tempo cost on
   opening, or an opening bid. Do not ship without measuring this again.
2. **Make high initiative worth paying for.** Either scale power steeply with
   initiative, or give high-initiative abilities a property that low ones cannot
   have. Without this, half the initiative range is decoration.
3. **Decide whether damage-over-time is a real kill route.** The dying round is one
   of the most original ideas in the design, and at current tuning it never
   happens. It needs poison to be competitive with direct damage.
4. **Design comeback mechanics deliberately**, before items are added rather than
   after.

**Architecture requirements confirmed:**

- One action per champion per round, enforced in the simulation, not by convention
- The sim must be cloneable cheaply — the AI clones state thousands of times per
  round, so state must be compact value types, not object graphs. This is a direct
  input to the Deterministic Simulation Core ADR.
- Search depth is a *personality* axis, not just a difficulty axis (see below)

**Scope adjustment:** none. Nothing here suggests the MVP is too large.

**Estimated production effort:** unchanged from the systems index — the ladder
remains an L (4+ design sessions), but the action economy question that was
blocking it is now answered.

---

## Lessons Learned

**Search depth changes play *style*, not just strength.** A depth-2 agent passes in
68% of positions; a depth-3 agent passes in 13%. A shallow AI is not simply a
weaker opponent — it is a strange, passive one. This cuts both ways: naive
difficulty tiers built by varying depth will produce incoherent opponents, but
depth is also a ready-made axis for the *readable archetypes* the concept calls for.
"The cautious one" and "the aggressive one" may be closer to free than expected.

**The action economy and the AI budget were the same question.** The systems index
flagged this as a design-time feedback loop, and it was correct: capping at one
action per champion is simultaneously what bounds the round, what makes passing
meaningful, and what makes the tree searchable. That is a strong signal the rule is
right — a constraint that solves three problems at once usually is.

**A rule that costs the opponent something is a decision; a rule that costs only
yourself is a formality.** The difference between "a pass ends the round" and "both
must pass" is the entire difference between a ladder with strategy and a ladder
without one. Worth carrying into other systems: when adding an option, check
whether declining it takes anything from the opponent.

**For the Balance Simulation Harness (Vertical Slice):** this prototype is a crude
version of that system, and it paid for itself immediately. It found a 70%
first-mover advantage in about a minute of compute — something that would otherwise
have surfaced months into playtest as an unexplained sense that "going first feels
better." Building the real harness early is likely to be worth more than its
priority tier suggests.
