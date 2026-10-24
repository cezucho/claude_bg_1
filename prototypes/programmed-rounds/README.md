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
random).** An **opening** before any secret order, and a **board switch** — the 37-hex board or
the main game's Field 7 — plus a **threat count** on every hex (on by default): how many enemy
champions could reach that hex next round (their move plus their longest hostile range); 3+
turns the hex dark red.

**Changed 2026-10-24 (owner: the opening should be the main game's, not a plain move).** The
opening is now the **scripted opening of v2**: each ability owns a script of three instructions
in the team's frame ("mid ↗" moves whoever plays mid one hex forward-right; "cast" fires the
ability that owns the script, aimed by itself). Teams alternate; a play is one champion that
hasn't played and one of its three openings; every champion plays exactly one; A first. Moves
are strict (a blocked move voids the script); you must play a script if any fits, otherwise
each remaining champion steps one hex. Exactly one script per champion casts; opening casts
can't kill and may fizzle. Hovering a script previews its moves on the board. Field 7 now uses
the **main game's geometry** (start on the back row, nexus as a line of three hexes on the
start line, fronts six rows apart) and is the default board. The AI picks the script that most
improves its position (forward, few enemies in reach, hills, an ally beside, near the centre)
plus what its cast does; it tries each candidate on the real state and undoes it.

**Not in it.** Quests, spells, cooldowns, walls and pushes, swamps, the alternating ceiling
toggle, armed programs. Hotseat (secret programs on one screen need a pass-device step). The
scripts are written for Field 7's back row; on the small board (start row −2) eleven of the
thirty are blocked from the first play, so the small board now plays mostly fallback steps — it
needs its own scripts if it is to be compared seriously.

**How to run.** Open `index.html`, or the published artifact link in the session. "New: play A
vs AI" or "New: watch AI vs AI"; "Board" switches the map (starts a new match). Rules are
under the Rules button.

**Status.** In progress — first playable 2026-10-21, opening and board switch 2026-10-22,
scripted opening and real Field 7 geometry 2026-10-24. Numbers from the AI-vs-AI strip are
indicative only (the AI is greedy and one ply).

**Findings so far.**
- On the 37-hex board, speed 2 plus range 3 means almost every hex is within reach of almost
  every enemy champion from the first round. The threat overlay shows 4–5 on nearly every hex.
  There is no "approach" on that board, which is why advancing felt like walking into random
  fire: nothing was out of range to begin with.
- With the fronts only four rows apart (the first Field 7 layout), scripted openings collide
  at once: the mirror keeps the river row fixed, so a champion standing on it blocks its
  counterpart's script to the same hex, and a team's own earlier plays block the rest. Half the
  champions fell to the fallback step. Six rows apart (the real Field 7), a full opening plays
  out with at most one fallback and the lines end one to two hexes apart, hills and centre
  contested — the "meet in the middle" the owner described.
- The first player's casting scripts fire into empty ground (nobody is in range yet); the
  second player's land. v2 has the same asymmetry and pays it back with the round-1 token.
