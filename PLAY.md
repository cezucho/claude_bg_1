# Playing AUGURY — v2 (three abilities + summoner spells, Field 7)

Two clients over the same simulation: a **browser client** laid out like a match
broadcast (recommended), and the original terminal client. Both play by the real rules —
the same simulation a Godot client will sit on later.

You need the **.NET 8 SDK**.

## Browser client (recommended)

```bash
dotnet run --project src/Augury.Web                 # then open http://localhost:5080
dotnet run --project src/Augury.Web -- --port 8080   # another port
```

Pick a mode on the draft screen (or from the toolbar at any time): **vs AI as A**
(bottom, blue), **vs AI as B** (top, red), **Hotseat**, or **Watch AI**.

- **Left and right panels** are the two teams, ordered top · jungle · mid · bottom ·
  support. Each card has a portrait, role badge, HP and shield, stats, status chips, and
  the four abilities.
- **Ability icons**: the big number is initiative (colour = tier), the corner letter is
  the key, the small tag is the sigil (solid = printed, dashed = slot, white = lit by a
  beacon), a dark overlay is the cooldown. Glowing icons are playable now; dimmed ones
  say why not when you hover them.
- **Hover an ability** to see its reach (faint) and what it could hit (gold) — for either
  team. On your turn it also shows exactly where it can be played. The tooltip draws the
  ability's **shape** in one fixed example — single target or area, which hexes are hit —
  and its **three opening moves** from the starting line.
- **Map tokens show the role** (sword top, claws jungle, diamond mid, crosshair bottom,
  shield support), coloured by team; the champion's name is in the side panel.
- **In the draft, hover a champion** for a card with every ability's combat shape and
  opening moves side by side, to compare champions before you pick. Each card says how the
  champion is meant to be played, and tags its **opening attack**.
- **Summoner spells** come after the draft: give each of your champions one of eight spells
  (no spell twice in a team). The other team's spells stay hidden until the opening. A spell
  sits in slot R with a dashed gold border; slot R's opening is still the champion's own.
- **Statuses** show twice: as chips on the card (hover for the rule) and as lettered pips
  above the token — **R**oot, **B**urn, **M**ark, e**X**haust, **U**nstoppable, **W**ound,
  **P**oison. **Walls** are stone hexes on the map with the round closes they have left.
- **To act**: click a glowing ability, then a gold marker on the map. Tier-3 patterns
  show one arrow per facing. In the basics, click one of your champions, then a green hex
  (move) or a gold ring (basic attack).
- **Hover any marker, chain or Pass to preview the exact outcome** — damage, kills,
  movement, captures. The engine is deterministic, so the preview is what will happen.
- **Magenta always marks the last action**, yours or the AI's, until the next one: the
  champion who acted (token, card and the ability icon it used), the hexes it covered, a
  moving line to what it hit, a dashed ghost where anyone moved from with an arrow to where
  they went, and the numbers that changed. Moved champions slide from their old hex. The
  banner under the map names the action. When a round closes, magenta beams run from every
  held tower to the enemy nexus it fires at.
- **The top bar is the nexus race**: each team's nexus HP, whether it is open, and the
  siege it will take at the next round close.
- **Undo** takes back your last decision. **Pause** stops the AI between moves;
  **AI speed** sets how fast it plays. **Rules** is a one-screen summary. Esc cancels a
  selection.

### Trying another board

The shipped board is `field7` with solid towers. `"board"` in `assets/data/rules_config.json`
also takes `field7base`, `field5` or `classic`. Restart after changing it. The v2 openings are
written for Field 7, so on another board many of them fall back. The candidates and their
measurements are in `design/board-layouts.md`.

## Terminal client

From the repository root:

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

**Draft** one champion per role, then choose a **summoner spell** for each, hidden from the
opponent. In the **opening**, each of your champions plays one of its four openings, whose
three instructions move your team into formation — order matters, and if nothing is available
you fall back to one hex each. One opening per champion **casts** an ability (sometimes a
teammate's): with nothing in reach it fizzles, and either way that ability starts round 1 on
cooldown. Opening damage can't kill. Each **half** of a round starts with
**basics** (two of your champions each move or basic-attack), then the **ladder**: play an
ability at initiative ≤ the ceiling, the ceiling drops to what you played, and so on;
**pass** and your opponent gets one unanswerable **Last Word**. **Chains** let two
abilities sharing a sigil resolve as one step and exceed the ceiling. At **round close**
the dead die, poison and towers bite (going below zero *here* means **Dying**: one more
round at half power), towers score, cooldowns tick.

**Win** by destroying the enemy **nexus** (60 HP). At every round close each tower you hold
fires at it for 1, and every enemy death costs it 3; once it is **open** — after you've
taken one of their two home towers — you can also attack it directly.

Full rules: `design/mvp-rules.md`. Every call made without you: `design/claude-decisions.md`.

## For the harness

```bash
dotnet run -c Release --project tools/Augury.Tools selfplay 500            # AI vs AI, criteria check
dotnet run -c Release --project tools/Augury.Tools selfplay 300 towerHp=12 # try a rule change
dotnet run -c Release --project tools/Augury.Tools trace 7                 # one random match, full log
dotnet test Augury.sln                                                     # 67 rules tests
```
