# Playing AUGURY — first playable

A terminal build. Crude to look at, but it plays by the real rules: the same
simulation a Godot client will sit on later.

## Run it

You need the **.NET 8 SDK**. From the repository root:

```bash
dotnet run --project src/Augury.Cli                  # menu
dotnet run --project src/Augury.Cli -- --vs-ai A     # you are team A (bottom) vs the AI
dotnet run --project src/Augury.Cli -- --vs-ai B     # you are team B (top)
dotnet run --project src/Augury.Cli -- --hotseat     # two players, one keyboard
dotnet run --project src/Augury.Cli -- --watch       # the AI plays itself
```

Add `--no-color` if your terminal shows escape codes. Run it from inside the repository,
because it loads `assets/data/`.

## Controls

- Type a **number** to choose an action.
- When an action needs a target or a destination, the options appear as **letters on the
  board**: type the letter. `b` goes back.
- `i <n>` inspects champion *n*: full kit, opening instructions, passive, molding so far.
- `h` is a one-screen rules summary. `q` quits.

## Reading the board

```
r  2          .   l   TB  ·   s   ·   .      ← team B, lower case, top
r  0      .   .   ·   ·   w   ·   ·   .   .
r -2          S   ·   TA  O   TA  ·   R      ← team A, UPPER CASE, bottom
```

| Mark | Meaning |
|---|---|
| `W` / `w` | a champion (the letter is its glyph); `!` dying, `~` already acted this half |
| `TA` `TB` `T·` | tower owned by A, by B, or neutral |
| `N` | nexus hex |
| `*1` `*2` `*3` | beacon, by sigil |
| `=` `·` `.` `_` | lane, open ground, jungle, spawn hex |

## A match in one paragraph

**Draft** one champion per role. In the **opening**, each of your champions plays one
ability whose three instructions move your team into formation — order matters, and if
nothing is available you fall back to one hex each. Each **half** of a round starts with
**basics** (two of your champions each move or basic-attack), then the **ladder**: play an
ability at initiative ≤ the ceiling, the ceiling drops to what you played, and so on;
**pass** and your opponent gets one unanswerable **Last Word**. **Chains** let two
abilities sharing a sigil resolve as one step and exceed the ceiling. At **round close**
the dead die, poison and towers bite (going below zero *here* means **Dying**: one more
round at half power), towers score, cooldowns tick.

**Win** by reaching **60 points** (3 per kill, 1 per tower per round), or by destroying
the enemy **nexus** — which only opens once you've taken one of their two home towers.

Full rules: `design/mvp-rules.md`. Every call made without you: `design/claude-decisions.md`.

## For the harness

```bash
dotnet run -c Release --project tools/Augury.Tools selfplay 500            # AI vs AI, criteria check
dotnet run -c Release --project tools/Augury.Tools selfplay 300 towerHp=12 # try a rule change
dotnet run -c Release --project tools/Augury.Tools trace 7                 # one random match, full log
dotnet test Augury.sln                                                     # 67 rules tests
```
