# ADR-0001: Simulation / Presentation Assembly Boundary

## Status

Proposed

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

The game's rules must be testable headlessly, byte-deterministic, cloneable
thousands of times per AI decision, and runnable server-side for future PvP. This
ADR splits the codebase into `Augury.Sim` — a pure C# assembly with **no reference
to Godot at all** — and `Augury.Game`, the Godot project that renders it.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — the decision's entire purpose is to remove engine surface from the rules |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None. `Augury.Sim` uses no engine API whatsoever |
| **Verification Required** | Confirm a Godot C# project can reference a plain .NET class library and that `dotnet test` runs the library's tests without invoking the Godot binary |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | None — this is the root decision |
| **Enables** | ADR-0002, ADR-0003, ADR-0004, ADR-0007, ADR-0008, ADR-0009 |
| **Blocks** | Every implementation epic. Nothing may be coded before this is Accepted |
| **Ordering Note** | Must be Accepted first. Every other Foundation ADR assumes this boundary exists |

## Context

### Problem Statement

Four requirements, drawn from separate documents, all constrain where game logic may
live. Deciding them separately would produce four incompatible answers:

- `technical-preferences.md` mandates xUnit for simulation logic, run headless via
  `dotnet test`, with no Godot boot.
- `game-concept.md` Pillar 1 forbids randomness and requires that identical inputs
  produce byte-identical outputs, which is also a precondition for asynchronous PvP.
- `prototypes/initiative-ladder/REPORT.md` measured ~1,900 state clones per AI
  decision and ~19,000 per round at search depth 3.
- The concept defers asynchronous PvP to post-launch but requires the architecture
  not to preclude it.

The cost of not deciding now is total. Retrofitting a simulation boundary after
gameplay code exists means rewriting every system, and the project would discover
the need at exactly the point where rewriting is most expensive.

### Current State

No code exists. `src/` contains a `.gitkeep`.

### Constraints

- Godot C# projects are .NET projects; referencing a plain class library is standard.
- The team is one developer. Two assemblies is close to the maximum structural
  complexity worth carrying.
- Godot's `Node`, `Resource` and `Variant` types are reference types with engine-owned
  lifetimes. They cannot be cloned cheaply, and their float behaviour is not a
  determinism guarantee.

### Requirements

- Simulation tests execute without loading the Godot runtime (TR-LADDER-016).
- Identical inputs produce byte-identical serialised output (TR-LADDER-015).
- A full state clone must be cheap enough for ~19,000 per round (TR-LADDER-014).
- The simulation must be runnable without a renderer (TR-CONCEPT-003).
- Physics must never carry game state (TR-CONCEPT-005).

## Decision

The codebase is split into four projects. **`Augury.Sim` has no Godot reference and
never will.**

### Architecture

```
Augury.sln
│
├── Augury.Sim/            net8.0   — NO Godot reference
│     Foundation:  Integer math helpers · HexCoord · MatchState ·
│                  RoundSequencer · EventStream · ContentLoader
│     Core:        InitiativeLadder · Damage · StatusEffects ·
│                  DeathAndRespawn · Molding · MovementTargeting
│     Feature:     Draft · OpeningPhase · Objectives · Economy · Ai
│
├── Augury.Sim.Tests/      net8.0   — xUnit; references Augury.Sim only
│
├── Augury.Tools/          net8.0   — Balance harness CLI; references Augury.Sim
│
└── Augury.Game/           Godot 4.6 C# project — references Augury.Sim
      Presentation: BoardView · CombatHud · LadderUi · ResolutionPlayback ·
                    DraftScreen · ShopScreen · Audio · InputCommandBuilder
```

Dependencies point **inward only**. `Augury.Sim` depends on nothing but the BCL.

### Key Interfaces

```csharp
// Augury.Sim — the entire surface presentation is permitted to touch.
public interface ISimulation
{
    MatchState State { get; }
    IReadOnlyList<Command> LegalCommands(TeamId team);
    ResolveResult Resolve(Command command);
    MatchState Clone();
}
```

Presentation submits `Command` values and consumes `GameEvent` values. It never holds
a mutable reference into simulation state.

### Implementation Guidelines

- `Augury.Sim.csproj` must **not** contain `<Reference Include="GodotSharp" />` or any
  Godot package reference. Enforce it in CI: fail the build if the compiled assembly's
  referenced assemblies include anything Godot.
