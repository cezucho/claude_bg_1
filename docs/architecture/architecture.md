# AUGURY — Master Architecture

## Document Status

| Field | Value |
|---|---|
| **Version** | 1 |
| **Last Updated** | 2026-08-14 |
| **Engine** | Godot 4.6 · C# (.NET 8+) · PC (Steam / Epic) |
| **GDDs Covered** | `design/gdd/game-concept.md`, `design/gdd/initiative-ladder.md` |
| **ADRs Referenced** | **ADR-0001 … ADR-0007 Accepted 2026-08-14.** ADR-0008 (AI search) and ADR-0009 (replay format) outstanding |
| **Scope of this pass** | **Foundation and Core specified in full.** Feature and Presentation layers carry principles and interface contracts only, to be completed as their GDDs are authored (1 of 21 MVP GDDs currently exists) |
| **Technical Director Sign-Off** | 2026-08-14 — **APPROVED**. The two conditions (Open Questions 1 and 2) were resolved by ADR-0002 and ADR-0003 |
| **Lead Programmer Feasibility** | LP-FEASIBILITY skipped — Lean review mode |

---

## Engine Knowledge Gap Summary

Godot 4.6 released January 2026; the assistant's training cutoff is May 2026, so 4.6
falls **inside** training data. Risk is **MEDIUM on recency** rather than on absence:
a version months old at cutoff is thinly represented, and 4.6 changed *defaults*,
which is more dangerous than removing APIs because wrong code still compiles.

The striking result of the requirements audit is how little of Godot's risk surface
this project touches — and that is a *consequence* of the architecture below, not luck.

| 4.6 / 4.5 change | Risk here | Why |
|---|---|---|
| Jolt becomes the default 3D physics engine | **None** | Physics is presentation-only and forbidden from carrying game state (`technical-preferences.md`). The simulation performs no physics |
| Glow processes before tonemapping | **None** in MVP | Presentation only; no glow in the MVP art target |
| D3D12 default on Windows (was Vulkan) | **Low** | Presentation only. Note for performance testing on Windows |
| Animation IK fully restored | **None** in MVP | No skeletal IK planned |
| **UI dual-focus system** — mouse/touch focus separated from keyboard/gamepad focus | **MEDIUM — the only material risk** | The ladder UI is keyboard + mouse with QWER hotkeys and dense hover inspection. This change lands directly on it. Verify before building the ladder interface |
| `duplicate()` → `duplicate_deep()` for nested Resources (4.5) | **Avoided by design** | Gameplay data never becomes a Godot `Resource` — see ADR-0007. Had abilities been Resources, deep-duplication semantics would be a hazard on every clone |
| Quaternion initialises to identity (was zero) | **None** | No quaternion use in the simulation |
| `Texture2D` → `Texture` in shader params (4.4) | **Deferred** | Relevant only when shaders are written |

**Deprecated patterns that bind this project** (from `deprecated-apis.md`): typed
signal connections rather than string-based `connect()`; `@onready` cached references
rather than `$NodePath` in per-frame code; typed collections throughout.

---

## Architecture Principles

Five principles govern every technical decision on this project. Where a decision is
ambiguous, these break the tie.

1. **The simulation does not know Godot exists.** No gameplay rule may reference an
   engine type. This is testable: `Augury.Sim` has no Godot assembly reference, and
   the build fails if one is added.
2. **No floating point in the simulation.** Determinism is a pillar requirement and a
   precondition for asynchronous PvP. **Integers only** — `int` and `long`, with
   fractional scaling expressed in permille and one rounding rule (`Arith.FloorDiv`).
   ADR-0002 examined fixed-point and rejected it: this game moves units between
   discrete hexes and deals integer damage, so it has no runtime need for fractions.
3. **Presentation reads events; it never mutates state.** The simulation is a pure
   function `(State, Command) → (State′, Event[])`. Rendering, animation and audio
   consume the event stream.
