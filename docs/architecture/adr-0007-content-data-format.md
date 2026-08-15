# ADR-0007: Content Data Format and Loading

## Status

Proposed

## Date

2026-08-14

## Last Verified

2026-08-14

## Decision Makers

User (project owner) + Claude (technical-director domain)

## Summary

The roster is the content, so the cost of adding a champion is the project's content
velocity. This ADR defines champion and ability definitions as **plain JSON loaded into
immutable simulation value types**, explicitly *not* as Godot `Resource` objects — which
keeps gameplay data available to headless tests, the balance harness, and the AI.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Godot 4.6 · C# (.NET 8+) |
| **Domain** | Core |
| **Knowledge Risk** | **LOW** — `System.Text.Json` from the BCL; no engine API |
| **References Consulted** | `docs/engine-reference/godot/deprecated-apis.md` (`duplicate()` → `duplicate_deep()`, 4.5), `.claude/docs/coding-standards.md` |
| **Post-Cutoff APIs Used** | None — and this ADR is partly *about* avoiding one |
| **Verification Required** | Confirm Godot 4.6's export pipeline includes plain `.json` files under `res://` in packaged builds |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0002, ADR-0005 |
| **Enables** | Ability Definition Schema GDD; Balance Simulation Harness |
| **Blocks** | Any champion or ability authoring |
| **Ordering Note** | ADR-0005 first — pattern offsets are part of the schema this ADR serialises |

## Context

### Problem Statement

`coding-standards.md` requires that gameplay values be data-driven and never hardcoded.
`game-concept.md` identifies the roster as the content and the Ability Definition
Schema as the project's content-velocity bottleneck: get it right and champion sixteen
takes an afternoon; get it wrong and twenty abilities need retrofitting.

The obvious Godot answer — make abilities `Resource` subclasses, author them in the
editor — is unavailable, and understanding why is the substance of this decision.

### Current State

No content pipeline. The prototype hardcoded eight champion templates in Python, which
was correct for a throwaway and is exactly what production must not do.

### Constraints

- Gameplay data is read by the simulation, which cannot reference Godot (ADR-0001).
- The balance harness and xUnit tests must load content with no engine present.
- Definitions are read constantly during AI search, so lookup must be cheap and
  allocation-free.
- All values are integers, with scalars in permille (ADR-0002).

### Requirements

