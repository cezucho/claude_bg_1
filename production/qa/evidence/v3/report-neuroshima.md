# Programmed rounds vs. Neuroshima Hex — what AUGURY should borrow and avoid

Consultant angle: Neuroshima Hex (Portal Games, Michał Oracz). Rules cited are the 3.0 core rules; where I
could not re-verify a detail against the rulebook (the PDFs are blocked from this environment) I say so.

## 0. The Neuroshima resolution, stated exactly

- The board is 19 hexes. Each HQ has 20 HP, initiative 0, and a 1-damage melee hit on every adjacent hex.
- Nothing happens when you place a tile. Combat only happens when a **Battle** tile is played, when the
  19th hex fills, or as a final battle when a player draws their last tile.
- A battle is a pure function of the board: phases run **from the highest initiative (3) down to 0**. In
  phase N every un-netted unit with initiative N attacks **simultaneously**. Damage is applied, and units
  reduced to 0 are turned face-down but **stay on the board until the phase ends** — they still deliver
  their own attack in that phase, and their nets and module bonuses still apply — then they are removed.
- Consequence: a 3 kills a 2 before the 2 acts; two 3s facing each other both die; a unit netted by a
  netter that dies in phase 3 is free from phase 2 onward. "I kill you before you act" exists with zero
  randomness and zero turn order — only initiative and geometry.
- Armies differ by initiative distribution and tricks (from memory: Moloch slow/armoured/tough with
  modules, Borgo fast melee with an HQ that grants +1 initiative, Outpost mobile and ranged, Hegemony nets
  and boss modules). Armour drops incoming damage by 1 from the armoured side; Mobility lets a unit move
  or rotate one step each turn for free; a netted unit cannot attack and its modules do not work.

## 1. Alternating ceiling vs. simultaneous tiers

The owner's ladder (starting team's highest goes first, ceiling drops, the other team alternates, anything
above the ceiling is discarded) has a structural flaw that Neuroshima avoids: **the starting team can
undercut**. If I start and program only initiative-2 abilities, every 3 and 4 you programmed is deleted
before anything happens. That makes initiative *inverted* for the starter (low is safe) and makes
initiative 4 nearly unplayable for the non-starter on alternate rounds. It also keeps the beat count at
up to ten serial steps per round — the same "trading damage" cadence players already dislike.

**Borrow true simultaneous-by-tier resolution.** Proposed rule set:

1. **Tiers 4 → 3 → 2 → 1.** In each tier every programmed ability from both teams resolves against the
   board as it stood at the *start* of the tier. There is no starting team and no ceiling.
2. **Dead units finish their tier (Neuroshima's rule).** A champion killed in tier 3 still fires its tier-3
   program; deaths are removed at tier end. A champion killed in tier 4 never fires its tier-2 program.
3. **"Discarded" becomes two things, both legible:** *Lost* — the caster died in a higher tier (cooldown
   not spent, the ability flashes grey); *Fizzled* — the pattern or range no longer contains a legal
   target (cooldown spent, the pattern draws on empty hexes so the player sees exactly what they missed).
4. **Priority token** for the one thing simultaneity cannot settle: spatial conflicts (two pushes into one
   hex, two walls on one hex, Flash into the same hex). The token holder's effect wins; the token passes to
   the other team at round end. This is the only place the owner's alternation survives, and it carries
   no damage advantage.

Readability: four camera beats per round instead of ten, and each beat is "everything at initiative N".
Depth: initiative becomes a real resource — the 4-tier big pattern goes first but is blind-aimed; the
1-tier precise hit goes last and might never happen. Momentum: a tier-4 double pattern that drops two
champions before they act is exactly the 60%-HP burst the current game lacks (18% of kills). Also,
secret simultaneous programs remove first-mover advantage entirely, which the alternating version
creates every other round.

On the owner's "use all abilities if initiative permits" wish: Neuroshima's answer is that a tile does one
thing, and *modules* make it do more. Keep one program per champion; the existing sigil chain is the
module equivalent — two chained abilities resolve as a single step in the tier of the lower one.

## 2. Should resolution have a trigger?

Neuroshima's tension comes from *accumulated public state* (tiles stay) plus *private potential* (your
hand). Battles are rare and explosive because placement is cheap and resolution is total. AUGURY's
programs are ephemeral intentions, so resolving every round is natural — but it produces an even tempo,
the opposite of "everything shifts in a moment".