4. **Every gameplay value is data, never code.** Required by
   `.claude/docs/coding-standards.md`, and it is what lets the Balance Simulation
   Harness sweep tuning values without a rebuild.
5. **Physics is cosmetic.** Jolt may move debris. It may never be read back by
   anything that affects game state.

---

## The Simulation Boundary

This is the load-bearing decision; everything else follows from it.

```
┌──────────────────────────────────────────────────────────────┐
│  Augury.Game            (Godot 4.6 project, C#)              │
│  ───────────────────────────────────────────────────────     │
│  Board & unit presentation · Combat HUD · Ladder UI ·         │
│  Resolution playback · Draft & shop screens · Audio · Input   │
└───────────────┬──────────────────────────────▲───────────────┘
                │ Command                      │ Event[]
                ▼                              │
┌──────────────────────────────────────────────────────────────┐
│  Augury.Sim             (pure C#, NO Godot reference)        │
│  ───────────────────────────────────────────────────────     │
│  Foundation: integer arithmetic · hex model · state container│
│              round sequencer · event stream · content load   │
│  Core:       initiative ladder · damage · statuses · death   │
│              molding · movement & targeting                  │
│  Feature:    draft · opening phase · objectives · economy    │
│              AI opponent                                     │
└──────────────────────────────────────────────────────────────┘
                ▲                              ▲
                │                              │
    ┌───────────┴──────────┐      ┌────────────┴─────────────┐
    │  Augury.Sim.Tests    │      │  Augury.Tools            │
    │  xUnit, headless     │      │  Balance harness (CLI)   │
    └──────────────────────┘      └──────────────────────────┘
```

**Why this specific boundary.** It satisfies four requirements simultaneously, and no
weaker boundary satisfies all four:

- **Headless testing.** `technical-preferences.md` mandates xUnit for simulation logic
  with no Godot boot. A referenced Godot assembly would force the engine to load.
- **Determinism.** Engine float behaviour is not a determinism guarantee. Excluding
  the engine from the simulation removes the entire category of hazard.
- **AI performance.** The prototype measured ~1,900 state clones per AI decision and
  ~19,000 per round at depth 3. Cloning Godot `Node` graphs at that rate is not
  viable; cloning compact value-type arrays is trivial.
- **Asynchronous PvP.** Deferred to post-launch, but only reachable if the simulation
  can run server-side without a renderer.

**It is also self-policing.** If a simulation test ever *needs* Godot to run, the
boundary has been breached — the test suite reports the architectural drift before a
reviewer would.

---

## System Layer Map

Every system from `design/gdd/systems-index.md`, assigned to a layer.

### Platform Layer
Godot 4.6 runtime · .NET 8 · OS input, windowing, audio devices.
*Owned by the engine. We call it; we do not model it.*

### Foundation Layer — `Augury.Sim` (specified in full)

| Module | Owns | Index # |
|---|---|---|
| **Integer arithmetic** | `Arith.FloorDiv`, permille scaling — the only rounding in the game | *(new — ADR-0002)* |
| **Hex Grid & Spatial Model** | `HexCoord`, distance, adjacency, line of sight, occupancy, pattern offset resolution | 1 |
| **Deterministic Simulation Core** | `MatchState`, cloning, serialisation, replay, the `(State, Command) → (State′, Event[])` contract | 2 |
| **Round Phase Sequencer** | Round and half boundaries; the death-check-then-status ordering | 3 |
| **Event Stream** | Ordered event log; ladder history including legal-but-unused actions | *(new — implied by TR-LADDER-017/018)* |
| **Content Loading** | Champion and ability definitions from data files into sim value types | 27 (partial) |

### Core Layer — `Augury.Sim` (specified in full)

