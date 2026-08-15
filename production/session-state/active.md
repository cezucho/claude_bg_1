# Active Session State

*Last updated: 2026-08-14*

## Current Task

**Seven Foundation ADRs written**, all Status `Proposed` in `docs/architecture/`:

| ADR | Decision |
|---|---|
| 0001 | Simulation / presentation assembly boundary — `Augury.Sim` has no Godot reference |
| 0002 | **Integer-only arithmetic.** No fixed-point type; permille scalars, one floor-rounding rule |
| 0003 | `MatchState` is one blittable ~400-byte value struct; **cloning is assignment, zero allocation** |
| 0004 | Command / Event protocol — `Resolve` is the only mutation path |
| 0005 | Axial hex coordinates; patterns as offset lists, rotation as a pure function |
| 0006 | Round phase sequencer owns the death-check-then-status ordering |
| 0007 | Content is JSON loaded into immutable value tables — **not** Godot `Resource` |

Two of these overturned assumptions in `architecture.md` v1, which has been
reconciled:
- **No fixed-point type is needed.** The game moves units between discrete hexes
  and deals integer damage; the only fractional values are scaling multipliers,
  which become permille integers. A numeric library that would have needed its own
  test suite simply does not get written.
- **The array-of-structs vs struct-of-arrays question did not apply.** The whole
  state is ~400 bytes, so it is one struct and cloning is `var copy = state;`.
  ~7.6 MB of memcpy per round, zero allocations, no GC inside the AI budget.

TR registry: **23 of 25 covered**. `TR-LADDER-018` and `TR-LADDER-019` remain open,
awaiting ADR-0009 (replay format) and ADR-0008 (AI search).

**All seven are `Proposed`.** Per `docs/CLAUDE.md`, stories citing a `Proposed` ADR
are auto-blocked — they need explicit acceptance before implementation begins.

**Next:** accept the ADRs, then either `/create-control-manifest` (compiles them
into the flat programmer rules sheet) or `/design-system` #2 (Champion Data & Stat
Model + Ability Definition Schema, whose contract ADR-0005 and ADR-0007 now fix).

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
- [x] `/prototype initiative-ladder` — PROCEED; 3 rounds, 2 harness bugs found and fixed
- [x] `/design-system initiative-ladder` — 605 lines, complete
- [ ] `/design-review design/gdd/initiative-ladder.md` — **next, fresh session**
- [ ] `/create-architecture` — ADRs for the three architecture-owned systems
- [ ] Remaining 12 design-owned MVP GDDs

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

- ~~Action economy~~ **CLOSED**: one action per champion per **half**. Measured —
  per-round left a team with zero available champions entering the half it opens in
  61.8% of rounds, and ended 62% of halves by exhaustion rather than choice.
- Movement's relationship to the ladder — action with initiative, separate phase, or free?
  *(Assumed initiative-1 costing the champion's action; still unverified.)*
- **Applicability geometry** — F4 assumes tier-4 fixed patterns are legal in ~30% of
  board states. That is an assumption about hex geometry, not a decision.
- Equal-initiative exchanges — what prevents ping-pong at initiative 1?
- Negative HP and the dying round — what debuffs apply, can healing rescue?
- Map composition — lanes and minions unresolved (recommendation: cut minions,
  keep lane-shaped corridors plus a jungle).
- Points economy — what awards points, at what rate, to what total.
- Snowball risk — scaling respawns + points race + 15-minute match, with no runway.

## Highest Risks

1. **AI Opponent** — mandatory for ship, hardest technical problem in the project.
2. ~~Initiative Ladder~~ — **de-risked.** Prototyped over 3 rounds; rules hold.
3. **Determinism** — expensive to retrofit, gates PvP permanently.
4. **Molding legibility** — Pillars 1 and 5 collide; drift may read as noise.

## Follow-Ups Noted

- `.claude/docs/coding-standards.md` hardcodes the Godot CI command as
  `godot --headless --script tests/gdunit4_runner.gd`, which assumes GDScript.
  With C# it needs `dotnet test` for the sim plus a gdUnit4 pass. Fix in `/test-setup`.
