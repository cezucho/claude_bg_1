# ADR-0002: Integer-Only Deterministic Arithmetic

## Status

Proposed

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

Pillar 1 forbids randomness and requires byte-identical results across machines, which
rules out floating point in the simulation. Rather than build a fixed-point numeric
type, this ADR establishes that **the simulation is integer-only**, with fractional
scaling expressed in permille (‰) and one explicitly specified rounding rule.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — BCL integer arithmetic only; no engine API involved |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md` (determinism caution), `.claude/docs/technical-preferences.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None. `long` arithmetic is exact and platform-independent by the ECMA-335 specification |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001 |
| **Enables** | ADR-0003, ADR-0007, ADR-0009 |
| **Blocks** | All simulation implementation |
| **Ordering Note** | Must be Accepted before any formula is implemented, since it fixes how every formula is expressed |

## Context

### Problem Statement

`design/gdd/game-concept.md` Pillar 1 ("Chess, Not Dice") states that outcomes are
fully determined, and `initiative-ladder.md` acceptance criterion 21 makes byte-identical
reproduction a **blocking** gate. Asynchronous PvP additionally requires two different
machines to agree exactly.

IEEE-754 floating point does not provide this. Compiler optimisation, x87 versus SSE
paths, and fused multiply-add can all change results across platforms and builds.

### Current State

`docs/architecture/architecture.md` v1 assumed a **fixed-point** numeric type would be
needed, and recorded "fixed-point width and precision" as an open question blocking
this ADR. Investigating that question produced a better answer, recorded here. The
architecture document is updated accordingly.

### Constraints

- Every value the simulation manipulates must be exactly representable and exactly
  reproducible.
- A custom numeric type is code that must itself be written, tested and trusted.
- The simulation is cloned ~19,000 times per round; numeric representation affects
  state size.

### Requirements

- No floating point anywhere in `Augury.Sim` (TR-CONCEPT-001, TR-LADDER-015).
- Support the scaling in `initiative-ladder.md` F3, where `M = [1.0, 1.3, 2.2, 4.4]`.
- Rounding must be specified, not incidental.

## Decision

**The simulation uses `int` and `long` exclusively. There is no fixed-point type, and
no `float` or `double` may appear in `Augury.Sim`.**

Every quantity the game actually manipulates — HP, damage, initiative, cooldowns, hex
coordinates, points, stat values — is naturally an integer. The only fractional values
in the design are *scaling multipliers*, and those are expressed as **permille
integers** (parts per thousand) applied by multiply-then-divide.

The `M` multipliers of F3 become `[1000, 1300, 2200, 4400]`, and

```
raw_power = FloorDiv(base_power * M_permille, 1000)
```

`effective_value` (F4), `applicability` and `k` are **design-time balance targets, not
runtime computations**. They are evaluated by the Balance Simulation Harness and by
designers in a spreadsheet. They never execute inside a match.

### Architecture

```
   Data file (permille integers)          Runtime (exact integer math)
   ─────────────────────────────          ────────────────────────────
   power_multiplier: 2200        ──►      raw = FloorDiv(base * 2200, 1000)
   resist_permille:   150        ──►      dmg = raw - FloorDiv(raw * 150, 1000)

   No float ever exists at any point in this pipeline.
```

### Key Interfaces

```csharp
namespace Augury.Sim;

/// All scaling in the simulation goes through these. There is no other rounding.
public static class Arith
{
    /// Floor division — rounds toward negative infinity, unlike C#'s `/`,
    /// which truncates toward zero. The difference matters for negative HP
    /// (the Dying state) and for any debuff expressed as a negative delta.
    public static long FloorDiv(long numerator, long denominator)
    {
        long q = numerator / denominator;
        if ((numerator % denominator != 0) && ((numerator < 0) != (denominator < 0)))
            q--;
        return q;
    }

    /// Applies a permille scalar. THE canonical scaling operation.
    public static int ScalePermille(int value, int permille)
        => (int)FloorDiv((long)value * permille, 1000);
}
```

### Implementation Guidelines

- **Forbidden in `Augury.Sim`:** `float`, `double`, `decimal`, `Math.Round`,
  `MathF`, and bare `/` on any value that could be negative.
- Intermediate products use `long` before narrowing, so a permille scale of a large
  stat cannot overflow `int`.
- All rounding is floor, always, everywhere. There is exactly one rounding rule in the
  game and it is `Arith.FloorDiv`. Any other rounding is a bug.
- Add a CI grep for `float`/`double` in `Augury.Sim/` alongside the ADR-0001 check.

## Alternatives Considered

### Alternative 1: A fixed-point `Fixed` value type (Q32.32 or Q48.16)

