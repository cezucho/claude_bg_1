# v3 Idea — Programmed Rounds

> **Date**: 2026-10-21 · **Status**: brainstorm, not a decision; nothing built. The v2 game is
> untouched. A prototype, if the owner wants one, is built *beside* v2.
> **Sources**: the owner's idea (below), three specialist reports commissioned for it — a core-loop
> designer, a mobile pace-of-play specialist, and a Neuroshima Hex expert — and Claude's synthesis.
> Reports are summarised in `production/qa/evidence/v3/` (the full texts were returned in the
> session; the summaries keep every rule they proposed).

## The owner's idea (2026-10-21)

Each round both teams **secretly program one ability per champion**, then a resolution ladder
runs. One team **starts** each round (alternating). The starter knows its highest-initiative
programmed ability goes first — but *which one is first matters*: if the starter opens at
initiative 2 and the other team programmed 3s and 4s, those are **discarded** (the ceiling has
dropped below them). Teams then alternate, the initiative going down. A round can be up to five
abilities a side, or three if the ceiling cuts some. The owner's own caveats: five a side "is a
lot"; it contradicts an earlier wish that a champion could use all its abilities if initiative
permits; and the map should be much smaller so that after the opening most programs actually hit.
Inspirations: Marvel Snap (secret commits, big reveals, escalating energy, combos inside a turn,
positioning) and the wish for League-style momentum swings.

## Where all three specialists agree

1. **Secret, simultaneous commit with a full reveal is the right core.** It turns ~20 alternating
   ladder decisions a round into one 40-second think and a 15-second show. It is compatible with
   "nothing hidden" if intent is hidden for one window only, symmetrically, bounded by public
   menus/cooldowns/positions, and revealed in full — a *tighter* exception than today's secret
   spells, which are hidden for the whole draft and opening.
2. **One ability per champion per round is the right unit.** "Five a side" is too many only if
   all five are *casts*; with an escalating budget most rounds hold ~3 casts plus moves. The
   "all abilities if initiative permits" wish is met by **escalation** (later rounds allow more)
   and by a **descending pair** (a champion may program two abilities in strictly descending
   initiative — give at 3, cash in at 1 — the Snap combo-inside-a-turn), not by four casts a
   champion.
3. **Delete, for the prototype:** the two halves; the separate basic phase (moves become part
   of the program); Pass and Last Word (they only protected alternation); chains, sigils and
   beacons; the 4-stack upgrade; swamps. **Keep:** the draft, the opening (re-scoped), quests,
   give/want ×2, "caught alone", one or two hills, the nexus race.
4. **A much smaller board:** a radius-3 hexagon, **37 hexes**, nexuses at opposite corners (6
   apart), the centre tower, one home tower each side; nexus **40 HP**; ~8–10 rounds.
5. **The reveal must be staged one beat at a time**, cuts shown as their own beat (what was
   discarded and by what), and the programming screen must show predicted hits, "cut if they
   start at N", and an enemy **threat overlay** (every enemy kit is public, so the player can
   enumerate what might come).
6. **The owner's pure rule has one exploit, and all three found it: the undercut.** The starter
   programs only low initiative and deletes every enemy 3 and 4 before anything happens. It
   inverts initiative (low is safe for the starter), makes 4s unplayable for the non-starter on
   alternate rounds, and keeps ten serial beats a round. Each specialist fixes it differently
   (next section).
7. **Momentum comes with the package:** deaths resolve inside the round, a tier-4 pattern can
   drop two champions before they act, and a misread round that cuts three programs and loses a
   carry swings ~15 of 40 nexus HP — the Snap turn-6 scale. With the v2 levers (death costs half
   the stacks, respawn per stack, caught alone) that is the ambush-the-carry story.

## Where they disagree, and Claude's call

