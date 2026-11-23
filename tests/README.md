# tests/

The main game (`game/`) has no automated test suite yet. Its checks are built into the page:
`simulate(n)` plays n AI matches headless and reports balance numbers, and `patternAudit()` measures formations
(see `game/README.md`). Browser checks are run with Playwright against `game/index.html`.

The xUnit suite for the old C# simulation is archived with it in `archive/v2-simulation/tests/`.
A Godot port will bring its tests here (xUnit for simulation logic, gdUnit4 for engine integration).
