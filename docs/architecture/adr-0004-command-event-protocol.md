# ADR-0004: Command / Event Protocol

## Status

Accepted (2026-08-14)

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

Presentation, the AI, the test suite and a future PvP server all need to drive the
simulation, and all need to observe what happened. This ADR defines the single
mutation path — `(State, Command) → (State′, Event[])` — with commands as the only way
in and an ordered event stream as the only way out.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — plain C# types; the protocol exists precisely to keep engine types out |
| **References Consulted** | `design/gdd/initiative-ladder.md` (F1, Core Rules, Edge Cases), `.claude/docs/technical-preferences.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None for the simulation side. The presentation side's input handling must be checked against Godot 4.6's dual-focus system — tracked separately |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0003 |
| **Enables** | ADR-0006, ADR-0008, ADR-0009 |
| **Blocks** | All gameplay implementation; the ladder UI |
| **Ordering Note** | ADR-0003 first — commands and events are value types under the same no-reference-members constraint |

## Context

### Problem Statement

Four consumers must drive and observe the simulation: the player through
presentation, the AI through search, the test suite, and eventually a PvP server. If
each reaches into state directly, determinism becomes unverifiable, replay becomes
impossible, and Pillar 1's promise that every defeat is legible has no mechanism
behind it.

The ladder GDD also requires that presentation know exactly **why** each ability is
unavailable — cooldown, above ceiling, or no legal target are three different
decisions and must not look alike (TR-LADDER-020). That information has to cross the
boundary in a structured form.

### Current State

No code exists. `architecture.md` established the protocol shape; this ADR fixes it.

### Constraints

- Commands and events cross the assembly boundary, so both must be free of engine types.
- Under ADR-0003 they should be value types with no reference members.
- The ladder resolves strictly sequentially, with no simultaneity anywhere
  (TR-LADDER-001) — so the event stream is a simple ordered list, not a graph.

### Requirements

- One mutation path only (TR-LADDER-001, TR-LADDER-002).
- One `AbilityResolved` event per resolved ability, in resolution order, sufficient for
  playback to reconstruct the round (TR-LADDER-017).
- Legal command enumeration exposes per-ability unavailability reasons (TR-LADDER-020).
- A blitz-clock timeout is expressible as an ordinary command (TR-LADDER-013).
- Molding applies **after** its ability's effect resolves (TR-LADDER-011).

## Decision

**`Resolve(Command)` is the only function that mutates match state.** It is total and
deterministic: the same `(State, Command)` always yields the same `(State′, Event[])`.

Four command kinds cover the entire game: `Ability`, `Pass`, `LastWord`, `Decline`.
A blitz-clock expiry is converted at the presentation boundary into a `Pass` — the
simulation has no concept of wall-clock time, which is what makes replay and
asynchronous PvP possible (TR-LADDER-013).

### Architecture

```
  Presentation                    Augury.Sim
  ────────────                    ──────────
  InputEvent ──► CommandBuilder
                      │
                      ▼
                  Command ─────────► Resolve(cmd)
                                          │
                                     ┌────┴─────────────────────┐
                                     │ 1. validate legality (F1)│
                                     │ 2. apply effect          │
                                     │ 3. apply molding delta   │  ← strictly after (2)
                                     │ 4. update ceiling        │
                                     │ 5. emit events           │
                                     └────┬─────────────────────┘
                                          ▼
  ResolutionPlayback ◄──────────── (State′, Event[])
  CombatHUD          ◄──────────── State′ snapshot (by value)
```

Steps 2 and 3 are ordered, not incidental: an ability never benefits from its own
molding delta (TR-LADDER-011).

### Key Interfaces

```csharp
public enum CommandKind : byte { Ability, Pass, LastWord, Decline }

public readonly record struct Command(
    CommandKind Kind,
    ChampionId Champion,     // ignored for Pass / Decline
    byte AbilityIndex,       // 0-4; index 4 is Reposition
    HexCoord Target);

public enum Unavailable : byte
{
    None = 0,
    Cooldown,        // ability is cooling down
    AboveCeiling,    // initiative exceeds the current ladder ceiling
    NoLegalTarget,   // pattern does not line up — the common tier-4 case
    ChampionSpent,   // this champion already acted this half
    ChampionDead
}

/// Presentation renders these three reasons DIFFERENTLY (TR-LADDER-020).
public readonly record struct AbilityAvailability(
    ChampionId Champion, byte AbilityIndex, Unavailable Reason);

public enum EventKind : byte
{
    HalfOpened, AbilityResolved, DamageDealt, StatusApplied,
    MoldingApplied, LastWordOffered, LastWordTaken, LastWordDeclined,
    HalfClosed, DeathCheck, StatusPhase, ChampionDied,
    ChampionEnteredDying, ChampionRescued, PointsScored, RoundClosed
}

public readonly record struct GameEvent(
    EventKind Kind, ChampionId Source, ChampionId Target, HexCoord At, int Value);

public readonly record struct ResolveResult(
    MatchState NewState,
    ReadOnlyMemory<GameEvent> Events);

