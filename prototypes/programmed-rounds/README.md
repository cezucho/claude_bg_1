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

**Not in it.** The opening phase, quests, spells, cooldowns, walls and pushes, the alternating
ceiling toggle, armed programs. Hotseat (secret programs on one screen need a pass-device step).

**How to run.** Open `index.html`, or the published artifact link in the session. "New: play A
vs AI" or "New: watch AI vs AI". Rules are under the Rules button.

**Status.** In progress — first playable, 2026-10-21. Numbers from the AI-vs-AI strip are
indicative only (the AI is greedy and one ply).

**Findings.** None yet; the owner plays first.