- **Description**: A struct wrapping a `long`, with operator overloads for exact
  fractional arithmetic.
- **Pros**: Handles arbitrary fractional maths; familiar from deterministic-lockstep
  RTS engines.
- **Cons**: It is a numeric library — needing its own multiplication, division,
  overflow handling, rounding semantics and an exhaustive test suite. Multiplication in
  Q32.32 requires a 128-bit intermediate. It doubles state size versus `int`. **And
  this game has no genuine need for fractions at runtime.**
- **Estimated Effort**: Substantially higher — a week of work and a permanent
  correctness surface.
- **Rejection Reason**: Solving a problem the design does not have. Deterministic RTS
  engines need fixed point because they simulate continuous motion; this game moves
  units between discrete hexes and deals integer damage.

### Alternative 2: `decimal`

- **Description**: Use .NET's `decimal` for exact base-10 fractional arithmetic.
- **Pros**: Exact for the values in question; no custom type to write.
- **Cons**: 128 bits per value (inflating cloned state fourfold), an order of magnitude
  slower than integer maths, and it is a floating type — exactness is not the same as
  cross-platform bit-identity guarantees.
- **Estimated Effort**: Comparable.
- **Rejection Reason**: All the cost of fractions with none of the need.

## Consequences

### Positive

- **No numeric library to write, test, or trust.** The most likely source of subtle
  determinism bugs is removed by not creating it.
- `long` arithmetic is exact and platform-independent per ECMA-335. Determinism is a
  property of the language, not of our care.
- Smallest possible state, which directly serves ADR-0003's clone budget.
- Values in data files are human-readable: `2200` is legible as 2.2×.

### Negative

- Designers must express multipliers in permille rather than decimals. A tooling
  concern, and the authoring pipeline (ADR-0007) can present `2.2` and store `2200`.
- Repeated scaling compounds floor-rounding error. Chains of scalars must be applied in
  a **specified order**, and each formula's order is part of its GDD.
- If a future system genuinely needs fractional simulation values — a physical
  trajectory, a continuous resource — this ADR must be superseded rather than bent.

### Neutral

- Formulas in GDDs are written with decimal multipliers for human readability; the data
  files hold permille. The GDD is the specification, the data file is the encoding.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Someone writes `a / b` on a negative value and gets truncation | Medium | High — silent determinism divergence | Ban bare `/` in the sim by convention and CI grep; `Arith.FloorDiv` is the only division |
| Compounding floor error makes a formula feel wrong | Medium | Low | Specify scalar application order per formula in its GDD; the Balance Harness measures the result |
| A later system needs true fractions | Low | Medium | Supersede this ADR rather than introducing floats locally |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Arithmetic cost | n/a | Native integer ops — faster than any alternative | — |
| State size contribution | n/a | 4 bytes per value (vs 8 fixed-point, 16 decimal) | See ADR-0003 |
| AI search throughput | n/a | Improved: smaller state, cheaper maths | 1.5 s per turn |

## Migration Plan

Greenfield. This ADR **supersedes the "fixed-point" assumption** in
`docs/architecture/architecture.md` v1 and closes its Open Question 1.

**Rollback plan**: If a genuine fractional requirement appears, introduce a `Fixed`
type behind `Arith`, keeping the call sites unchanged. The abstraction point exists
precisely so this remains possible.

## Validation Criteria

- [ ] `grep -rE "\b(float|double|decimal)\b" Augury.Sim/` returns nothing.
- [ ] Determinism test: 1,000 randomly generated command sequences, replayed twice,
      produce byte-identical serialised states.
- [ ] Cross-platform test: the same replay produces identical output on Windows and
      Linux CI runners.
- [ ] `Arith.FloorDiv(-7, 2) == -4` (not `-3`).

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/initiative-ladder.md` | Deterministic Simulation Core | TR-LADDER-015 — byte-identical outputs from identical inputs | Integer arithmetic is exact and platform-independent by specification |
| `design/gdd/game-concept.md` | Deterministic Simulation Core | TR-CONCEPT-001 — no randomness anywhere (Pillar 1) | No RNG and no floating-point non-determinism exists in the sim |
| `design/gdd/initiative-ladder.md` | Initiative Ladder | F3 initiative power budget, `M = [1.0, 1.3, 2.2, 4.4]` | Encoded as permille integers, applied via `Arith.ScalePermille` |

## Related

- Depends on ADR-0001 · Enables ADR-0003, ADR-0007
- Supersedes the fixed-point assumption in `docs/architecture/architecture.md` v1
- `design/gdd/initiative-ladder.md` — Formulas F3, F4
