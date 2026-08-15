# ADR-0006: Round Phase Sequencer

## Status

Proposed

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

The ladder's round structure — two halves, a per-half action economy, and a death check
that runs *before* the status phase — is what makes the dying round exist and what
bounds the AI's search. This ADR makes that ordering an explicit state machine that
owns phase transitions, rather than behaviour distributed across gameplay systems.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — pure control flow; no engine API |
| **References Consulted** | `design/gdd/initiative-ladder.md` Core Rules 1–7, States and Transitions, Edge Cases |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0003, ADR-0004 |
| **Enables** | ADR-0008, ADR-0009 |
| **Blocks** | Initiative Ladder, Status Effects, Death & Respawn, Objectives |
| **Ordering Note** | ADR-0004 first — phase transitions emit events, so the event vocabulary must exist |

## Context

### Problem Statement

`initiative-ladder.md` fixes an exact ordering: two halves, then a death check, then
the status phase. That order is not bookkeeping — it is the mechanism that produces
the **dying round**. A champion killed by ladder damage dies at the round's death
check; a champion driven below zero by *poison*, which resolves after the death check,
survives one further round, debuffed and able to act. Reverse the two phases and one of
the game's most distinctive mechanics silently disappears.

The GDD's own Open Questions and the prototype both identify this ordering as where
subtle bugs will live. Distributing it across the systems that participate in it —
damage, statuses, death — would mean no single place is responsible for it being right.

### Current State

No code. The prototype implemented the ordering inline inside its round loop, which was
adequate for measurement and is not adequate for production.

### Constraints

- Under ADR-0004 there is one mutation path, so the sequencer drives transitions rather
  than mutating state directly.
- The action economy is per **half**, not per round — measured, not assumed
  (`prototypes/initiative-ladder/ladder_v2.py`).
- The ceiling must be monotonically non-increasing within a half; this property is what
  collapses the AI search tree (TR-LADDER-003) and cannot be weakened for convenience.

### Requirements

- A round is two halves, each opened by a different team (TR-LADDER-004).
- One action per champion per half, resetting at the half boundary (TR-LADDER-005).
- Round close runs death check then status phase, verifiable by event order
  (TR-LADDER-007).
- Dying persists one round; healing above zero clears it (TR-LADDER-008).
- Cooldowns decrement once at round close, never at a half boundary (TR-LADDER-012).
- Ceiling monotonically non-increasing within a half (TR-LADDER-003).

## Decision

**A single explicit state machine owns all phase transitions.** No gameplay system
advances a phase; systems respond to phases the sequencer announces.

### Architecture

```
  ROUND N
  ├─ HALF 1  opener = (trailing team, or alternating on a tie)
  │   ├─ ceiling ← 4
  │   ├─ all champions ← Ready            (per-HALF reset)
  │   └─ ladder: alternate, ceiling non-increasing
  │        ├─ pass       ─► LAST WORD ─► half closes
  │        └─ no legal action ─────────► half closes, NO Last Word
  │
  ├─ HALF 2  opener = the other team
  │   └─ (identical; champions Ready again)
  │
  └─ ROUND CLOSE   ── strict order, no exceptions ──
      1. DEATH CHECK    every champion at ≤0 HP dies
      2. STATUS PHASE   DoT ticks; ≤0 here ⇒ ENTERS DYING (does not die)
      3. SCORING        objectives and kills
      4. UPKEEP         cooldowns −1, shields cleared, respawn timers

         A champion already Dying at step 1 dies there.
         Healed above 0 before step 1 ⇒ Dying cleared, survives.
```

Steps 1 and 2 are the mechanism. Their order is asserted by a test, not by comment.

### Key Interfaces

```csharp
public enum Phase : byte
{
    HalfOpen, LadderDescending, LastWordOffered, HalfClosed,
    RoundClosing, MatchOver
}

public interface IRoundSequencer
{
    Phase Current { get; }
    TeamId ActiveTeam { get; }
    byte Ceiling { get; }

    /// Advances after a resolved command. Emits phase events; never mutates
    /// gameplay values itself.
    void Advance(ref MatchState state, in Command applied, EventSink events);
}
```

### Implementation Guidelines

- **The ordering of death check and status phase is asserted in a test, not documented
  in a comment.** Assert on event order: `DeathCheck` must precede `StatusPhase` in
  every round's event stream.
- The sequencer emits `HalfOpened`, `HalfClosed`, `DeathCheck`, `StatusPhase`,
  `RoundClosed`. Systems subscribe to phases; they never infer them from state.
- Champion `Ready` reset occurs at the **half** boundary. Cooldown decrement occurs at
  the **round** boundary. These are different boundaries and the distinction is
  load-bearing — a cooldown-2 ability used in half 1 is unavailable for the whole of
  the next round, not merely half 2.
