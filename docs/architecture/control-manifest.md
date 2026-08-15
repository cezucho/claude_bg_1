# Control Manifest

> **Engine**: Godot 4.6 · C# (.NET 8+)
> **Last Updated**: 2026-08-14
> **Manifest Version**: 2026-08-14
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007
> **Status**: Active — regenerate with `/create-control-manifest` when ADRs change

`Manifest Version` is the date this manifest was generated. Story files embed this
date when created; `/story-readiness` compares a story's embedded version against this
field to detect stories written against stale rules.

This is a programmer's quick-reference extracted from all Accepted ADRs, technical
preferences, and engine reference docs. **For the reasoning behind any rule, read the
referenced ADR** — this document deliberately states *what*, not *why*.

**🤖 = enforceable by CI**, not by review. Rules a machine checks are worth more than
rules a human remembers.

---

## ⚠️ Known Gap

**ADR-0008 (AI search architecture) and ADR-0009 (replay format) are not written.**
The Feature layer below is therefore thin, and the AI — the project's
highest-severity technical risk — is governed only by its time budget. Do not read the
sparseness of the Feature section as permission. `TR-LADDER-018` and `TR-LADDER-019`
remain `open` in `tr-registry.yaml`.

---

## Foundation Layer Rules

*Applies to: `Augury.Sim` — integer arithmetic, hex model, `MatchState`, round
sequencer, event stream, content loading*

### Required Patterns

- **🤖 `Augury.Sim` has no Godot assembly reference.** CI fails the build if one
  appears in the compiled assembly's reference set — ADR-0001
- **🤖 No `using Godot;` anywhere under `Augury.Sim/`** — ADR-0001
- **`Augury.Tools` (balance harness) exists from day one**, not at Vertical Slice —
  ADR-0001
- **All scaling goes through `Arith.ScalePermille`.** Multipliers are permille
  integers: `2200` means 2.2× — ADR-0002
- **All rounding is `Arith.FloorDiv`, always, everywhere.** There is exactly one
  rounding rule in this game. Any other rounding is a bug — ADR-0002
- **Intermediate products use `long` before narrowing to `int`**, so a permille scale
  of a large stat cannot overflow — ADR-0002
- **Pass `MatchState` by `ref` or `in`.** A copy is explicit and rare:
  `var candidate = state;` at a search node, `ref` everywhere else — ADR-0003
- **🤖 `MatchState` and everything inside it contains no reference-typed members** —
  no `List<T>`, no arrays, no strings, no classes — ADR-0003
- **Champion and ability definitions are content, not state.** `MatchState` refers to
  them by index; definitions are never copied into state and never cloned — ADR-0003,
  ADR-0007
- **`Resolve(Command)` is the only function that mutates match state** — ADR-0004
- **Events are emitted in resolution order and never reordered** — ADR-0004
- **Event payloads carry final numbers** (damage dealt, stat delta) so presentation
  never recomputes anything — ADR-0004
- **An ability's effect resolves before its molding delta is applied.** An ability
  never benefits from its own stat change — ADR-0004
- **The round sequencer owns every phase transition.** Systems respond to announced
  phases; they never advance one — ADR-0006
- **🤖 Death check precedes the status phase, asserted by a test on event order** —
  not by a comment — ADR-0006
- **Champion `Ready` resets at the HALF boundary. Cooldowns decrement at the ROUND
  boundary.** Different boundaries, and the distinction is load-bearing — ADR-0006
- **Ceiling resets to `max_initiative` on half open and only ever decreases within a
  half.** Assert monotonicity in a test — ADR-0006
- **Content is JSON, validated on load, failing loudly.** Initiative in 1–4, cooldown
  in 0–4, tier-4 abilities must declare at least one offset, permille values positive
  — ADR-0007
- **Content is keyed by stable string `id` in JSON, resolved to indices at load.**
  JSON never contains indices — ADR-0007
- **Content files live under `assets/data/`** — ADR-0007, `directory-structure.md`

### Forbidden Approaches

- **🤖 Never reference Godot from the simulation.** Not "prefer not to" — the build
  fails. The boundary's entire value is that it is mechanically checkable — ADR-0001
- **🤖 Never use `float`, `double`, `decimal`, `Math.Round` or `MathF` in
  `Augury.Sim`** — ADR-0002
