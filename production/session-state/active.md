# Active Session State

*Last updated: 2026-08-16*

## Current Task

**Champion Data & Ability Definition Schema GDD written** —
`design/gdd/champion-and-ability-schema.md`, covering systems #4 and #5 together.
Awaiting `/design-review` in a fresh session.

Preceded by a real measurement rather than an assumption. `tools/Augury.Tools` (the
Balance Simulation Harness ADR-0001 says exists from day one) sampled 100,000
actor-positions per placement model to close ladder Open Question 2:

- **Melee is not the baseline.** A range-1 ability reaches an enemy in 50% of contested
  board states, not the 100% ladder F4 assumed. Tier-1 abilities must be ranged.
- **Rotatable pattern size is irrelevant; reach is the dial.** 2-hex line and 3-hex
  wedge measure identically (68.7%) — six facings make area nearly meaningless.
- **Fixed pattern size matters linearly**, ≈6pp per hex → 5 hexes is the tier-4 target.

`M` revised `[1.0, 1.3, 2.2, 4.4]` → `[1.0, 1.3, 2.0, 4.0]`; effective value now flat
within ±2%. The curve barely moved — the shape was right, the top tiers ~10% hot.

**Key schema decisions:** six permille stats (VIT/POW/ARM/RCH/SPD/RES), split into
continuous (drift invisibly) and threshold (floored, snap visibly); the **cross rule**
(an ability never molds the stat it scales from, so a kit is a rotation not a button);
and an **initiative budget** of 10 with ≤2 per tier, which admits exactly three kit
shapes — Ladder `1-2-3-4`, Anvil `1-1-4-4`, Vice `2-2-3-3`. The Vice cannot answer at
initiative 1 at all, which is a deliberate exploitable weakness.

**Owed to ADR-0007:** `AbilityDef` needs `MoldUp`/`MoldDown` pairs plus `ScalesFrom` to
enforce the cross rule. Additive change, no implementation exists yet, but it must land
before content authoring.

---

**Earlier: test infrastructure scaffolded AND VERIFIED — 32 tests passing.**

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
- [x] `/design-system initiative-ladder` — complete; Open Question 2 now closed
- [x] `/create-architecture` — master blueprint + ADR-0001..0007 accepted
- [x] `/test-setup` — 32 tests passing, CI in `.github/workflows/tests.yml`
- [x] Applicability measurement — `tools/Augury.Tools`, ladder F3/F4 revised
- [x] `/design-system` champion + ability schema
- [ ] `/design-review` on both GDDs — **next, fresh session**
- [ ] ADR-0007 amendment for the mold pair; ADR-0008 (AI search); ADR-0009 (replay)
- [ ] Remaining 11 design-owned MVP GDDs

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