- Ceiling resets to `max_initiative` on half open and only ever decreases within a
  half. Assert monotonicity in a test.
- Exhaustion and passing are **different** terminations: a pass grants a Last Word,
  running out of legal actions does not. The sequencer must not collapse them.
- The sequencer has no concept of wall-clock time. The blitz clock lives in
  presentation and produces a `Pass` command (ADR-0004).

## Alternatives Considered

### Alternative 1: Implicit ordering inside the ladder resolver

- **Description**: The ladder resolver calls the death check and status phase inline at
  the end of a round.
- **Pros**: Fewer moving parts; matches what the prototype did.
- **Cons**: No single owner of phase order. Adding any system that participates in
  round close means editing the ladder resolver, and the death-then-status order is one
  refactor away from silently inverting — taking the dying round with it.
- **Estimated Effort**: Lower.
- **Rejection Reason**: The one ordering in the game that must never drift deserves an
  owner and a test, not a convention.

### Alternative 2: Event-driven phases with subscriber-determined ordering

- **Description**: Systems subscribe to a round-close event and run in registration
  order.
- **Pros**: Loosely coupled; easy to extend.
- **Cons**: Ordering becomes an emergent property of registration order — precisely the
  kind of invisible, order-dependent behaviour that determinism forbids.
- **Estimated Effort**: Comparable.
- **Rejection Reason**: Makes a load-bearing guarantee accidental.

## Consequences

### Positive

- One place owns phase order, and a test guards it.
- The dying round is structurally protected rather than conventionally protected.
- The AI can query `Phase` and `Ceiling` directly instead of inferring them, keeping
  search-node evaluation cheap.
- Round-close ordering is visible in the event stream, so Post-Match Review and
  determinism tests both read it for free.

### Negative

- One more indirection between a command and its effects.
- Systems must be written to react to announced phases rather than acting whenever they
  like — correct, and initially unfamiliar.

### Neutral

- The state machine is small. Six phases will look like over-engineering right until
  the first time someone reorders round close by accident.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Death check and status phase inverted during a refactor | Low | **High** — the dying round vanishes silently | Event-order assertion test; ADR referenced from the control manifest |
| Half-boundary versus round-boundary resets confused | Medium | Medium | Separate named methods (`ResetForHalf`, `UpkeepForRound`); one test per boundary |
| Exhaustion and passing conflated | Medium | Medium | Distinct terminations with distinct events; explicit test for "no Last Word on exhaustion" |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Per-command overhead | n/a | One enum comparison and a switch | Negligible |
| Round-close cost | n/a | Two passes over 10 champions | Negligible |
| AI benefit | n/a | Phase and ceiling read directly, not derived | Supports the 1.5 s budget |

## Migration Plan

Greenfield.

**Rollback plan**: None required. The sequencer can absorb additional phases (an
opening phase, a shop phase) without changing its contract — which is the point of
giving it one.

## Validation Criteria

- [ ] In every round's event stream, `DeathCheck` precedes `StatusPhase`.
- [ ] A champion at 1 HP that takes 5 damage in half 1 still acts in half 2, then dies
      at round close.
- [ ] A champion driven to ≤0 by poison acts for one further round, then dies.
- [ ] A champion at −2 HP healed above 0 before the death check survives and is not Dying.
- [ ] A cooldown-2 ability used in half 1 is unavailable throughout the next round.
- [ ] Within any half, the sequence of resolved initiatives is monotonically
      non-increasing.
- [ ] A half ended by exhaustion emits no `LastWordOffered`; a half ended by a pass does.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/initiative-ladder.md` | Round Phase Sequencer | TR-LADDER-004 — a round is two halves, each team opens one | The state machine's half structure |
| `design/gdd/initiative-ladder.md` | Initiative Ladder | TR-LADDER-005 — one action per champion per half | `ResetForHalf` at the half boundary |
| `design/gdd/initiative-ladder.md` | Initiative Ladder | TR-LADDER-003 — ceiling monotonically non-increasing | Ceiling owned by the sequencer, decreasing only |
| `design/gdd/initiative-ladder.md` | Round Phase Sequencer | TR-LADDER-007 — death check then status phase | Fixed steps 1 and 2 of round close, asserted by test |
| `design/gdd/initiative-ladder.md` | Death, Dying Round & Respawn | TR-LADDER-008 — Dying persists a round; healing clears it | Dying set in step 2, resolved at the next round's step 1 |
| `design/gdd/initiative-ladder.md` | Round Phase Sequencer | TR-LADDER-012 — cooldowns tick at round close only | Upkeep, step 4, at the round boundary |

## Related

- Depends on ADR-0001, ADR-0003, ADR-0004 · Enables ADR-0008, ADR-0009
- `design/gdd/initiative-ladder.md` — Core Rules 1–7, States and Transitions