- **Never use bare `/` on a value that could be negative.** C# truncates toward zero;
  the game floors. This matters for negative HP (the Dying state) and every negative
  delta — ADR-0002
- **Never add a reference-typed member to `MatchState`.** A single one destroys
  blittability, deterministic serialisation, and the allocation-free property at once
  — ADR-0003
- **Never pass `MatchState` by value except as a deliberate clone** — ADR-0003
- **Never mutate state outside `Resolve`** — ADR-0004
- **Never clamp or correct an illegal command — reject it.** A command absent from
  `LegalCommands` is a programming error and must surface as one — ADR-0004
- **Never let presentation construct a `MatchState`** — ADR-0004
- **Never let a gameplay system advance a phase** — ADR-0006
- **Never conflate exhaustion with passing.** A pass grants a Last Word; running out
  of legal actions does not — ADR-0006
- **Never make gameplay data a Godot `Resource`.** It would breach ADR-0001, break
  headless tests and the balance harness, and land on the 4.5
  `duplicate()`/`duplicate_deep()` nested-resource hazard — ADR-0007
- **Never mutate `ContentTables` after load.** Molding changes *state*, never
  definitions; a mutation here leaks between matches and destroys determinism —
  ADR-0007
- **Never hardcode a gameplay value** — `coding-standards.md`, ADR-0007
- **Never give the simulation a concept of wall-clock time** — ADR-0004, ADR-0006
- **Never introduce randomness of any kind into the simulation** — Pillar 1, ADR-0002

### Performance Guardrails

- **AI decision**: ≤ 1.5 s, hard ceiling 3 s — `technical-preferences.md`
- **AI hot path**: zero heap allocations, measured with
  `GC.GetAllocatedBytesForCurrentThread()` — ADR-0003
- **`MatchState` size**: under 1 KB (currently ~400 bytes) — ADR-0003
- **Simulation test suite**: under 5 s, no engine boot — ADR-0001
- **Content load**: milliseconds for a few hundred records — ADR-0007

---

## Core Layer Rules

*Applies to: initiative ladder, damage, status effects, death and respawn, molding,
movement and targeting — all inside `Augury.Sim`, so every Foundation rule also
applies here*

### Required Patterns

- **The legal action predicate (F1) is evaluated at every decision point**: initiative
  ≤ ceiling, champion Ready, cooldown 0, non-empty target set, correct team —
  ADR-0004
- **A Dying champion is eligible to act.** It is not dead until the next death check —
  ADR-0004, ADR-0006
- **Rotation uses `Hex.Rotate`, integer-exact, six-fold.** `Rotate(offset, 6)` returns
  the identity — ADR-0005
- **Patterns are content, not code.** Adding a champion never means editing targeting
  logic — ADR-0005, ADR-0007
- **Board bounds are a radius test against the origin**, not a rectangle — ADR-0005
- **Off-board pattern hexes are dropped; the ability stays legal if any legal target
  remains** — ADR-0005
- **An empty target set means the ability is not legal.** For tier 4 this is the
  intended and common case, not an error — ADR-0005
- **Patterns affect allies exactly as they affect enemies** unless the ability
  explicitly declares otherwise — ADR-0005

### Forbidden Approaches

- **NEVER truncate a generated action or target set along an ordered axis.** If a set
  must be capped, cap it by a symmetric criterion or randomly — never by generation
  order. *This is not a style note.* Capping move targets to the first six of a
  direction-ordered list made one team unable to move toward the map objectives and
  read as a 70% first-mover advantage for three rounds of prototype investigation —
  ADR-0005, `prototypes/initiative-ladder/REPORT.md` Round 3
- **Never resolve a win condition, or any tie, by player index.** Simultaneous
  outcomes must be represented as simultaneous — `prototypes/initiative-ladder/REPORT.md`
  Round 3
- **Never use trigonometry anywhere in the simulation** — ADR-0005
- **Never allow two effects to resolve simultaneously.** Every ability resolves fully
  before the next action is chosen — ADR-0004

### Performance Guardrails

- **Ladder length**: ≤ 16 resolutions per round. This is the *match-length* dial as
  well as a balance dial — at 16.6 resolutions and a 6 s blitz clock, a 15-minute
  match holds ~16 rounds — `design/gdd/initiative-ladder.md` F5

