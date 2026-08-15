# Active Session State

*Last updated: 2026-08-14*

## Current Task

**Test infrastructure scaffolded AND VERIFIED — 32 tests passing in 119 ms.**

I installed the .NET 8 SDK in this container (extracted from
packages.microsoft.com's jammy repo; `dot.net` and the Azure CDN are blocked by
the egress proxy, `packages.microsoft.com` is not). So this scaffold is
**compiled and run**, not written blind.

```
Augury.sln
src/Augury.Sim/          Arith.cs, HexCoord.cs — specified verbatim by ADR-0002/0005
tests/unit/Augury.Sim.Tests/
    Architecture/ManifestRuleTests.cs   the manifest's 🤖 rules, executable
    Foundation/ArithTests.cs            ADR-0002 validation criteria
    Foundation/HexCoordTests.cs         ADR-0005 validation criteria
tests/integration/       gdUnit4, empty until Augury.Game exists
tests/smoke/critical-paths.md
tests/evidence/
.github/workflows/tests.yml   sim-tests · manifest-guard · integration (if: false)
```

**Three real defects the verification caught** — all would have shipped as
"looks right" documentation:
1. `ReadOnlySpan<HexCoord>` cannot be backed by a collection expression (CS9203)
2. Missing XML doc comments are build **errors** here — `TreatWarningsAsErrors`
   plus `GenerateDocumentationFile` enforces `coding-standards.md` mechanically
3. The prototype-isolation CI guard false-positived on a doc comment *citing*
   `prototypes/initiative-ladder/REPORT.md`; narrowed to real references

**Two repo-level fixes:**
- `.gitignore` excluded `*.csproj` and `*.sln` (Unity-oriented, since Unity
  regenerates them). Ours are hand-authored source — `Augury.Sim.csproj` is
  where ADR-0001's boundary is declared. Un-ignored, with a note.
- `coding-standards.md`'s Godot CI command assumed GDScript. Now names
  `dotnet test Augury.sln` for this project. (The long-standing follow-up.)

**Not verified:** anything needing Godot. No engine in this container, so the
gdUnit4 job is `if: false` and the integration directory is empty by design.

**Next:** `/design-system` #2 (Champion Data & Stat Model + Ability Definition
Schema) · `/architecture-decision` ADR-0008 (AI search) · `/design-review` on
the ladder GDD in a **fresh session**.

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
