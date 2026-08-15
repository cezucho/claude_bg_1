# Active Session State

*Last updated: 2026-08-14*

## Current Task

`design/gdd/initiative-ladder.md` is **COMPLETE** — all 8 required sections plus
Visual/Audio, UI Requirements and Open Questions. 605 lines, no placeholders.
Status: Designed, pending review.

**Next:** run `/design-review design/gdd/initiative-ladder.md` in a **fresh
session** (never in the authoring session — the reviewer must be independent).
Then `/design-system` for #2 in the design order: Champion Data & Stat Model +
Ability Definition Schema.

**Highest-priority follow-up:** Open Question 1 — re-run
`prototypes/initiative-ladder/` with the two-half structure and the Last Word
rule. Both weaken the denial that made passing a real decision (6% strategic
passing measured; 0% when denial was removed). This must be settled before
implementation.

Key rules settled in Detailed Design:
- Round has **two halves**; each team opens one. Removes the 70% first-mover
  advantage the prototype measured.
- **One action per champion per round**, across both halves — so teams must
  allocate five champions between the half they open and the half they answer in.
- **Pass grants the opponent one unanswerable "Last Word"**, then the half ends.
  Stops passing being a free combo-breaker; makes baiting a pass a real line.
- **Targeting rigidity scales with initiative**: 1–2 free, 3 rotatable pattern,
  4 fixed absolute pattern. High initiative is strong but conditional on
  position, which is what stops play collapsing to low initiative.
- Round closes: death check, *then* status phase. Status-induced death grants a
  dying round.

**Needs re-measuring in the prototype before implementation:** the two-half
structure plus the Last Word both weaken the denial that made passing a real
decision (measured at 6% strategic passes; 0% when denial was removed entirely).

## Project

**AUGURY (working title)** — turn-based tactics / MOBA hybrid. One player commands
five drafted champions on a hex map; abilities carry an **initiative** value and
both sides trade down a descending response ladder.

- Phase: **Concept → Systems Design**
- Engine: Godot 4.6, C# (.NET 8+), PC
- Review mode: `lean` (director gates fire only at `/gate-check`)

## Progress

- [x] `/start` — onboarding, review mode set to lean
- [x] `/brainstorm` — `design/gdd/game-concept.md` written
- [x] `/setup-engine` — Godot 4.6 + C# pinned; `CLAUDE.md`, `technical-preferences.md`, `VERSION.md` updated
- [x] `/map-systems` — `design/gdd/systems-index.md` written (33 systems, 21 MVP)
- [ ] `/design-system initiative-ladder` — **next**
- [ ] Remaining 12 design-owned MVP GDDs
- [ ] `/create-architecture` — ADRs for the three architecture-owned systems
- [ ] `/prototype initiative-ladder`

## Key Decisions

- **Initiative ladder**: an ability at initiative N may be answered by any ability
  at initiative ≤ N, from any champion; the ladder descends until someone passes.
- **Round order**: ladder exchange → death check → status resolution. A champion
  taken to ≤0 HP by statuses survives one more round, debuffed, before dying.
- **Molding**: ability use applies permanent in-match stat changes. Champions
  arrive from the draft unfinished.
- **Three phases**: draft → opening phase (one ability per champion, starts on
  cooldown) → tactical combat. ~25 turns, 10–15 minutes.
- **Turn structure**: alternating, not simultaneous — the ladder supplies the
  collision feel that simultaneous commit would have provided.
- **Action Economy merged into the Initiative Ladder GDD** — they define each other.
- **Physics is presentation-only.** Determinism is a pillar requirement and a
  precondition for post-launch async PvP.
- **Tests split**: xUnit for engine-independent simulation, gdUnit4 for engine
  integration. If a sim test needs Godot to run, the architecture has drifted.
- Items and blitz clock deferred to Vertical Slice — validate one economy first.

## Files Written This Session

| File | Purpose |
|------|---------|
| `design/gdd/game-concept.md` | Full game concept: pitch, pillars, loop, MVP, scope, risks |
| `design/gdd/systems-index.md` | 33 systems with dependencies, priorities, design order, risks |
| `CLAUDE.md` | Technology stack → Godot 4.6 / C# |
| `.claude/docs/technical-preferences.md` | Engine, input, naming, budgets, testing, routing |
| `docs/engine-reference/godot/VERSION.md` | Corrected LLM cutoff (May 2026); 4.6 risk HIGH → MEDIUM |
| `production/review-mode.txt` | `lean` |

## Open Questions

- **Action economy per round** — what limits how many abilities are spent in one
  ladder? Bounds both match length and AI tractability. Settle in prototype.
- Movement's relationship to the ladder — action with initiative, separate phase, or free?
- Equal-initiative exchanges — what prevents ping-pong at initiative 1?
- Negative HP and the dying round — what debuffs apply, can healing rescue?
- Map composition — lanes and minions unresolved (recommendation: cut minions,
  keep lane-shaped corridors plus a jungle).
- Points economy — what awards points, at what rate, to what total.
- Snowball risk — scaling respawns + points race + 15-minute match, with no runway.

## Highest Risks

1. **AI Opponent** — mandatory for ship, hardest technical problem in the project.
2. **Initiative Ladder** — the whole game rests on it; prototype before finalising.
3. **Determinism** — expensive to retrofit, gates PvP permanently.
4. **Molding legibility** — Pillars 1 and 5 collide; drift may read as noise.

## Follow-Ups Noted

- `.claude/docs/coding-standards.md` hardcodes the Godot CI command as
  `godot --headless --script tests/gdunit4_runner.gd`, which assumes GDScript.
  With C# it needs `dotnet test` for the sim plus a gdUnit4 pass. Fix in `/test-setup`.