Two honest options:

- **Default (safe): resolve every round.** Pace is predictable, the AI search is one layer of hidden
  programs deep, and the ~12-round budget holds.
- **Variant B — armed programs ("call the battle").** Rule: *a programmed ability stays armed on its
  champion (visible as "armed", content hidden) until a Battle fires; an armed champion may not move;
  either team may call Battle at the start of its programming step; Battle also fires automatically
  every third round; siege ticks every round regardless.* Fixes: it reproduces Neuroshima's long,
  tense maneuvering where you can see *who* is loaded but not *what*, and it gives the weaker team the
  classic ambush moment of choosing *when*. Risks: stalling (mitigated by siege ticks and the forced
  third-round battle), hidden state persisting across rounds (a bigger bend of "nothing hidden" than
  spells), and an AI that must model programs accumulated over two rounds. I would prototype the
  default first and add Variant B as a toggle once the per-round version is measured.

## 3. Board size for the prototype

Neuroshima's 19 hexes hold ~13–17 tiles at battle time; everything is in reach of something. AUGURY
needs 10 champions, towers and two nexuses, so 19 is too tight (13 of 19 hexes occupied on turn 1).

**Proposal: "Field 5", a radius-3 hexagon of 37 hexes.** Nexuses on two opposite corners (distance 6
apart). Centre tower on the centre hex. One home tower per side at distance 2 from its nexus, on the
long axis, so it blocks the straight lane and forces flanks. Each team starts on its three back-corner
rows (the 7–9 hexes within distance 2 of its nexus). With SPD 2–3, contact comes after one move; a
range-2 pattern covers roughly a third of the board, so most programs land by round 2.

What is lost: three true lanes (it becomes centre plus two flanks); jungle, beacon and hill variety;
"caught alone" loses nuance when everyone is adjacent to someone; the 3-instruction openings must be
re-scoped to shorter moves; quests like "in the enemy half with no ally beside it" become trivial or
suicidal and need re-authoring per map. Siege and kill-tax numbers also need re-tuning (section 6).

## 4. Asymmetry and initiative as the identity stat

Neuroshima lessons that map directly:

- **An army is its initiative histogram.** Give each of the 15 champions a visibly different tier
  profile — an "alpha" (4,3,2), a "sniper" (2,1,1), a "controller" (3,3,1) — and show the histogram on
  the draft card. Under simultaneous tiers that histogram *is* the champion's tempo.
- **Make give/want temporal and explicit.** Rule: *a status applied in tier N is live for all lower
  tiers of the same round and for the next round.* A giver at tier 3 feeding a wanter at tier 1 is a
  same-round combo; a giver at tier 1 and a wanter at tier 4 only works across rounds. This turns the
  draft's pair-finding into reading initiative chains, which is more interesting than matching icons.
- **Initiative modules.** Neuroshima's +1 initiative module is one of its strongest tiles. Let quest
  upgrades at 2 stacks raise one ability's initiative by 1 (not just a stat); it will matter far more
  than +8% POW under tier resolution, and it makes quest fulfilment a visible threat shift.
- **Each trick has a counter and the counter is legible**: nets beat damage, armour beats ranged, mobility
  beats nets. Audit AUGURY's six statuses so each has one clean counter (Cleanse, walls, Flash) and no
  status is counter-less.

## 5. Ordering control against damage

Neuroshima's rule: control is *continuous*, damage is *phased*. A net is applied from placement, so by the
time any phase fires, the netted unit is already silent; the only way out is to kill the netter in a
higher phase. Control is a counter to damage without being damage.

Proposed AUGURY tier sub-order (fixed, printed on screen):

1. **Displacement** — pushes, walls, Flash, Teleport; conflicts settled by priority token.
2. **Control** — root, exhaust. *Exhaust is the net: the target's unresolved program this round is
   cancelled (Lost).* Root fixes the target's hex for later displacement and movement.
3. **Damage and marking statuses** — mark, burn, poison, with the give/want multiplier read at this moment.
4. **Deaths removed.**