| Module | Owns | Index # |
|---|---|---|
| **Champion Data & Stat Model** | Champion record, stat block, Ready/Spent/Dying/Dead state | 4 |
| **Ability Definition Schema** | Ability record: initiative, cooldown, rigidity tier, pattern offsets, molding delta | 5 |
| **Initiative Ladder & Action Economy** | Ceiling, legality, passing, the Last Word, half termination | 8 |
| **Movement & Targeting** | Legal target sets, pattern resolution against occupancy | 6 |
| **Damage & Combat Resolution** | HP deltas, mitigation, shields | 7 |
| **Status Effects** | Status list, status-phase evaluation | 9 |
| **Death, Dying Round & Respawn** | Death check, Dying state, respawn timing | 10 |
| **Molding** | Permanent in-match stat deltas, applied after effect resolution | 11 |

### Feature Layer — `Augury.Sim` (contracts only, pending GDDs)
Draft (14) · Opening Phase (15) · Map & Terrain (12) · Objectives & Scoring (13) ·
Jungle & Neutral Powers (23) · Economy & Items (22) · **AI Opponent (16)**

### Presentation Layer — `Augury.Game` (contracts only, pending GDDs and `/ux-design`)
Board & Unit Presentation (17) · Combat HUD (18) · Initiative Ladder UI (19) ·
Resolution Playback (20) · Draft UI (21) · Shop UI (25) · Post-Match Review (26) ·
Audio (29) · Blitz Clock (24)

### Tools — `Augury.Tools`
Balance Simulation Harness (28) · Content Authoring Pipeline (27)

> **Note on the Blitz Clock.** It lives in Presentation, not the simulation. A
> timeout is converted into a `Pass` command at the boundary. The simulation has no
> concept of wall-clock time — a requirement for both replay and asynchronous PvP.

---

## Module Ownership

### Foundation

| Module | Exposes | Consumes | Engine APIs |
|---|---|---|---|
| Integer arithmetic | `Arith.FloorDiv`, `Arith.ScalePermille` | — | **None** |
| Hex Grid | `Distance`, `Neighbours`, `Line`, `ResolvePattern`, `IsOccupied` | Match state occupancy | **None** |
| Simulation Core | `ISimulation` (below) | All Core modules | **None** |
| Round Sequencer | `AdvanceHalf`, `CloseRound` | Ladder, Status, Death | **None** |
| Event Stream | `IReadOnlyList<Event>`, `LadderHistory` | All Core modules | **None** |
| Content Loading | `LoadChampions`, `LoadAbilities` | Data files (JSON) | **None** — see ADR-0007 |

**Zero engine APIs across the entire Foundation and Core layer.** That is the point,
and it is the single most useful property of this architecture: none of the HIGH or
MEDIUM engine risks identified above can reach the rules of the game.

### Presentation (contract-level, pending `/ux-design`)

| Module | Exposes | Consumes | Engine APIs (4.6 risk) |
|---|---|---|---|
| Input & Command Builder | `Command` submission | Legal command set from sim | `InputEvent` — ⚠️ **dual-focus (4.6, MEDIUM)** |
| Ladder UI | — | Legal set, ceiling, Spent champions, Last Word pending | `Control` — ⚠️ **dual-focus (4.6, MEDIUM)** |
| Resolution Playback | — | Event stream | `AnimationMixer`, `Tween` (LOW) |
| Board Presentation | — | Match state snapshot | `Node3D`, `MeshInstance3D` (LOW) |

---

## Data Flow

### 1. Player decision path

```
   Player input                    Augury.Game
        │
        ▼
   InputEvent ──► CommandBuilder ──► Command { Champion, Ability, Target }
                                         │
        ═════════ assembly boundary ═════╪═════════
                                         ▼
                                    ISimulation.Resolve(cmd)
                                         │
                                    State′ + Event[]
        ═════════ assembly boundary ═════╪═════════
                                         ▼
                       ResolutionPlayback ◄── Event[]
                       CombatHUD          ◄── State′ snapshot
```

Synchronous call. No shared mutable state crosses the boundary — commands go in by
value, events come out by value.

### 2. AI decision path