- Externally configurable gameplay values (TR-CONCEPT-002).
- Ability records carry initiative, cooldown, rigidity tier, pattern offsets, molding
  delta (TR-LADDER-009, and the ladder GDD's Interactions table).
- Content loads identically in the game, the tests, and the harness.

## Decision

**Champion and ability definitions are plain JSON files, deserialised once at startup
into immutable value-type tables owned by `Augury.Sim`.**

Definitions are **content, not state**. They are loaded once, never mutated, and never
cloned. `MatchState` refers to them by index (ADR-0003), so the ~19,000 clones per
round copy a `byte` index rather than an ability definition.

### Why not Godot `Resource`

Three independent reasons, any one of which would be sufficient:

1. **It would breach ADR-0001.** `Resource` is a Godot type; the simulation could not
   read its own content.
2. **Headless tests and the balance harness could not load content**, so neither could
   run — and both are mandated by `technical-preferences.md`.
3. **It would put us directly in the path of a known 4.5 hazard.** `deprecated-apis.md`
   records `duplicate()` being superseded by `duplicate_deep()` for nested resources. An
   ability resource containing a nested pattern resource has exactly the ambiguous
   deep-copy semantics that change described — and it would bite on every copy, in a
   system copying state 19,000 times a round.

The editor-authoring convenience Godot `Resource` offers is real. It is bought back by
the authoring tool in `Augury.Tools`, which can present a friendly view and write JSON.

### Architecture

```
  assets/data/champions/*.json     ─┐
  assets/data/abilities/*.json     ─┤
                                    ├─►  ContentLoader (Augury.Sim)
                                    │      ├─ validate schema + invariants
                                    │      ├─ resolve name → index
                                    │      └─ build immutable tables
                                    │
     ┌──────────────────────────────┴───────────────────────────┐
     ▼                        ▼                                 ▼
  Augury.Game            Augury.Sim.Tests               Augury.Tools
  (loads at startup)     (loads fixtures)               (loads + sweeps)

  MatchState stores  AbilityId (byte)  ─── index into ──►  AbilityTable
  Definitions are NEVER copied into state and never cloned.
```

### Key Interfaces

```csharp
public readonly record struct AbilityId(byte Value);
public readonly record struct ChampionDefId(byte Value);

public sealed class ContentTables            // immutable after load
{
    public ReadOnlyMemory<AbilityDef>   Abilities  { get; init; }
    public ReadOnlyMemory<ChampionDef>  Champions  { get; init; }
    public ReadOnlyMemory<HexCoord>     Offsets    { get; init; }  // flat pool
}

public readonly record struct AbilityDef(
    byte Initiative,          // 1-4
    byte Cooldown,            // rounds
    RigidityTier Tier,
    ushort OffsetStart,       // slice into ContentTables.Offsets
    byte OffsetCount,
    int PowerPermille,        // ADR-0002: 2200 means 2.2x
    EffectKind Effect,
    StatId MoldStat,
    int MoldDelta);
```

Pattern offsets live in **one flat pooled array**, referenced by slice, so an
`AbilityDef` stays a small fixed-size value type with no reference members.

### Implementation Guidelines

- **JSON, not a binary format.** Content is authored by a human and lives in git;
  reviewable diffs are worth more than parse speed for a few hundred records.
- Validate on load and **fail loudly**: initiative in 1–4, cooldown in 0–4, tier 4
  abilities must declare at least one offset, permille values positive. A malformed
  content file must not produce a subtly wrong game.
- Definitions are keyed by stable string `id` in JSON and resolved to indices at load.
  JSON never contains indices; nothing breaks when a file is reordered.
- **Never mutate `ContentTables` after load.** Molding changes *state*, never
  definitions — a mutation here would leak between matches and destroy determinism.
- The authoring tool may present multipliers as `2.2` and store `2200`.
- Content files live under `assets/data/` per `.claude/docs/directory-structure.md`,
  and the Godot export must be configured to include them.

## Alternatives Considered

### Alternative 1: Godot `Resource` subclasses authored in the editor

- **Description**: `AbilityResource : Resource` with `[Export]` properties, edited in
  the Godot inspector.
- **Pros**: Best-in-class authoring UX; validation and pickers for free; the idiomatic
  Godot approach.
- **Cons**: Breaches ADR-0001; unavailable to headless tests and the harness; and lands
  on the 4.5 `duplicate()`/`duplicate_deep()` nested-resource hazard.
- **Estimated Effort**: Lower to start, and it forecloses the test strategy.
- **Rejection Reason**: Three independent blockers, any one decisive.

### Alternative 2: C# source-defined content (static classes)

- **Description**: Define champions and abilities as C# constants.
- **Pros**: Compile-time checked; no parsing; fast.
- **Cons**: Violates `coding-standards.md` — every balance tweak becomes a code change
  and a rebuild, and the balance harness cannot sweep values.
- **Estimated Effort**: Lowest.
- **Rejection Reason**: Explicitly forbidden by the project's coding standards, and it
  would make the harness useless.

### Alternative 3: A spreadsheet-driven pipeline (CSV export)

- **Description**: Author in a spreadsheet, export CSV, import at build time.
- **Pros**: Designers get a familiar tool; excellent for bulk balance passes.
- **Cons**: CSV cannot express nested pattern offsets without an escaping scheme; diffs
  are poor; a build step is needed.
- **Estimated Effort**: Higher.
- **Rejection Reason**: Pattern offsets are structurally nested. Worth revisiting for
  *balance tables* specifically if bulk tuning becomes painful.

## Consequences

### Positive

- Content loads identically in the game, the tests, and the balance harness — a single
  path with no build-time divergence.
- Balance sweeps need no rebuild, which is what makes the harness worth having.
- Reviewable content diffs in git; a champion change is legible in a pull request.
- The 4.5 nested-resource duplication hazard cannot occur.
- `MatchState` stays tiny because definitions are referenced, not embedded.

### Negative

- No editor authoring UX until `Augury.Tools` provides one. Early champions are
  hand-written JSON, which is tolerable for eight and unpleasant for sixteen.
- Runtime validation replaces compile-time checking, so schema errors surface at load
  rather than at build.
- String-to-index resolution adds a load step and a class of "unknown id" errors.

### Neutral

- Content authors will not use the Godot editor for gameplay data. Given the roster is
  the content, an authoring tool is warranted on its own merits regardless.

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Hand-authored JSON becomes a bottleneck at ~16 champions | Medium | Medium | Build the authoring tool during Vertical Slice, before the roster expands |
| Malformed content ships and misbalances the game silently | Low | High | Strict load-time validation; a content-validation test in CI over all shipped files |
| Godot export omits `.json` files from packaged builds | Low | High | Verify early; add a smoke test that loads content from an exported build |
| Someone mutates `ContentTables` to implement a buff | Low | High — cross-match leakage | Expose only `ReadOnlyMemory`; molding writes to state, never definitions |

## Performance Implications

| Metric | Before | Expected After | Budget |
|---|---|---|---|
| Content load time | n/a | A few hundred records — milliseconds | Load time budget |
| Per-lookup cost | n/a | Array index | Called constantly in AI search |
| `MatchState` contribution | n/a | 1 byte per ability reference | See ADR-0003 |

## Migration Plan

Greenfield. The prototype's eight hardcoded champion templates become the first eight
JSON fixtures — a translation, not a port, since prototype code is never migrated.

**Rollback plan**: Moving to a binary or editor-authored format later would require a
converter, but the loader interface would be unchanged.

## Validation Criteria

- [ ] `Augury.Sim.Tests` loads content fixtures with no Godot binary present.
- [ ] `Augury.Tools` loads the same shipped content files as the game.
- [ ] Content validation rejects: initiative 0 or 5, a tier-4 ability with no offsets,
      an unknown ability id, a negative permille value.
- [ ] An exported Godot build loads content successfully from `res://`.
- [ ] `ContentTables` exposes no mutable member, asserted by reflection in a test.
- [ ] Adding a champion requires **zero** code changes — verified by adding a ninth.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/game-concept.md` | Content Loading | TR-CONCEPT-002 — gameplay values data-driven, never hardcoded | JSON content files loaded at runtime; no gameplay constant in code |
| `design/gdd/initiative-ladder.md` | Ability Definition Schema | Ability records carry initiative, cooldown, rigidity tier, pattern offsets, molding delta | `AbilityDef` fields, with offsets sliced from a flat pool |
| `design/gdd/initiative-ladder.md` | Tuning Knobs | All tuning values data-driven, never hardcoded | Every knob in the GDD's Tuning Knobs table maps to a content or config field |

## Related

- Depends on ADR-0001, ADR-0002, ADR-0005
- Enables the Ability Definition Schema GDD (next in the design order) and the Balance
  Simulation Harness
- `docs/engine-reference/godot/deprecated-apis.md` — the 4.5 `duplicate_deep()` change
  this decision routes around