public interface ISimulation
{
    IReadOnlyList<Command> LegalCommands(TeamId team);
    IReadOnlyList<AbilityAvailability> Availability(TeamId team);  // for the UI
    ResolveResult Resolve(Command command);
}
```

### Implementation Guidelines

- **`Resolve` rejects illegal commands; it never clamps or corrects them.** A command
  absent from `LegalCommands` is a programming error, surfaced as such.
- Events are emitted in resolution order and never reordered. Playback reads the list
  front to back; that is the entire contract.
- `Availability` exists for the UI, not the AI. The AI uses `LegalCommands`, which
  returns only what is playable.
- **Presentation must never construct a `MatchState`.** It receives snapshots by value
  from `ResolveResult` and treats them as read-only.
- Event payloads carry the numbers presentation needs (damage dealt, stat delta), so
  playback never recomputes anything. Recomputation in presentation is a determinism
  hazard by another name.

## Alternatives Considered

### Alternative 1: Direct method calls on state objects

- **Description**: Presentation calls `champion.TakeDamage(5)` and similar.
- **Pros**: Simplest to write; immediately familiar.
- **Cons**: No single mutation path, so no replay, no determinism verification, and no
  natural event stream for playback. Every caller becomes a place where an invariant
  can be broken.
- **Estimated Effort**: Lower initially.
- **Rejection Reason**: Forfeits replay, determinism testing, and async PvP — three
  requirements for one convenience.

### Alternative 2: Godot signals as the event mechanism

- **Description**: The simulation emits Godot `[Signal]` events that presentation binds to.
- **Pros**: Idiomatic Godot; automatic editor integration.
- **Cons**: Requires the simulation to be a Godot object, violating ADR-0001 outright.
  Signals are also fire-and-forget, whereas replay needs an inspectable ordered log.
- **Estimated Effort**: Comparable.
- **Rejection Reason**: Breaks the boundary. Presentation may translate events into
  signals on its own side — that is the correct place for it.

### Alternative 3: Full event sourcing, with state derived from events

- **Description**: Events are primary; state is a fold over the event log.
- **Pros**: Perfect audit trail; time-travel debugging.
- **Cons**: Every state read means replaying or maintaining a projection — catastrophic
  for an AI evaluating ~1,900 nodes per decision.
- **Estimated Effort**: Higher.
- **Rejection Reason**: State-plus-events gives the same replay property at a fraction
  of the read cost. The command log alone already reconstructs any match (ADR-0009).

## Consequences

### Positive

- Replay, determinism testing, and async PvP all become consequences of the protocol
  rather than separate features.
- The event stream is exactly what Resolution Playback and Post-Match Review need,
  with no adapter layer.
- `Availability` gives the ladder UI structured reasons, so distinguishing
  "on cooldown" from "above ceiling" from "no target" is a rendering choice, not an
  inference problem.
- The AI and the player use identical entry points, so anything the AI can do the
  player can, and vice versa.

### Negative

- More verbose than direct mutation. Every interaction is a command type and an event
  type.
- Event enumeration must be kept in step with gameplay features; a new mechanic that
  emits no event is invisible to playback.
- Snapshot-by-value means presentation reads slightly stale state between resolutions —
  correct, but it requires care in the HUD.

### Neutral

- The protocol will feel like more ceremony than a single-player game needs, right up
  until replay, determinism tests, or PvP are wanted.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Event kinds proliferate and playback drifts out of sync | Medium | Medium | One test asserting every `EventKind` has a playback handler |
| Presentation recomputes values instead of reading event payloads | Medium | High — silent divergence | Event payloads carry final numbers; forbid recomputation in review |
| Command validation duplicated between UI and sim | Medium | Low | `LegalCommands` is the single source; the UI renders it and never re-derives it |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Events per round | n/a | ~16 resolutions × 2–4 events ≈ 50 | Negligible |
| Event allocation | n/a | One pooled buffer per resolve; no per-event allocation | 0 on the AI hot path |
| AI overhead from the protocol | n/a | `LegalCommands` per node — the dominant cost, as intended | 1.5 s per turn |

## Migration Plan

Greenfield. No migration.

**Rollback plan**: None needed; the protocol is additive. New command and event kinds
extend the enums without breaking existing handlers.

## Validation Criteria

- [ ] Every state mutation in `Augury.Sim` occurs inside `Resolve`, verified by review
      and by the absence of public setters on `MatchState`.
- [ ] Replaying a recorded command log reproduces a byte-identical final state.
- [ ] Every resolved ability emits exactly one `AbilityResolved` event, in order.
- [ ] `Availability` distinguishes all six `Unavailable` reasons in a UI test.
- [ ] A `Pass` command produced by clock expiry is indistinguishable from a deliberate
      pass in both state and events (TR-LADDER-013).

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/initiative-ladder.md` | Initiative Ladder | TR-LADDER-001 — strictly sequential resolution, no simultaneity | `Resolve` handles one command at a time and emits an ordered event list |
| `design/gdd/initiative-ladder.md` | Initiative Ladder | TR-LADDER-002 — legal action predicate F1 | `LegalCommands` implements F1; `Resolve` rejects anything outside it |
| `design/gdd/initiative-ladder.md` | Initiative Ladder | TR-LADDER-006 — pass grants one unanswerable Last Word | `LastWord` and `Decline` command kinds, with `LastWordOffered/Taken/Declined` events |
| `design/gdd/initiative-ladder.md` | Molding | TR-LADDER-011 — molding applies after the ability's own effect | Resolution steps 2 then 3, fixed in order |
| `design/gdd/initiative-ladder.md` | Blitz Clock | TR-LADDER-013 — timeout equals a pass | Presentation converts expiry into a `Pass` command; the sim has no clock |
| `design/gdd/initiative-ladder.md` | Event Stream | TR-LADDER-017 — one event per resolved ability, in order | `AbilityResolved`, emitted once per resolution |
| `design/gdd/initiative-ladder.md` | Ladder UI | TR-LADDER-020 — three distinct unavailability reasons | `AbilityAvailability` with the `Unavailable` enum |

## Related

- Depends on ADR-0001, ADR-0003 · Enables ADR-0006, ADR-0008, ADR-0009
- `design/gdd/initiative-ladder.md` — Core Rules, Edge Cases, UI Requirements
