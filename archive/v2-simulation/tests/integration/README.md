# Engine Integration Tests (gdUnit4)

Tests here need the Godot runtime. Everything that *doesn't* belongs in
`tests/unit/Augury.Sim.Tests/` instead — see `tests/README.md` for why the split
exists and why it is self-policing.

**Currently empty.** There is no Godot project yet; the `integration-tests` CI job is
guarded by `if: false` until `Augury.Game/` exists.

## What belongs here

- Presentation consuming the simulation's event stream
- Input handling, especially against Godot 4.6's **dual-focus system** — the only
  material engine risk identified in `docs/architecture/architecture.md`
- Scene loading, node lifecycle, signal wiring
- Resolution playback pacing

## What does NOT belong here

Anything expressible as a rule about the game. Damage numbers, ladder legality,
round ordering, molding, hex geometry — all of that is `Augury.Sim` and tests under
xUnit, without an engine.

## Installing gdUnit4

1. Godot → AssetLib → search "gdUnit4" → Download & Install
2. Project → Project Settings → Plugins → gdUnit4 ✓
3. Restart the editor
4. Verify `addons/gdUnit4/` exists
5. Remove the `if: false` guard from the `integration-tests` job in
   `.github/workflows/tests.yml`
