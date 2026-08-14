# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Godot 4.6
- **Language**: C# (.NET 8+, primary), C++ via GDExtension (native plugins only)
- **Rendering**: Forward+ (desktop default)
- **Physics**: Jolt (Godot 4.6 default) — **but see note below**

> **Physics is presentation-only.** Gameplay resolution is a deterministic
> simulation with no rigid-body dynamics: units occupy discrete hexes, and
> outcomes are computed, never simulated. A physics engine is a determinism
> hazard and must never be load-bearing for game state. Jolt may be used for
> cosmetic effects only (debris, ragdolls, cloth), never for anything the
> simulation reads back.

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Steam / Epic)
- **Input Methods**: Keyboard/Mouse
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: Partial (menu navigation; not required for tactical play)
- **Touch Support**: None
- **Platform Notes**: Ability hotkeys map to **Q / W / E / R** to borrow MOBA
  muscle memory directly — this is a deliberate onboarding shortcut, not an
  arbitrary binding. Hover-based inspection is acceptable and expected, since
  mouse is the primary input and Pillar 1 requires all state be inspectable.
  The UI must expose full board state — every ability, cooldown, initiative
  value, stat, and molding delta — without hiding anything behind a menu.

## Naming Conventions

- **Classes**: PascalCase (`ChampionState`) — Godot-derived classes must also be `partial`
- **Variables**: public properties/fields PascalCase (`MoveSpeed`); private fields `_camelCase` (`_currentHealth`)
- **Signals/Events**: PascalCase + `EventHandler` suffix (`HealthChangedEventHandler`)
- **Files**: PascalCase matching class (`ChampionState.cs`)
- **Scenes/Prefabs**: PascalCase matching root node (`ChampionView.tscn`)
- **Constants**: PascalCase (`MaxHealth`, `DefaultInitiative`)

## Performance Budgets

- **Target Framerate**: 60 fps
- **Frame Budget**: 16.6 ms
- **Draw Calls**: < 1000
- **Memory Ceiling**: 2 GB
- **AI Decision Budget**: **1.5 s maximum per AI turn** (hard ceiling 3 s)

> The AI decision budget is the performance number that actually matters for
> this game. Rendering ten low-poly units on a hex grid will never approach the
> frame budget; the AI's search over the initiative ladder is the only thing
> that can stall the game, and a slow AI turn is felt far more acutely than a
> dropped frame in a turn-based title.

## Testing

- **Framework**: **xUnit** for simulation logic (runs headless via `dotnet test`,
  no Godot boot) + **gdUnit4** for engine-integration tests
- **Minimum Coverage**: Simulation layer 90% line coverage — it is pure logic
  with no engine dependency, so there is no excuse for less. Presentation and UI
  have no coverage minimum; they are covered by the manual walkthrough evidence
  described in the Testing Standards table in `coding-standards.md`.
- **Required Tests**: Ladder resolution order, initiative legality rules, death
  check and status phase sequencing, molding stat application, balance formulas,
  determinism (identical inputs must produce byte-identical outputs)

> The split is deliberate and architectural. If simulation tests need the Godot
> runtime to execute, the simulation is entangled with presentation — which
> would also block asynchronous PvP later. A failing `dotnet test` that cannot
> run without the engine is a design smell, not a tooling problem.

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: godot-specialist
- **Language/Code Specialist**: godot-csharp-specialist (all .cs files)
- **Shader Specialist**: godot-shader-specialist (.gdshader files, VisualShader resources)
- **UI Specialist**: godot-specialist (no dedicated UI specialist — primary covers all UI)
- **Additional Specialists**: godot-gdextension-specialist (GDExtension / native C++ bindings only)
- **Routing Notes**: Invoke primary for architecture decisions, ADR validation, and cross-cutting code review. Invoke C# specialist for code quality, [Signal] delegate patterns, [Export] attributes, .csproj management, and C#-specific Godot idioms. Invoke shader specialist for material design and shader code. Invoke GDExtension specialist only when native C++ plugins are involved.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | godot-csharp-specialist |
| Shader / material files (.gdshader, VisualShader) | godot-shader-specialist |
| UI / screen files (Control nodes, CanvasLayer) | godot-specialist |
| Scene / prefab / level files (.tscn, .tres) | godot-specialist |
| Project config (.csproj, NuGet) | godot-csharp-specialist |
| Native extension / plugin files (.gdextension, C++) | godot-gdextension-specialist |
| General architecture review | godot-specialist |