---

## Feature Layer Rules

*Applies to: draft, opening phase, objectives and scoring, economy and items, AI
opponent. **See the Known Gap above — ADR-0008 is unwritten.***

### Required Patterns

- **The AI uses `LegalCommands`, never `Availability`.** `Availability` exists for the
  UI — ADR-0004
- **The AI and the player use identical entry points.** Anything the AI can do, the
  player can, and vice versa — ADR-0004
- **The AI runs entirely inside `Augury.Sim`, on clones.** It never touches
  presentation and never sees an engine type — ADR-0001, ADR-0003

### Forbidden Approaches

- **Never let the AI read presentation state or wall-clock time** — ADR-0001, ADR-0004
- **Never treat search depth as a pure difficulty dial.** It changes play *style*: a
  depth-2 agent passes in 68% of positions versus 13% at depth 3, so a shallow AI is
  strange, not merely weaker — `prototypes/initiative-ladder/REPORT.md`

### Performance Guardrails

- **AI decision**: ≤ 1.5 s, hard ceiling 3 s. Prototype measured ~1,900 nodes per
  decision at depth 3, so headroom is expected — `technical-preferences.md`, ADR-0008 *(pending)*

---

## Presentation Layer Rules

*Applies to: `Augury.Game` — board and unit presentation, combat HUD, ladder UI,
resolution playback, draft and shop screens, audio, input*

### Required Patterns

- **Godot-derived classes must be declared `partial`** — `technical-preferences.md`
- **Presentation submits `Command` values and consumes `GameEvent` values** — ADR-0001
- **Snapshots are values, not views.** Presentation receives `MatchState` by value
  from `ResolveResult` and treats it as read-only — ADR-0004
- **A blitz-clock timeout is converted into a `Pass` command** at the presentation
  boundary — ADR-0004
- **Render the unavailability reasons distinctly.** Cooldown, above-ceiling, and
  no-legal-target are three different decisions and must not look alike — ADR-0004,
  `initiative-ladder.md` UI Requirements
- **All ladder state is inspectable without a menu**: ceiling, legal set, Spent
  champions, Last Word availability — `technical-preferences.md`,
  `initiative-ladder.md`
- **Ability hotkeys map to Q / W / E / R**, borrowing MOBA muscle memory —
  `technical-preferences.md`
- **Tier-4 fixed patterns are previewed on the board in absolute orientation before
  commitment** — `initiative-ladder.md`
- **Use typed signal connections** — `signal.connect(callable)`, never string-based —
  `deprecated-apis.md`
- **Cache node references with `@onready` / constructor lookup.** Never resolve a node
  path in per-frame code — `deprecated-apis.md`

### Forbidden Approaches

- **Never hold a mutable reference into simulation state** — ADR-0004
- **Never recompute a value that arrives in an event payload.** Recomputation in
  presentation is a determinism hazard by another name — ADR-0004
- **Never let physics affect game state.** Jolt may move debris; nothing the
  simulation reads may come back from it — `technical-preferences.md`, Principle 5
- **Never hide required state behind a menu, hover, or toggle.** Hover may *enrich*,
  never *reveal* — `technical-preferences.md`
- **Never use string-based `connect()`** — `deprecated-apis.md`
- **Never use `$NodePath` inside `_process()`** — `deprecated-apis.md`

### Performance Guardrails

- **Target framerate**: 60 fps — `technical-preferences.md`
- **Frame budget**: 16.6 ms — `technical-preferences.md`
- **Draw calls**: < 1000 — `technical-preferences.md`
- **Memory ceiling**: 2 GB — `technical-preferences.md`

### Engine Constraints (Godot 4.6)

- **⚠️ Dual-focus system (4.6)**: mouse/touch focus is now separate from
  keyboard/gamepad focus, and visual feedback differs by input method. **This is the
  only material engine risk in the project** and it lands directly on a ladder UI
  built around QWER hotkeys plus hover inspection. **Verify before building the ladder
  interface** — `current-best-practices.md`, `architecture.md`
- **D3D12 is the default backend on Windows (4.6)**, previously Vulkan. Performance-test
  on Windows specifically — `current-best-practices.md`
- **Glow processes before tonemapping (4.6)**, with screen blending. Existing glow
  intuitions do not transfer — `current-best-practices.md`

