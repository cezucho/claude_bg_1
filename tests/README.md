# Test Infrastructure

| | |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Frameworks** | **xUnit** (simulation, headless) + **gdUnit4** (engine integration) |
| **CI** | `.github/workflows/tests.yml` |
| **Setup date** | 2026-08-14 |

## Two frameworks, and why

This project does **not** run all tests through gdUnit4, and that is deliberate.

ADR-0001 splits the codebase at an assembly boundary: `Augury.Sim` contains every
gameplay rule and has **no Godot reference at all**. Its tests therefore run under
plain xUnit via `dotnet test`, with no engine boot, in milliseconds.

`.claude/docs/technical-preferences.md` mandates this split, and it is self-policing:
**if a simulation test ever needs Godot to run, ADR-0001's boundary has been breached**
— the test suite reports the architectural drift before a reviewer would.

## Directory Layout

```
tests/
  unit/
    Augury.Sim.Tests/       xUnit. Simulation logic — headless, no engine.
      Architecture/         Executable enforcement of the control manifest's 🤖 rules
      Foundation/           Arith, HexCoord — ADR validation criteria made runnable
  integration/              gdUnit4. Engine integration — needs a Godot project.
  smoke/                    Critical path list for the /smoke-check gate
  evidence/                 Screenshots and manual sign-off records
```

`tests/unit/` holds a .NET project rather than loose test files, so the .NET solution
layout and the framework's expected paths both work. `/qa-plan`,
`/test-evidence-review` and `/smoke-check` still find what they expect.

## Running Tests

```bash
# Simulation tests — no Godot required, runs anywhere with the .NET SDK
dotnet test Augury.sln

# A single class
dotnet test --filter FullyQualifiedName~ArithTests

# Engine integration tests (once Augury.Game exists and gdUnit4 is installed)
godot --headless --path Augury.Game -s addons/gdUnit4/bin/GdUnitCmdTool.gd -a tests/integration
```

## Test Naming

- **Files**: `[System]Tests.cs`
- **Methods**: `Subject_Condition_ExpectedOutcome`
- **Example**: `ArithTests.FloorDiv_RoundsTowardNegativeInfinity`

Name the *behaviour*, not the method under test. `FloorDiv_DiffersFromCSharpDivisionOnNegatives`
tells a reader why the helper exists; `TestFloorDiv2` does not.

## Story Type → Test Evidence

| Story Type | Required Evidence | Location | Gate |
|---|---|---|---|
| Logic (formulas, AI, state machines) | Automated unit test, must pass | `tests/unit/Augury.Sim.Tests/` | **BLOCKING** |
| Integration (multi-system) | Integration test OR documented playtest | `tests/integration/` | **BLOCKING** |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` | Advisory |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` | Advisory |
| Config/Data | Smoke check pass | `production/qa/smoke-[date].md` | Advisory |

## The architecture tests

`tests/unit/Augury.Sim.Tests/Architecture/ManifestRuleTests.cs` enforces the rules
marked 🤖 in `docs/architecture/control-manifest.md`:

| Test | Enforces |
|---|---|
| `Sim_ReferencesNoGodotAssembly` | ADR-0001 — the simulation does not know Godot exists |
| `Sim_ContainsNoFloatingPointFields` | ADR-0002 — integer-only arithmetic |
| `MatchState_HasNoReferenceTypedMembers` | ADR-0003 — blittable state |
| `MatchState_IsUnderOneKilobyte` | ADR-0003 — the ~19,000-clones-per-round budget |

The last two arm themselves automatically when `MatchState` exists; until then they
pass trivially. **Do not delete them for being inert.**

## Determinism

Acceptance criterion 21 of `design/gdd/initiative-ladder.md` is a **blocking** gate:
identical inputs must produce byte-identical serialised output on any platform. That
test cannot be written until `MatchState` and `Resolve` exist, and it is the single
most important test in the project. It belongs in
`tests/unit/Augury.Sim.Tests/Determinism/` when the time comes.

## CI

Tests run on every push to `main` and every pull request. A failing suite blocks merge.
Never disable or skip a failing test to make CI pass (`coding-standards.md`).

The `integration-tests` job is guarded by `if: false` until a Godot project exists.
Remove the guard when `Augury.Game/` is created and gdUnit4 is installed.