Because control resolves before damage *within the same tier*, a tier-3 exhaust beats a tier-3 nuke —
"control wins ties", which is Neuroshima's balance exactly. Guardrail: most control should sit at tiers
1–2 (precise, late); a tier-3 or tier-4 control ability is a deliberate "Net Fighter" premium and should
be rare and costly in cooldown.

Targets vs. movement: Neuroshima units fire in a direction, never at a unit. Rule: *tier 3–4 programs
commit a pattern and facing; tier 2 commits a hex; tier 1 commits a champion and tracks it anywhere in
range.* Then "the target moved" is a feature (dodging a 4 by stepping out) rather than a bug, and tier 1
earns its lateness by being the only homing tier.

## 6. HQ damage as score

Neuroshima scores only HQ damage; unit deaths cost nothing except that the tile is gone forever from a
finite deck. The pacing effect is that every battle is read through "does damage reach the HQ", the end
state is usually a brawl in the HQ's corner, and the game is decided over 3–5 battles. AUGURY's
respawning champions make permanence impossible, so the kill tax is the right substitute for it.

For the 37-hex prototype: **nexus 40 HP, kill tax 4, siege only from the centre tower (1/round), and a
champion adjacent to the enemy nexus deals its basic hit to it in the status phase** (Neuroshima's
HQ-adjacency rule). Dropping home-tower siege keeps the small map from bleeding both nexuses passively;
adjacency damage gives the "we are at the gates" ending Neuroshima always delivers. Measure whether
matches end by kills or by gates — a healthy split is roughly 60/40.

## 7. What Neuroshima players complain about, and the fix

- **Analysis paralysis before a Battle tile.** Computing a 15-tile battle by hand is the game's known
  pain. AUGURY has a computer: at programming time, show a **threat overlay** (every enemy kit is public —
  for each hex, which enemy abilities could cover it this round and at which tier) and a **best-case
  preview** of your own programs against the current board. Decision count per round stays at five
  programs plus five moves; cap the UI at those ten controls.
- **Reveal readability in the digital version.** The app resolves the whole battle as an animation and
  players lose the "why" (which module, which net). Fix: step the reveal **tier by tier with a
  pause**, grey out Lost programs with the killer named, draw Fizzled patterns on the empty hexes, and keep
  a per-tier log row. Never resolve all four tiers in one animation.
- **First-player advantage.** Neuroshima's mitigation is that the first player draws only one tile on
  turn one. Simultaneous secret programs remove the problem at the source; the residual priority token
  alternates, and the team that picked *second* in the draft holds it in round 1.
- **Turtling around the HQ / stalemate.** Only a risk under Variant B; the siege tick and the forced
  third-round battle are the brakes. Watch average round-of-first-death.
- **Flat solo play.** Neuroshima's single-player is thin because the AI only reacts. AUGURY's AI will
  need an opponent model over programs; the cheapest Neuroshima-flavoured one is "assume the enemy
  programs its highest-tier ability that can hit something", then search your reply.

## 8. One-line summary for the recommended rule set

Programs are secret, one per champion, committed as pattern-and-facing (tiers 3–4), hex (tier 2) or
tracked champion (tier 1); reveal tier by tier 4 → 1 with simultaneous resolution inside a tier,
displacement → control → damage → deaths; the dead finish their tier; Lost and Fizzled are distinct
and shown; a priority token alternates for spatial conflicts; 37-hex board, nexus 40, kill tax 4,
centre-tower siege plus nexus-adjacency hits; resolve every round first, test "armed programs" second.

Sources confirming the core battle rules: [Neuroshima Hex 3.0 rulebook (nastol.io mirror)](https://nastol.io/storage/games/files/neuroshima_hex_30/889063/Neuroshima_Hex_3.pdf),
[Neuroshima Hex Duel rulebook](https://world-of-board-games.com.sg/docs/Neuroshima-Hex-Duel.pdf),
[Neuroshima Hex v1 rules summary](https://www.orderofgamers.com/downloads/NeuroshimaHex_v1.pdf),
[boardgaming.com overview](https://boardgaming.com/games/neuroshima-hex),
[Wikipedia: Neuroshima Hex!](https://en.wikipedia.org/wiki/Neuroshima_Hex!),
[PocketGamer review of the digital version](https://www.pocketgamer.com/articles/024297/neuroshima-hex/).