---

## Global Rules (All Layers)

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase; Godot-derived also `partial` | `ChampionState`, `partial class ChampionView` |
| Public properties / fields | PascalCase | `MoveSpeed` |
| Private fields | `_camelCase` | `_currentHealth` |
| Signals / events | PascalCase + `EventHandler` suffix | `HealthChangedEventHandler` |
| Files | PascalCase matching the class | `ChampionState.cs` |
| Scenes | PascalCase matching the root node | `ChampionView.tscn` |
| Constants | PascalCase | `MaxHealth`, `DefaultInitiative` |

Source: `.claude/docs/technical-preferences.md`

### Performance Budgets

| Target | Value |
|---|---|
| Framerate | 60 fps |
| Frame budget | 16.6 ms |
| Draw calls | < 1000 |
| Memory ceiling | 2 GB |
| **AI decision budget** | **1.5 s per turn (hard ceiling 3 s)** |

> The AI budget is the number that actually matters. Ten low-poly units on a hex grid
> will never trouble the frame budget; the AI's search over the ladder is the only
> thing that can stall the game — `technical-preferences.md`

### Approved Libraries / Addons

**None.** Nothing is approved yet. Do not add a dependency speculatively — add it to
`technical-preferences.md` when integration actually begins —
`technical-preferences.md`

### Forbidden APIs (Godot 4.6)

Deprecated — the replacement is mandatory, not preferred. Source:
`docs/engine-reference/godot/deprecated-apis.md`

| Never use | Use instead | Since |
|---|---|---|
| `TileMap` | `TileMapLayer` | 4.3 |
| `VisibilityNotifier2D` / `3D` | `VisibleOnScreenNotifier2D` / `3D` | 4.0 |
| `YSort` | `Node2D.y_sort_enabled` | 4.0 |
| `Navigation2D` / `Navigation3D` | `NavigationServer2D` / `3D` | 4.0 |
| `EditorSceneFormatImporterFBX` | `EditorSceneFormatImporterFBX2GLTF` | 4.3 |
| `yield()` | `await signal` | 4.0 |
| `connect("signal", obj, "method")` | `signal.connect(callable)` | 4.0 |
| `instance()` / `PackedScene.instance()` | `instantiate()` | 4.0 |
| `get_world()` | `get_world_3d()` | 4.0 |
| `OS.get_ticks_msec()` | `Time.get_ticks_msec()` | 4.0 |
| `duplicate()` on nested resources | `duplicate_deep()` | 4.5 |
| `Skeleton3D` signal `bone_pose_updated` | `skeleton_updated` | 4.3 |
| `AnimationPlayer.method_call_mode` | `AnimationMixer.callback_mode_method` | 4.3 |
| `AnimationPlayer.playback_active` | `AnimationMixer.active` | 4.3 |
| `Texture2D` in shader parameters | `Texture` | 4.4 |
| GodotPhysics3D for new projects | Jolt (default in 4.6) — **cosmetic use only** | 4.6 |
| Untyped `Array` / `Dictionary` | `Array[Type]`, typed variables | — |
| Manual post-process viewport chains | `Compositor` + `CompositorEffect` | 4.3 |

### Cross-Cutting Constraints

- **Determinism is a blocking gate, not an aspiration.** Identical inputs must produce
  byte-identical serialised output on any platform. This is acceptance criterion 21 of
  `initiative-ladder.md` — ADR-0002
- **Every gameplay value is data, never code** — `coding-standards.md`
- **All public APIs carry doc comments** — `coding-standards.md`
- **Dependency injection over singletons**; all public methods must be unit-testable —
  `coding-standards.md`
- **Commits reference the relevant design document or task ID** — `coding-standards.md`
- **Write tests first for gameplay systems.** Verify UI changes with screenshots —
  `coding-standards.md`
- **Never disable or skip a failing test to make CI pass** — `coding-standards.md`
- **Run every balance question as a mirror match first.** Any deviation from 50% in a
  mirror match is a harness bug until proven otherwise. It costs one run and would have
  caught both prototype bugs immediately —
  `prototypes/initiative-ladder/REPORT.md` Round 3
- **No production code may reference or import from `prototypes/`**, and prototype code
  is never refactored into production — `.claude/rules/prototype-code.md`
