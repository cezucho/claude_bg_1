# ADR-0003: State Representation and Cloning

## Status

Accepted (2026-08-14)

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

The AI clones full match state roughly 1,900 times per decision and 19,000 times per
round, so clone cost dominates the AI budget. This ADR makes `MatchState` a single
**blittable value struct** — cloning is assignment, with zero heap allocation and zero
GC pressure.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — .NET language features only; no engine API |
| **References Consulted** | `.claude/docs/technical-preferences.md` (AI budget), `prototypes/initiative-ladder/REPORT.md` |
| **Post-Cutoff APIs Used** | None from Godot. Uses `[InlineArray]`, a C# 12 / .NET 8 feature — within the pinned .NET 8+ target |
| **Verification Required** | Confirm `[InlineArray]` is available in the Godot 4.6 C# toolchain's language version; fall back to explicit fields if not |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0002 |
| **Enables** | ADR-0004, ADR-0008, ADR-0009 |
| **Blocks** | AI implementation; any system holding match state |
| **Ordering Note** | ADR-0002 must be Accepted first — the choice of `int` over a wider numeric type is what keeps the struct small enough for this approach |

## Context

### Problem Statement

`prototypes/initiative-ladder/REPORT.md` measured **~1,900 state clones per AI
decision** at search depth 3, and ~19,000 per round. `technical-preferences.md` sets a
**1.5 s** AI decision budget with a 3 s hard ceiling.

If a clone allocates on the heap, the AI generates ~19,000 allocations per round.
Even at a few hundred bytes each that is megabytes of garbage per round, and GC pauses
land in the middle of the exact operation the budget constrains. The representation
decision *is* the AI performance decision.

### Current State

No code exists. `docs/architecture/architecture.md` recorded "array-of-structs versus
struct-of-arrays" as an open question blocking this ADR. The answer turns out to be
neither, because the state is far smaller than that framing assumes.

### Constraints

- 10 champions (5 per team), fixed for the whole match.
- Per champion: HP, position, attack, defence, shield, 5 cooldowns, status data,
  state enum, molding deltas. All `int` or smaller under ADR-0002.
- Search is depth-first, so clones are short-lived and strictly nested.

### Requirements

- Clone cost must be negligible against a 1.5 s decision budget (TR-LADDER-014).
- State must serialise deterministically for replay and determinism tests
  (TR-LADDER-015).
- No heap allocation on the AI's hot path (TR-LADDER-019).

## Decision

**`MatchState` is a single blittable value struct. Cloning is assignment.**

```csharp
var candidate = state;          // this is the clone
candidate.Apply(command);       // mutates the copy, not the original
```

Champions are held in an `[InlineArray]` of `Champion` structs — inline storage, not a
heap array — so the whole match state is one contiguous, copyable block with no
reference members anywhere.

**Estimated size:** `Champion` ≈ 32 bytes × 10 = 320 bytes, plus round, half, ceiling,
opener, points, and per-team acted flags ≈ 64 bytes. **Roughly 400 bytes total.**

At 19,000 clones per round that is **~7.6 MB of `memcpy` per round, with zero
allocations**. A modern CPU copies that in well under a millisecond of aggregate time.
The 1.5 s budget is not remotely threatened by cloning; it will be spent on evaluation,
which is where it belongs.

### Architecture

```
MatchState  (struct, blittable, ~400 bytes, NO reference members)
├── Champions : ChampionBuffer   [InlineArray(10)] of Champion (struct)
│     └── Champion { Hp, Atk, Dfn, Shield, Pos(HexCoord), State,
│                    Cooldowns(CooldownBuffer), Poison, PoisonLeft,
│                    MoldAtk, MoldDfn, RespawnIn }
├── Round      : int
├── Half       : byte
├── Ceiling    : byte
├── Opener     : TeamId
├── Points     : PointsBuffer   [InlineArray(2)] of int
└── Flags      : uint            (bitfield: acted-this-half per champion, etc.)

Clone  =  struct assignment  =  ~400-byte memcpy  =  0 allocations
```

### Key Interfaces

```csharp
[System.Runtime.CompilerServices.InlineArray(10)]
public struct ChampionBuffer { private Champion _element0; }

[System.Runtime.CompilerServices.InlineArray(5)]
public struct CooldownBuffer { private byte _element0; }

public struct Champion
{
    public int Hp, Atk, Dfn, Shield, MoldAtk, MoldDfn;
    public HexCoord Pos;
    public ChampionState State;      // byte enum
    public CooldownBuffer Cooldowns;
    public byte Poison, PoisonLeft, RespawnIn;
}

public struct MatchState
{
    public ChampionBuffer Champions;
    public PointsBuffer Points;
    public int Round;
    public byte Half, Ceiling;
    public TeamId Opener;
    public uint Flags;              // bit i = champion i has acted this half

    /// Deterministic byte serialisation. Because the struct is blittable with
    /// no padding-dependent layout, this is a direct memory write.
    public void WriteTo(Span<byte> destination);
}
```

### Implementation Guidelines

- **Pass by `ref` or `in`, never by value, except when cloning deliberately.** A
  400-byte struct copied accidentally on every method call would erase the benefit.
  Make the copy explicit and rare: `var candidate = state;` at the search node, `ref`
  everywhere else.
- **No reference members, ever.** No `List<T>`, no arrays, no strings, no classes. A
  single reference member destroys blittability, deterministic serialisation, and the
  allocation-free property simultaneously.