```
   ISimulation.Clone() ──► search tree (≈1,900 nodes/decision at depth 3)
        │                      │
        │                 evaluate(State)
        ▼                      │
   best Command ◄──────────────┘   budget: 1.5 s hard, 3 s ceiling
```

The AI runs entirely inside `Augury.Sim`, on clones. It never touches presentation
and never sees an engine type. Search depth is a **difficulty and personality axis**,
not merely a strength dial — the prototype found a depth-2 agent passes in 68% of
positions versus 13% at depth 3, so shallow agents play *differently*, not just worse.

### 3. Round closure

```
half 1 ladder ──► half 2 ladder ──► DEATH CHECK ──► STATUS PHASE ──► scoring
                                          │               │
                                    kills applied    dying round granted
```

Ordering is owned by the Round Phase Sequencer and is **not** negotiable by any
Core module — the death-check-then-status order is what makes the dying round exist.

### 4. Replay and serialisation

```
   initial MatchState (seed) + ordered Command log  ──►  full match reconstruction
```

Because the simulation is deterministic and side-effect free, a match is fully
described by its initial state plus its command log. Replay, post-match review, and
asynchronous PvP all consume this same representation. **No separate replay system is
required** — it is a consequence of Principles 2 and 3 rather than a feature.

### 5. Initialisation order

```
1. Content load (champions, abilities) ──► immutable definition tables
2. MatchState construction (draft result, map, spawn)
3. Sim ready
4. Presentation binds to event stream, renders initial snapshot
5. Blitz clock starts (Vertical Slice onward)
```

Presentation may not construct match state. It requests a match and binds to the
result.

---

## API Boundaries

Written in the project's language (C#, per `technical-preferences.md`). These are the
contracts programmers implement against.

```csharp
// ─── Value types. Structs, not classes: the AI clones these ~19,000×/round. ───

public readonly record struct HexCoord(int Q, int R);
// No fixed-point type: the simulation is integer-only. See ADR-0002.
public readonly record struct ChampionId(byte Value);
public readonly record struct TeamId(byte Value);

public enum ChampionState : byte { Ready, Spent, Dying, Dead }
public enum RigidityTier  : byte { Free = 1, FreeToo = 2, Rotatable = 3, Fixed = 4 }

// ─── The simulation contract ───

public interface ISimulation
{
    MatchState State { get; }

    /// Every legal command for `team` at the current ceiling.
    /// Empty means the half ends WITHOUT granting a Last Word.
    IReadOnlyList<Command> LegalCommands(TeamId team);

    /// Applies a command atomically and completely. Emits one or more events.
    /// Invariant: identical (State, Command) always yields identical (State′, Event[]).
    ResolveResult Resolve(Command command);

    /// Deep value copy. Must not allocate reference graphs — see ADR-0003.
    MatchState Clone();
}

public readonly record struct Command(
    ChampionId Champion,
    byte AbilityIndex,
    HexCoord Target,
    CommandKind Kind);          // Ability | Pass | LastWord | Decline

public readonly record struct ResolveResult(
    MatchState NewState,
    IReadOnlyList<GameEvent> Events);
```

**Invariants callers must respect**

- A `Command` not present in `LegalCommands` must be rejected, not clamped.
- `Resolve` is the *only* mutation path into match state.
- Presentation must never hold a reference to a `MatchState` it did not receive from
  `Resolve` — snapshots are values, not views.

**Guarantees the simulation makes**

