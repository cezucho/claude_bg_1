# Godot Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Godot 4.6 |
| **Language** | C# (.NET 8+) |
| **Release Date** | January 2026 |
| **Project Pinned** | 2026-08-14 |
| **Last Docs Verified** | 2026-08-14 |
| **LLM Knowledge Cutoff** | May 2026 |
| **Risk Level** | **MEDIUM** — within training data, but recent enough to verify |

## Knowledge Gap Assessment

Godot 4.6 released in January 2026, roughly four months before the assistant's
May 2026 training cutoff, so 4.4, 4.5, and 4.6 all fall **inside** training data.
This is a correction to an earlier version of this file, which assumed a May 2025
cutoff and warned that training covered only up to ~4.3 — that warning was
written for an older model and no longer applies.

MEDIUM rather than LOW, for two reasons:

1. **Recency dulls recall.** A version released months before a cutoff is
   represented far more thinly in training data than one that has been in the
   wild for years. Specific API signatures, defaults, and 4.6-only behavior are
   exactly the details most likely to be misremembered.
2. **4.6 changed defaults, not just APIs.** Jolt became the default physics
   engine, D3D12 became the Windows default, and glow was reworked. Defaults
   that changed are more dangerous than APIs that were removed, because wrong
   code still compiles and simply behaves differently.

**Practical rule:** cross-reference this directory before suggesting a Godot API,
and use WebSearch for anything uncertain. Note that `godotengine.org` and
`docs.godotengine.org` are blocked by this environment's egress proxy — fetch
documentation from `raw.githubusercontent.com/godotengine/godot-docs` instead.

## Beyond This Version

| Version | Release | Status | Notes |
|---------|---------|--------|-------|
| 4.7 | ~Jun 2026 | **Beyond training data** | Feature release. Not adopted. |
| 4.7.1 | 14 Jul 2026 | **Beyond training data** | Latest stable at time of pinning. Not adopted. |

The project deliberately stays on 4.6. If upgrading later, run
`/setup-engine upgrade 4.6 4.7.1` — the official guide describes 4.6 → 4.7 as
"relatively safe," and the known breaking changes are largely irrelevant to this
project (RichTextLabel parameter renames, `AudioStreamPlayer` default
`area_mask` changing from 1 to 0, Jolt `WorldBoundaryShape3D` plane distance
sign reversal, mouse/keyboard device IDs becoming `InputEvent.DEVICE_ID_*`
constants, macOS minimum rising to Big Sur). Anything past 4.6 is outside
training data and requires reference docs before use.

## Version History for This Project

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 4.4 | ~Mid 2025 | LOW | Jolt physics option, FileAccess return types, shader texture type changes |
| 4.5 | ~Late 2025 | LOW | Accessibility (AccessKit), variadic args, `@abstract`, shader baker, SMAA |
| 4.6 | Jan 2026 | **MEDIUM — pinned** | Jolt default, glow rework, D3D12 default on Windows, IK restored |

## Project-Specific Cautions

- **Determinism.** Pillar 1 forbids randomness and future asynchronous PvP
  requires bit-identical resolution across machines. Godot's floating-point
  behavior is not a determinism guarantee. Keep the simulation off engine math
  entirely — see `.claude/docs/technical-preferences.md`.
- **Physics is cosmetic only.** Jolt is the 4.6 default and must never carry
  game state.
- **C# specifics.** Godot C# classes must be declared `partial`. C# web export
  remains weak in 4.x — irrelevant for this PC-only project, but it does close
  the browser-PvP door.

## Verified Sources

- Official docs: https://docs.godotengine.org/en/stable/ *(blocked by this environment's proxy)*
- Docs source (fetchable): https://raw.githubusercontent.com/godotengine/godot-docs/master/
- 4.6→4.7 migration: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/migrating/upgrading_to_godot_4.7.rst
- 4.5→4.6 migration: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/migrating/upgrading_to_godot_4.6.rst
- Changelog: https://github.com/godotengine/godot/blob/master/CHANGELOG.md