- Use `[StructLayout(LayoutKind.Sequential, Pack = 1)]` if serialisation proves
  padding-sensitive across platforms; measure before applying, since packing can cost
  alignment performance.
- Champion *definitions* (name, ability list, pattern offsets) are **immutable content,
  not state** — they live in a shared read-only table loaded once (ADR-0007), and state
  refers to them by index. Definitions are never cloned.
- Search recursion depth is bounded (depth 3–4), so ~400 bytes per stack frame is
  irrelevant. If iterative deepening later pushes depth much higher, re-measure.

## Alternatives Considered

### Alternative 1: Class-based state with a `Clone()` method

- **Description**: `MatchState` as a class holding `Champion[]`; clone allocates a new
  object and copies the array.
- **Pros**: Idiomatic C#; familiar; no `InlineArray` dependency.
- **Cons**: ~19,000 heap allocations per round, GC pauses inside the AI budget, and
  reference members break deterministic byte serialisation.
- **Estimated Effort**: Slightly lower.
- **Rejection Reason**: It puts the garbage collector inside the one operation with a
  hard latency budget.

### Alternative 2: Struct-of-arrays (parallel arrays per field)

- **Description**: Separate arrays for HP, position, and so on, indexed by champion.
- **Pros**: Cache-friendly for wide operations over many entities; standard ECS layout.
- **Cons**: Optimises for entity counts in the thousands. With **ten** champions the
  entire state fits in a few cache lines either way, so SoA buys nothing and costs
  readability. Arrays are also heap references, reintroducing allocation.
- **Estimated Effort**: Higher.
- **Rejection Reason**: An optimisation for a scale this game does not have. The
  question in the architecture document assumed the state was large; it is not.

### Alternative 3: Persistent / immutable structural sharing

- **Description**: Immutable state with structural sharing between versions.
- **Pros**: Cloning is free; natural undo and replay.
- **Cons**: Allocation per modification, pointer chasing, and complexity far beyond
  what a 400-byte state warrants.
- **Estimated Effort**: Much higher.
- **Rejection Reason**: Copying 400 bytes is already cheaper than sharing it.

## Consequences

### Positive

- Zero allocation on the AI hot path; no GC pressure from search.
- Cloning is a language primitive, so there is no `Clone()` implementation to keep in
  sync as fields are added — a whole class of "forgot to copy the new field" bug simply
  cannot occur.
- Blittable layout makes deterministic serialisation nearly free, serving both replay
  (ADR-0009) and the determinism tests.
- Value semantics mean no aliasing bugs: presentation cannot accidentally hold a live
  view into simulation state.

### Negative

- Requires `ref`/`in` discipline. Accidental copies are silent and would show up only
  as diffuse slowness.
- `[InlineArray]` is a relatively recent C# feature and less familiar than arrays.
- Champion count becomes a compile-time constant (10). Changing team size means editing
  the buffer attribute — acceptable, since 5v5 is a pillar-level design fact, not a
  tuning knob.

### Neutral

- The state struct will feel unusually low-level for gameplay code. That is the correct
  trade at this one boundary and should not spread to systems above it.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| `[InlineArray]` unsupported by the Godot 4.6 C# toolchain | Low | Medium | Fall back to ten explicit `Champion` fields, or `fixed` buffers in an `unsafe` block. The decision's substance is unchanged |
| Accidental by-value passing degrades performance | Medium | Medium | Enforce `in`/`ref` in review; benchmark the AI decision path against the 1.5 s budget in CI |
| State grows beyond a comfortable copy size as systems are added | Low | Medium | Budget: keep `MatchState` under 1 KB. Re-measure when items and objectives land |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Allocations per AI decision | n/a (class design: ~1,900) | **0** | 0 on the hot path |
| Clone cost per round | n/a | ~7.6 MB memcpy, sub-millisecond aggregate | Within 1.5 s AI budget |
| State size | n/a | ~400 bytes | < 1 KB |
| GC pauses during AI turn | n/a | None from search | None |

## Migration Plan

Greenfield. Closes Open Question 2 in `docs/architecture/architecture.md` v1 — and
reframes it: the question presumed a large state, and the real answer is that the state
is small enough for the question not to apply.

**Rollback plan**: Converting to a class-based design later is mechanical. The reverse
is not, because call sites would have come to rely on reference semantics.

## Validation Criteria

- [ ] `sizeof(MatchState)` is under 1 KB, asserted in a test.
- [ ] `MatchState` contains no reference-typed members, asserted by reflection in a test.
- [ ] An AI decision at depth 3 allocates **zero** bytes, measured with
      `GC.GetAllocatedBytesForCurrentThread()`.
- [ ] AI decision at depth 3 completes within 1.5 s on target hardware.
- [ ] Two independently cloned states, mutated identically, serialise byte-identically.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/initiative-ladder.md` | Deterministic Simulation Core | TR-LADDER-014 — state cheaply cloneable at ~19,000 per round | Clone is a ~400-byte struct assignment with no allocation |
| `design/gdd/initiative-ladder.md` | Deterministic Simulation Core | TR-LADDER-015 — byte-identical serialisation | Blittable layout with no reference members serialises deterministically |

## Related

- Depends on ADR-0001, ADR-0002 · Enables ADR-0004, ADR-0008, ADR-0009
- `prototypes/initiative-ladder/REPORT.md` — the clone-count measurement that drove this
