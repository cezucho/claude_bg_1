# Archive: the v2 simulation (retired 2026-11-23, D-086)

The C# main game from 2026-08 to 2026-11: a deterministic, integer-only simulation (`src/Augury.Sim`), a browser
client laid out like a broadcast (`src/Augury.Web`), a terminal client (`src/Augury.Cli`), measurement tools
(`tools/Augury.Tools`) and 115 xUnit tests (`tests/`). Its design is in `design/` (GDDs, mvp-rules, v2/v3 notes)
and its architecture in `docs/architecture/` (ADRs), all marked superseded.

Why archived: the game moved on to browser prototypes (auto skirmish, then Pattern Skirmish), and Pattern Skirmish
became the main game (`game/`). This code is kept because ADR-0001/0002/0005 (simulation apart from presentation,
integer arithmetic, hex coordinates) are the plan for the Godot port, and this is a working example of them.

Still builds and passes from here: `dotnet test archive/v2-simulation/Augury.sln` (115 tests, 2026-11-23).
It reads content from `assets/data/` at the repository root (it finds it by walking up the folders).
Play it: see `PLAY.md` ("v2 clients"). CI no longer runs it automatically (`.github/workflows/tests.yml`, manual).