- No allocation of engine types, ever.
- Determinism: identical inputs produce byte-identical serialised output, on any
  platform. This is a blocking acceptance criterion (`initiative-ladder.md` #21).
- Every resolved ability emits exactly one `AbilityResolved` event, in resolution
  order, sufficient for playback to reconstruct the round.

---

## Technical Requirements Traceability

25 requirements extracted from the concept and the ladder GDD. Full records in
`docs/architecture/tr-registry.yaml`. Coverage summary:

| Source | Requirements | Covered by a required ADR | Gaps |
|---|---|---|---|
| `initiative-ladder.md` | 20 | 20 | 0 |
| `game-concept.md` | 5 | 5 | 0 |

**Zero gaps** — but this reflects that only one GDD exists. Coverage must be
re-checked with `/architecture-review` after each additional GDD is authored.

---

## ADR Audit

No ADRs exist. `docs/architecture/tr-registry.yaml` was an empty stub before this
session. Nothing to audit; everything below is new.

---

## Required ADRs

### Must exist before any code is written — Foundation

> ✅ = written and **Accepted (2026-08-14)**. Stories may now reference these
> seven; `docs/CLAUDE.md` auto-blocks only stories citing a `Proposed` ADR.

| ADR | Decision | Covers |
|---|---|---|
| **ADR-0001** ✅ | Simulation / presentation assembly boundary | TR-LADDER-016, TR-CONCEPT-003, TR-CONCEPT-005 |
| **ADR-0002** ✅ | Determinism strategy: integer-only arithmetic, permille scalars, no floats in `Augury.Sim` | TR-LADDER-015, TR-CONCEPT-001 |
| **ADR-0003** ✅ | State representation and cloning: value types, layout, clone cost | TR-LADDER-014 |
| **ADR-0004** ✅ | Command / Event protocol and the mutation contract | TR-LADDER-001, TR-LADDER-017, TR-LADDER-020 |
| **ADR-0005** ✅ | Hex coordinate system, distance metric, pattern offset representation | TR-CONCEPT-004, TR-LADDER-009 |
| **ADR-0006** ✅ | Round phase sequencer: half boundaries, death-check-then-status ordering | TR-LADDER-004, TR-LADDER-007, TR-LADDER-008, TR-LADDER-012 |
| **ADR-0007** ✅ | Content data format and loading — why gameplay data is not a Godot `Resource` | TR-CONCEPT-002 |

### Must exist before the relevant system is built — Core

| ADR | Decision | Covers |
|---|---|---|
| **ADR-0008** | AI search architecture, budget enforcement, depth as a personality axis | TR-LADDER-019 |
| **ADR-0009** | Replay and match serialisation format | TR-LADDER-018 |

### Deferred to implementation

UI input routing under the 4.6 dual-focus system · animation queue and playback pacing ·
shader techniques · audio bus layout. None of these constrain the simulation.

---

## Open Questions

| # | Question | Blocks | Resolve by |
|---|---|---|---|
| 1 | ~~Fixed-point width and precision.~~ **CLOSED by ADR-0002** — there is no fixed-point type. The simulation is integer-only, with permille scalars and floor rounding | — | Closed |
| 2 | ~~State layout: array-of-structs or struct-of-arrays?~~ **CLOSED by ADR-0003** — neither. The whole state is ~400 bytes, so `MatchState` is one blittable value struct and cloning is assignment, with zero allocation | — | Closed |
| 3 | **Does the 4.6 dual-focus system disrupt QWER hotkeys alongside hover inspection?** The only material engine risk identified | Ladder UI | Before `/ux-design` on the HUD |
| 4 | **Is `applicability(i)` achievable with real hex geometry?** F4 assumes tier-4 patterns are legal in ~30% of board states — an assumption about geometry, not a decision | Ability Schema GDD | With the next GDD |
| 5 | **Where does the AI live at ship time?** In-process is assumed. Asynchronous PvP may want it server-side, which the boundary permits but does not yet specify | ADR-0008 | Before PvP work |
| 6 | **Snowballing.** Measured and real (leader at round 5 wins 91%). No comeback mechanic is designed | Objectives & Scoring GDD | Before that GDD is approved |

---

## Handoff

1. Write the seven Foundation ADRs, in the order listed, via `/architecture-decision`.
2. Run `/create-control-manifest` once they exist, to produce the flat programmer
   rules sheet.
3. Run `/architecture-review` after each new GDD lands, to re-check traceability.
4. Run `/gate-check pre-production` when the required ADRs are written and the
   remaining MVP GDDs are authored.
