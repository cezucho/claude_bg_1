# Smoke Test: Critical Paths

> **Purpose**: run these checks in under 15 minutes before any QA hand-off.
> **Run via**: `/smoke-check`, which reads this file.
> **Update**: add an entry whenever a new core system lands.

A failed smoke check means the build is not ready for QA. It is a gate, not a report.

## Automated (must pass before any manual check begins)

1. `dotnet test Augury.sln` — full simulation suite green
2. Control manifest guard — no Godot reference and no floating point in `Augury.Sim`
3. **Determinism**: identical inputs produce byte-identical output *(add when `MatchState` exists — blocking, `initiative-ladder.md` AC-21)*

## Core Stability

4. Game launches to main menu without crash *(once `Augury.Game` exists)*
5. A match can be started from the main menu
6. Main menu responds to keyboard and mouse without freezing

## Core Mechanic — the initiative ladder

*Add each as it is implemented. These are the checks that would catch a broken ladder
in under a minute.*

7. A round runs two halves, each opened by a different team
8. An ability at initiative N is answerable only at initiative ≤ N
9. A pass grants the opponent exactly one unanswerable Last Word, then the half ends
10. Running out of legal actions ends the half and grants **no** Last Word
11. A champion reduced to 0 HP mid-ladder still acts for the rest of the round
12. Round close runs death check **before** status phase — a poisoned champion at 0 HP
    gets its dying round
13. A champion that acted in half 1 is Ready again in half 2

## Data Integrity

14. Content loads from `assets/data/` without validation errors
15. A malformed content file fails loudly rather than producing a subtly wrong game

## Performance

16. AI returns a decision within the 1.5 s budget on target hardware
17. AI decision allocates zero bytes on the hot path
18. No visible frame rate drops at the 60 fps target