| Question | Core-loop designer | Pace specialist | Neuroshima expert | Claude's call |
|---|---|---|---|---|
| **Resolution order** | Keep the owner's alternating ceiling, but open with a **Clash**: each team's top order always plays (higher initiative first, tie to the starter); the ceiling becomes the lower of the two; then alternate | Keep the ceiling as *the* mind game — the starter choosing where to open is a legible sacrifice, better than Snap's priority; soften cuts if the non-starter stops programming 3s and 4s | **Replace alternation with simultaneous tiers 4 → 3 → 2 → 1**, both teams at once within a tier, no starter, no ceiling; "the dead finish their tier"; a priority token only for spatial conflicts | **Simultaneous tiers as the default, the Clash-ceiling as a toggle.** Tiers remove the first-mover problem at the source, give four reveal beats instead of ten, and make initiative a real identity stat. The ceiling's "choice" survives as *which tier you commit at*. Measure both. |
| **The budget** | **Energy**: `E(r) = min(10, 5 + ceil(r/2))`; an ability costs its initiative, a move costs 1; the non-starter gets +1; discards refund as Carry (cap +3) | **Slots**, not cost: program 3 champions in rounds 1–2, 4 in 3–4, 5 from round 5; initiative is order, not power, so pricing 4s is backwards | — (one program per champion; combos via chain) | **Energy, because it fits tiers.** Under simultaneous tiers higher initiative is strictly better (it acts first), so "cost = initiative" is coherent; it is backwards only under the ceiling, where high initiative is a risk. Energy also prices the descending pair naturally. |
| **Targeting** | Auto-aim at resolution; one free **Focus** enemy per team as the first tiebreak; tier 4 commits an **anchor hex** | Auto-aim everything; explicit targets break when targets move | **Commit by tier**: tiers 3–4 commit a pattern and facing, tier 2 a hex, tier 1 a tracked champion; "the target moved" is dodging, not a bug | **The Neuroshima gradient plus Focus.** It makes the tiers feel different (early = blind and big, late = homing and small) and keeps everything deterministic. Focus gives the team one shared intention to read. |
| **Summoner spells** | Keep in slot R | **Drop** (a second hidden layer, 5 autopilot decisions; the program now carries the thrill) | — | **Drop for the prototype.** Reversible; it also removes the older hidden-information exception. |
| **Order within a tier** | — | — | Fixed sub-order: **displacement → control → damage → deaths**; exhaust is the net (cancels the target's unresolved program); control wins ties | **Adopt.** It is the deterministic answer to "control vs damage" and is printable on one line. |
| **When to resolve** | Every round | Every round, 8 rounds, hard end | Every round first; later test **armed programs** ("call the battle": programs stay armed until either team calls it or a third round forces it) | **Every round.** Armed programs reproduce Neuroshima's calm-then-explosion but stack hidden state across rounds and complicate the AI; a toggle for later. |

## The streamlined prototype — a rule set to react to

1. **Board.** Radius-3 hexagon, 37 hexes. Nexuses at opposite corners (40 HP). Centre tower; one
   home tower per side two hexes from its nexus, on the axis, so it blocks the straight lane.
   Two hills (+1 reach) on the flanks. No swamps.
2. **Match.** Draft (as today) → opening (two openings per champion, re-scripted for the small
   map; the opening's end is a quest check) → **8 rounds** → hard end: higher nexus wins, ties to
   towers held. No spells, no halves, no basic phase, no Pass/Last Word, no chains.
3. **Programming (40 s clock).** Each team secretly writes, per champion: an optional **move**
   (up to SPD hexes) and up to **two abilities in strictly descending initiative**. A team also
   names one **Focus** enemy. Energy `E(r) = min(10, 5 + ceil(r/2))`: a move costs 1, an ability
   costs its initiative. Unspent energy is lost; Lost orders refund as Carry (cap +3). The AI
   commits before it reads the player's program.
4. **Commitment by tier.** Tier 3–4 abilities commit a facing or anchor hex; tier 2 commits a
   hex; tier 1 commits a champion and tracks it anywhere in range. Auto-aim rules (nearest,
   lowest HP, Focus first) fill in anything not committed.
5. **Reveal.** Everything is shown at once; then the round resolves in beats.
6. **Move beat.** All moves resolve simultaneously. A conflict over a hex goes to the **priority
   token** holder; the token alternates each round (the team that picked second in the draft
   holds it in round 1).
7. **Tiers 4 → 3 → 2 → 1.** In each tier every program from both teams resolves against the
   board as it stood at the tier's start, in the fixed sub-order displacement → control →
   damage → deaths. **Exhaust cancels the target's unresolved programs this round.** The dead
   finish their tier, then are removed.
8. **Lost and Fizzled.** A program whose caster died in an earlier tier is **Lost** (greyed, the
   killer named, no cooldown, energy refunded). A program with no legal target is **Fizzled**
   (its pattern drawn on empty hexes, cooldown spent). Nothing is silently discarded.
9. **Give/want is temporal.** A status applied in tier N counts for its wanters in all lower
   tiers this round and all of next round (×2, at least +3). Caught alone ×1.5 stays.
10. **Round close.** Status phase (burn, poison), siege (each held tower: 1 to the enemy nexus;
    a champion adjacent to the enemy nexus hits it for its basic damage), **kill tax 4** per
    death, quests (+8% POW per stack; at 2 stacks the upgrade **raises one ability's initiative
    by 1** instead of a stat), respawn (base + 1 per stack lost), win check.
11. **Momentum levers (from v2) stay:** death costs half the stacks; a carry stays dead longer;
    caught alone; a death inside the round is immediate at tier end.
12. **Optional toggles for the harness:** the owner's alternating ceiling with the Clash
    opener; slots instead of energy; armed programs ("call the battle").

**Decision budget:** 5 draft + 5 opening + ~4–6 per round × 8 ≈ **45–55 real decisions**, 10–20
seconds each, ~11 minutes. Today: ~270.

## The good parts, made better

- **Secret commit → read, not guess.** Everything but intent is public: each enemy champion's
  program space is ≤ 4 options with known cooldowns and positions. The threat overlay makes the
  read explicit. A wrong read is shown in full at reveal: "that is the sentence you misread".
- **Initiative becomes identity.** Under tiers, a champion's initiative histogram *is* its tempo:
  an alpha (4,3,2), a sniper (2,1,1), a controller (3,3,1). Show the histogram on the draft card.
  Give/want becomes a chain of tiers (a giver at 3 feeding a wanter at 1 is a same-round combo),
  which makes the draft's pair-finding geometric rather than icon-matching.
- **Combos inside a turn.** The descending pair is the Snap combo: root at 3, execute at 1, in
  one program, visible as one beat sequence. Content note: give abilities should lean to tiers
  3–4 and cash-ins to 1–2 so the pair falls out of the kits naturally.
- **Momentum.** A tier-4 double pattern that kills two before they act; an exhaust that nets the
  carry's execute; a dodged 4 that reveals the opponent read the move. All deterministic.

## The bad parts, and whether they are fixable

| Problem | Fixable? |
|---|---|
| The undercut (starter deletes the enemy's 3s and 4s by opening low) | Yes: simultaneous tiers remove it; the Clash fixes it inside the ceiling variant |
| Ten serial beats a round under alternation | Yes: tiers give four |
| Explicit targets break when targets move | Yes: commit by tier (facing / hex / tracked champion); moving out of a pattern is dodging |
| Who starts is public, so the non-starter programs only 1s and 2s | Only under the ceiling variant; measure the non-starter's share of 3s and 4s, soften cuts if < 25% |
| Everyone adjacent on a 37-hex board, so "caught alone" and "no ally beside it" quests go trivial | Partly: quests need re-authoring per map; caught alone may need distance 2 or removal |
| Hidden programs make the AI a game of imperfect information | Yes, for a prototype: a heuristic opponent model ("the enemy programs its highest tier that can hit something"), then search the reply; the opening planner is the seed |
| Three lanes become centre plus two flanks; jungle, beacons, swamps go | Accepted loss for a quicker game; the opening still shapes the first contact |
| Hidden state across rounds (armed programs) bends the pillar further | Avoid for now; toggle later |

## How to measure (the harness, once a prototype exists)

- Decisions with a real choice per match (target ≤ 60) and wall-clock per round.
- Orders per team per round (target ~3), **Lost/Fizzled per round** (target < 1), share of orders
  at each initiative (if 1s exceed 50%, low programming dominates), the **non-starter's share of
  3s and 4s** under the ceiling toggle.
- Starter / token-holder win rate (48–52%).
- Deaths, burst share, carry deaths and carry-kill win rate, lead changes, comebacks — compared
  with v2's 8–9, 18%, 2.5, 60–66%, 0.7, 22%.
- Draft effect (≥ 65/35) and opening effect (60–65%) on the small map.
- Combo rate: descending pairs that paid off, per team per match (target 1–2).
- AI time against the 1.5 s budget.

## Claude's recommendation

Prototype it, beside v2, as a separate rules mode on a separate board, with the three toggles
(resolution variant, budget variant, armed programs) so the harness can compare. Build order:
engine (program + tiers + Lost/Fizzled), heuristic AI, harness metrics, then the browser's
programming screen and staged reveal. The first measurement to look at is whether the reveal
produces bursts and carry kills that pay — the two things v2 could not deliver by tuning.