- No `using Godot;` anywhere under `Augury.Sim/`.
- Godot-derived classes must be `partial` (`technical-preferences.md`) — this applies
  only to `Augury.Game`, and is a useful smell test: a `partial` class in the sim
  assembly means something has gone wrong.
- `Augury.Tools` exists from day one, not at Vertical Slice. The prototype
  demonstrated that a harness finds structural bugs that aggregate metrics hide.

## Alternatives Considered

### Alternative 1: Single Godot project, discipline-enforced separation

- **Description**: One assembly; keep gameplay logic in classes that happen not to use
  engine types, enforced by code review.
- **Pros**: Simplest project structure; no cross-assembly friction.
- **Cons**: Nothing prevents drift. The first time someone needs a `Vector3` or a
  `Node` reference "just here", determinism and headless testing are both lost, and
  the loss is silent.
- **Estimated Effort**: Lower initially, far higher later.
- **Rejection Reason**: Unenforceable. The whole value of the boundary is that it is
  mechanically checkable.

### Alternative 2: Simulation as a separate process or service

- **Description**: Run the simulation out-of-process, communicating over IPC.
- **Pros**: Absolute isolation; closest to the eventual PvP server topology.
- **Cons**: Enormous complexity for a single-player game — serialisation on every
  decision, process lifecycle, debugging across a boundary.
- **Estimated Effort**: Much higher.
- **Rejection Reason**: Solves a problem the project does not have yet. The assembly
  boundary keeps the option open at a fraction of the cost.

## Consequences

### Positive

- Simulation tests run in milliseconds, without an engine.
- The whole category of engine-version risk is removed from the rules of the game.
  Jolt becoming the default physics engine, the 4.6 glow reorder, D3D12 on Windows —
  none can reach gameplay.
- The AI can clone state freely.
- Asynchronous PvP remains reachable without a rewrite.
- **The boundary is self-policing**: a simulation test that needs Godot to run has
  already breached it, and the test suite reports the drift.

### Negative

- Presentation cannot read simulation state directly; it works from snapshots and
  events. This is more code than direct access, and it is the price of the property.
- Two projects to configure, build and debug.
- Some duplication of small value types at the boundary (a `HexCoord` in the sim and a
  `Vector3` position in the view) is unavoidable and correct.

### Neutral

- The simulation is not "the game" in a Godot sense — it has no scene tree. Anyone
  coming from a Godot-first background will find this unfamiliar before finding it
  freeing.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Boundary erodes under deadline pressure | Medium | High | CI check on referenced assemblies; a failing build, not a review comment |
| Snapshot copying becomes a presentation bottleneck | Low | Medium | State is ~400 bytes (ADR-0003); copying it per frame is negligible |
| Developer friction leads to "just this once" leakage | Medium | High | The CI check makes the exception impossible rather than discouraged |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Simulation test suite runtime | n/a | < 5 s (no engine boot) | CI must stay fast enough to run per-push |
| AI clone cost | n/a | See ADR-0003 | 1.5 s per AI turn |
| Frame time | n/a | Unaffected — presentation only | 16.6 ms |

## Migration Plan

Greenfield. No migration.

**Rollback plan**: Merging the assemblies is mechanically easy (move files, delete a
project reference). Splitting them later is not. The asymmetry is the argument for
starting split.

## Validation Criteria

- [ ] `dotnet test Augury.Sim.Tests` passes with the Godot binary absent from PATH.
- [ ] CI fails if any Godot assembly appears in `Augury.Sim`'s reference set.
- [ ] `grep -r "using Godot" Augury.Sim/` returns nothing.
- [ ] A full match can be simulated by `Augury.Tools` with no renderer.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/initiative-ladder.md` | Deterministic Simulation Core | TR-LADDER-016 — simulation runs headless via `dotnet test` | The sim assembly has no engine reference, so no engine can be loaded |
| `design/gdd/game-concept.md` | Deterministic Simulation Core | TR-CONCEPT-003 — async PvP addable without a rewrite | The sim runs without a renderer, which is the precondition |
| `design/gdd/game-concept.md` | Deterministic Simulation Core | TR-CONCEPT-005 — physics never carries game state | Physics types are not reachable from the sim assembly |

## Related

- Enables ADR-0002 (arithmetic), ADR-0003 (state layout), ADR-0004 (protocol)
- `docs/architecture/architecture.md` — "The Simulation Boundary"
- `prototypes/initiative-ladder/REPORT.md` — clone-count measurements
