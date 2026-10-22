# Prototype: programmed rounds

**Hypothesis.** A round where both teams secretly program orders (a move and up to two
abilities per champion, under an energy budget), then reveal and resolve them simultaneously by
initiative tier (4 → 1), is quicker than v2's alternating ladder and produces the momentum
swings the owner wants (bursts, picks that pay), while keeping the game deterministic and
readable. Design source: `design/v3-programmed-rounds.md` (the specialists' synthesis).

**What it is.** One self-contained HTML file, no server, no libraries: `index.html`. Open it in
a browser (or the published artifact). It has a 10-champion draft (two per role, give/want pairs),
a 37-hex board with a centre tower, one home tower each, two hills and the nexuses at the corners,
8 rounds, energy `min(10, 5 + ⌈r/2⌉)`, commitment by tier (tier 4 fixed forward, tier 3 a facing,
tier 2 a hex, tier 1 follows a champion), Focus, lost/fizzled orders, exhaust as the net, "caught
alone", a greedy AI that commits before reading the player, a "watch AI vs AI" mode, and a strip
of the harness-style counters (burst-kill share, lost and fizzled orders, lead changes, decisions).

**Added 2026-10-22 (owner's feedback: programming while the armies are far apart feels
random).** An **opening** before any secret order: teams alternate visible plays, each play
moves one champion up to N hexes (2 on the small board, 3 on Field 7), every champion moves
once, A first; the secret rounds begin only when both armies have taken their ground. A **board
switch** — the 37-hex board or the main game's Field 7 (59 hexes, towers and hills mirrored
across the river) — so the owner can judge whether the larger map still works once there is an
opening. A **threat count** on every hex (on by default): how many enemy champions could reach
that hex next round (their move plus their longest hostile range); 3+ turns the hex dark red.

**Not in it.** Quests, spells, cooldowns, walls and pushes, the alternating ceiling toggle, armed
programs, the main game's scripted opening instructions (the prototype's opening is a plain
move). Hotseat (secret programs on one screen need a pass-device step).

**How to run.** Open `index.html`, or the published artifact link in the session. "New: play A
vs AI" or "New: watch AI vs AI"; "Board" switches the map (starts a new match). Rules are
under the Rules button.

**Status.** In progress — first playable 2026-10-21, opening and board switch 2026-10-22.
Numbers from the AI-vs-AI strip are indicative only (the AI is greedy and one ply).

**Findings so far.**
- On the 37-hex board, speed 2 plus range 3 means almost every hex is within reach of almost
  every enemy champion from the first round. The threat overlay shows 4–5 on nearly every hex.
  There is no "approach" on that board, which is why advancing felt like walking into random
  fire: nothing was out of range to begin with. Either the board is larger, or reach is shorter,
  or the opening does the closing — the opening is now in; the owner judges.
- Field 7 with a 3-step opening leaves the two lines one to two hexes apart at round 1, with
  the hills and the centre tower already contested. That is the "meet in the middle" the owner
  described.
