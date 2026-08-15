# Initiative Ladder & Action Economy

> **Status**: In Design
> **Author**: user + Claude (game-designer / systems-designer domains)
> **Last Updated**: 2026-08-14
> **Last Verified**: 2026-08-14
> **Implements Pillar**: 1 (Chess, Not Dice), 4 (Five Minds, One Machine), 5 (Small Choices, Felt Not Seen)

## Summary

A round of combat resolves as a **descending exchange**. Every ability carries an
initiative value; an ability played at initiative *N* may be answered by any ability
at initiative *N* or lower, from any champion on the answering team. The exchange
walks down the scale until someone passes — and a pass ends the round for both
sides. This is where the moment-to-moment game lives: not in choosing a best move,
but in judging what your move lets them do to you, and whether your answer is worth
spending now or holding.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Ability Definition Schema, Round Phase Sequencer`

## Overview

Every ability in the game carries an **initiative** value from 1 to 4, visible to
both players at all times. A round begins when the opening team plays one ability at
any initiative; it resolves immediately, on the board, before anything else happens.
The opposing team may then answer with any ability at equal or lower initiative —
drawn from *any* of their five champions, not only the one that was targeted. That
answer resolves, and the first team may answer it in turn, at equal or lower
initiative again. The exchange descends until one side chooses to pass, and a pass
ends the round for both players.

The consequence is that an ability's initiative is a second cost, paid separately
from its cooldown. Opening at initiative 4 with a devastating strike invites an
answer from the opponent's entire remaining kit; opening at initiative 1 is nearly
unanswerable but forfeits everything above it for the rest of the round. The player
is never choosing simply the strongest available action. They are choosing what they
are willing to let the opponent do in response, and deciding whether an answer they
hold is worth spending now or keeping in reserve to threaten with.

Because a pass closes the round for both sides, declining to act is not passivity —
it is a way to deny the opponent the rest of their turn. The five champions are
therefore all live at every moment: any of them may hold the answer that matters,
and the opponent has to account for all twenty abilities on the board rather than
the one champion currently under attack.

Without this system the game is alternating-turn tactics with a draft attached: one
side acts freely while the other watches, and a strike cannot be contested until the
turn has passed. The ladder is what makes an exchange feel like a MOBA teamfight —
both sides committing into each other inside a single round — without requiring
hidden orders or real-time execution.

## Player Fantasy

**The feeling is being right about a person.**

Not "I did more damage." Not "I rolled well." The specific satisfaction this system
exists to produce is the moment you discover your read was correct — that you
predicted what a thinking opponent would do, and prepared for it before they did it.

The anchor moment, concretely: you are holding a Riposte at initiative 1. Your
opponent opens the round at initiative 4 with their heaviest strike, and you let it
land. You answer at 3. They answer at 2. The ladder has now descended to a depth
where almost nothing lives — and you still have Riposte, and they have nothing left
at 1. You spend it into a champion who cannot be defended, and close the round.
Nothing about that sequence was lucky. You spent the round reading them, and you
were right.

The inverse is the failure the player must also feel, and feel as *their own fault*:
you spent that low-initiative answer three exchanges too early because it looked
strong in the moment, and now the ladder is descending past you and all you can do
is watch. That is not bad luck. That is a decision you made, visible in hindsight,
and it is exactly the kind of mistake a player can learn not to repeat.

**Pillar alignment.** This is Pillar 1 made into a moment-to-moment texture: *"No
randomness anywhere... The only uncertainty is what a mind will choose."* It is the
mechanism of Pillar 4: *"every champion should be live at every moment through the
answer it might hold."* And the reserve decision — spend now or hold — is Pillar 5's
*"individual decisions are subtle and compound quietly,"* because the cost of
spending early is invisible until the round when it matters.

**Tone.** Cold and analytical rather than heroic. The reference is blitz chess, not
a power fantasy: the pleasure is competence under time pressure, and the emotional
register is *quiet certainty*, not spectacle. Nothing in the presentation of this
system should congratulate the player. It should show them, precisely, that what
happened followed from what they chose.

> **Framing considered and rejected**: "conducting a team" — five specialists moving
> as one machine. That fantasy is real but belongs to Molding and the Draft, where
> the team is *assembled*. Here the opponent is present in every decision, and a
> framing that omits them would miss what the ladder is for.

## Detailed Design

### Core Rules

**1. Round anatomy.** A round has two halves. Each half is one complete ladder
exchange. In the first half the **round opener** plays first; in the second half the
other team plays first. The round opener alternates every round.

**2. Action economy.** Each champion may take **one action per half**. Availability
resets at the half boundary, so a champion that acted in the first half may act again
in the second. A team therefore has at most 5 actions per half and 10 per round.

> **This was measured, not assumed.** An earlier draft made it one action per
> *round*, on the theory that allocating five champions across two halves was an
> interesting decision. It is not — it is a trap. `prototypes/initiative-ladder/`
> ran both: under per-round, a team entered the half it opens with **zero available
> champions in 54.9% of rounds**, 54% of halves ended by exhaustion rather than
> choice, and the median half was three resolutions. Under per-half, halves end by a
> deliberate pass 68% of the time and the median half is nine resolutions. Because
> the ladder alternates, you can only play as many abilities as your opponent is able
> to answer — so an exhausted opponent does not merely lose the exchange, it
> *cancels* yours.

**3. The ladder.** The opening team plays one ability at any initiative; it resolves
immediately and completely before anything else happens. The opposing team may then
play any ability at initiative **≤ the initiative just played**, from any eligible
champion. Play alternates, each ability at or below the previous, until the half ends.

**4. Passing and the Last Word.** A team may pass instead of playing an ability. A
pass does not end the half immediately: the opposing team may take **exactly one
further action**, at initiative ≤ the current ceiling, and the half then ends. That
final action is **unanswerable** — no response to it is permitted.

Passing is therefore a deliberate trade: you stop the exchange and preserve your
remaining champion actions for the other half, and in exchange you concede one
unanswered action to your opponent. If the opposing team has no legal action at or
below the ceiling, or declines the Last Word, the half simply ends.

> **Emergent consequence, intended.** Because the Last Word cannot be answered, a
> team holding a strong low-initiative ability benefits from being passed to.
> Baiting a pass is a legitimate line of play, and knowing when *not* to pass into a
> held answer is a skill the ladder is meant to reward.

**5. Targeting rigidity scales with initiative.** *(This is the contract the Ability
Definition Schema must implement.)*

| Initiative | Targeting | Effect on play |
|---|---|---|
| 1 | Free — any legal target in range | Always applicable, weakest |
| 2 | Free — any legal target in range | Always applicable |
| 3 | **Rotatable pattern** — a fixed shape, orientable to any of the six hex facings | Requires the target to fall within the shape |
| 4 | **Fixed pattern** — specific hex offsets in absolute board orientation, non-rotatable | Devastating, and frequently unusable |

Power scales upward with initiative; applicability scales downward. An initiative-4
ability is not a risk you take — it is an opportunity the board grants you. This is
what prevents play from collapsing toward low initiative, and it makes positioning
strategically central: teams manoeuvre to make their heavy abilities applicable, and
opponents manoeuvre to deny those hexes.

**6. Round closure.** When the second half ends, the round closes in this exact
order: **(a)** death check — every champion at or below 0 HP dies; **(b)** status
resolution — damage-over-time and other status effects apply. A champion driven to 0
or below by step (b) does **not** die: it enters the Dying state and lives one more
round, debuffed, dying at the next round's step (a).

**7. Damage cannot deny a response.** This is a consequence of rule 6 and it is
intentional and load-bearing. Because the death check happens at round close, a
champion reduced to 0 HP mid-ladder is still alive and may still answer. Alpha-striking
a champion out of an exchange is structurally impossible, which forecloses the
degenerate burst strategy that dominates most tactical games.

### States and Transitions

| State | Entry Condition | Exit Condition | Behaviour |
|---|---|---|---|
| **Half Open** | A half begins | An ability is played, or the opening team passes | Opening team may play any initiative |
| **Ladder Descending** | An ability has resolved | A team passes, or no legal action exists | Answers restricted to ≤ last initiative played |
| **Last Word** | A team passed | The opposing team acts once, or declines | Exactly one action, at ≤ ceiling, unanswerable |
| **Half Closed** | Last Word taken or declined, or no legal answers remain | Next half begins, or round closes | No actions permitted |
| **Round Closing** | Second half closed | Death check and status phase complete | No player input |
| **Champion — Ready** | A half begins | The champion takes its action | Eligible to act or to answer |
| **Champion — Spent** | Champion acted this half | The half closes | Cannot act or answer for the rest of this half; resets at the half boundary |
| **Champion — Dying** | Reduced to ≤0 HP by the status phase | Next round's death check | Acts at a penalty; dies at the next death check unless healed above 0 |
| **Champion — Dead** | At ≤0 HP during a death check | Respawn timer expires | Off board |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| **Ability Definition Schema** | initiative (1–4), cooldown, targeting rigidity tier, pattern offsets | — | Ability Schema *(provisional — undesigned; this GDD defines the required fields)* |
| **Round Phase Sequencer** | — | `half-opened`, `ability-resolved`, `last-word`, `half-closed`, `round-closed` events | Round Phase Sequencer (ADR) |
| **Damage & Combat Resolution** | resolved ability + target set | HP deltas, applied before the next ladder step | Damage GDD |
| **Status Effects** | — | status list evaluated during the round-closure status phase | Status Effects GDD |
| **Death, Dying Round & Respawn** | HP state at round close | death and dying transitions | Death GDD |
| **Molding** | each ability use | permanent stat delta, applied at resolution before the next answer | Molding GDD |
| **Movement & Targeting** | pattern offsets, board occupancy | legal target set per ability | Movement GDD |
| **AI Opponent** | legal action set at each ladder step | chosen action, or pass | AI GDD |
| **Initiative Ladder UI** | legal action set, current ceiling, spent champions, Last Word availability | player selection | Ladder UI spec |

> **Provisional assumption.** Movement is treated as an initiative-1 action that
> consumes the champion's action for the round. Under rule 5 positioning is now
> expensive and strategically central, so the Movement & Targeting GDD may need to
> revisit this. Recorded in Open Questions.

## Formulas

> All values below are **starting values for the Balance Simulation Harness to
> tune**, not derived truths. Where a number is an assumption about geometry or
> player behaviour rather than a design decision, it is labelled as such.

### F1 — Legal Action Predicate

`legal(a, s) = (a.initiative ≤ s.ceiling) ∧ (a.champion.state = Ready) ∧ (a.cooldown = 0) ∧ (targets(a) ≠ ∅) ∧ (a.champion.team = s.active_team)`

| Variable | Type | Range | Description |
|---|---|---|---|
| `a.initiative` | int | 1–4 | Initiative of the candidate ability |
| `s.ceiling` | int | 1–4 | Current ladder ceiling |
| `a.champion.state` | enum | Ready / Spent / Dying / Dead | Dying champions **are** eligible to act |
| `a.cooldown` | int | 0–4 | Rounds remaining before the ability is reusable |
| `targets(a)` | set | 0–n hexes | Empty for a tier-4 fixed pattern that does not line up |

**Output:** boolean.
**Example:** an initiative-3 ability at cooldown 0 on a Ready champion, with the
ceiling at 2 → illegal on initiative alone.

### F2 — Ceiling Update

`ceiling′ = a.initiative` after any resolved action; `ceiling = 4` when a half opens.

**Output range:** 1–4, monotonically non-increasing within a half. This property is
what makes the search space collapse as a ladder deepens, and it is the reason the
AI is tractable (see `prototypes/initiative-ladder/REPORT.md`).

### F3 — Initiative Power Budget

`raw_power(i) = base_power × M(i)` where `M = [1.0, 1.3, 2.2, 4.4]`

| Variable | Type | Range | Description |
|---|---|---|---|
| `base_power` | int | 3–5 | Reference damage of a tier-1 ability |
| `i` | int | 1–4 | Initiative tier |
| `M(i)` | fixed-point | 1.0–4.4 | Power multiplier for tier `i` |

**Output range:** 3 → 13 damage at `base_power = 3`.
**Example:** a tier-4 ability at base 3 deals 13 damage — roughly 43% of a 30 HP
champion — from a single strike that is unanswerable if played as a Last Word.

### F4 — Effective Value (the balance target)

`effective_value(i) = raw_power(i) × applicability(i) × (1 − k × exposure(i))`
where `exposure(i) = i / 4`

| Variable | Type | Range | Description |
|---|---|---|---|
| `applicability(i)` | fixed-point | 0.30–1.00 | Fraction of board states in which the ability can legally target. **A measurement of pattern geometry, not a free parameter** |
| `exposure(i)` | fixed-point | 0.25–1.00 | Fraction of the opponent's kit that may legally answer |
| `k` | fixed-point | 0.20–0.30 | Weight of the answer-window cost. Start at 0.25 |

| i | applicability | M(i) | exposure | effective_value |
|---|---|---|---|---|
| 1 | 1.00 | 1.0 | 0.25 | **0.94** |
| 2 | 0.90 | 1.3 | 0.50 | **1.02** |
| 3 | 0.55 | 2.2 | 0.75 | **0.98** |
| 4 | 0.30 | 4.4 | 1.00 | **0.99** |

**Output range: 0.94–1.02 — flat within ±4%.** That flatness *is* the design target:
no initiative tier should be systematically correct to play. The prototype measured
agents choosing a mean initiative of 1.9–2.4 out of 4 under a flat power curve; this
formula exists to correct that. The Balance Harness must verify that real pattern
geometry produces the assumed `applicability` values.

### F5 — Round Duration Budget

`round_seconds = (decisions_per_round × t_decide) + (resolutions_per_round × t_resolve)`
`match_rounds = target_match_seconds / round_seconds`

| Variable | Type | Range | Description |
|---|---|---|---|
| `decisions_per_round` | int | 3–8 | Player decision points per round, own team only |
| `t_decide` | seconds | 4–10 | Mean time spent on the blitz clock per decision |
| `resolutions_per_round` | int | 8–20 | Abilities resolved per round, both teams, both halves |
| `t_resolve` | seconds | 1.0–2.0 | Playback time per resolved ability |

**Example, using the measured per-half economy:** 5 × 6s + 16 × 1.5s = **54 seconds
per round** → 900s ÷ 54 = **17 rounds** in a 15-minute match.

**Measured:** `prototypes/initiative-ladder/ladder_v2.py` records 16.2 resolutions
per round under the per-half economy (8.9 under per-round). The per-half rule is
therefore the more expensive of the two in match-length terms — 17 rounds rather than
21 — and it was still the correct choice, because per-round produced halves that
ended by exhaustion rather than decision.

> **This is a binding constraint, not an observation.** The concept fixes matches at
> 10–15 minutes. That budget survives only if ladders average **≤16 resolutions per
> round** and the blitz clock averages **≤6 seconds per decision**. The Last Word
> rule lengthens ladders by design, which raises round duration directly.
> **Ladder length is therefore the match-length dial as well as a balance dial, and
> the two cannot be tuned independently.** There is now less headroom than the
> earlier draft assumed.

## Edge Cases

### Ladder legality and termination

- **If the responding team has no legal action at or below the ceiling**: the half
  ends immediately. **No Last Word is granted** — the Last Word follows a *pass*, not
  exhaustion. Running out of options is not a decision and must not be rewarded as one.
- **If the opening team has no legal action when a half opens** (every champion dead,
  or every ability on cooldown): the half is skipped entirely; no Last Word is granted.
  Note that Spent status cannot cause this, since availability resets at the half
  boundary.
- **If a team passes at half open, before any ability has been played**: the ceiling
  is still 4, so the opponent's Last Word may be an initiative-4 ability, unanswerable.
  Declining to open is severely punished, deliberately.
- **If the team offered a Last Word declines it**: the half ends with no further
  action. Declining is always legal.
- **If a team passes while holding no legal actions anyway**: treated identically to
  any pass — the opponent receives the Last Word. The rule does not inspect intent.
- **If the blitz clock expires during a decision**: treated exactly as a pass,
  including granting the Last Word. One rule, no special case, fully deterministic.

### Champion state

- **If a champion is reduced to 0 HP or below mid-ladder**: it remains alive, remains
  Ready if unspent, and may act and answer for the remainder of the round. It dies at
  the round-close death check. *This is Core Rule 7 and it is why alpha-strike does
  not exist in this game.*
- **If a champion at ≤0 HP is healed above 0 before the round-close death check**: it
  survives, with no death and no Dying state. Rescuing a champion inside its own
  dying round is a legitimate and intended play.
- **If a Dying champion is healed above 0 HP**: it leaves the Dying state immediately
  and the Dying penalty is removed. It dies only if still at ≤0 at the next death check.
- **If a Dying champion acts**: permitted, at the Dying penalty defined by the Death,
  Dying Round & Respawn GDD. The ladder places no additional restriction on which
  abilities it may use.
- **If a champion dies at round close while Spent**: no additional effect. Death does
  not retroactively undo an ability it already resolved.

### Targeting and patterns

- **If a tier-3 or tier-4 pattern overlaps friendly champions**: they are affected
  exactly as enemies are. **Fixed and rotatable patterns are indiscriminate by
  default.** Friendly fire is part of the cost that justifies the tier's power budget;
  an ability that discriminates must declare so explicitly in its definition.
- **If part of a pattern falls off the board**: those hexes are ignored. The ability
  remains legal provided at least one legal target exists within the pattern.
- **If a tier-4 fixed pattern has no valid target from the champion's current
  position**: the ability is not legal — `targets(a) = ∅` in F1. This is the intended
  and common case, not an error state.

### Simultaneity and ordering

- **If two effects would modify the same value within one ladder**: they never
  collide. Every ability resolves fully — damage, statuses, molding — before the next
  action is chosen. **There is no simultaneity anywhere in the ladder.** This is a
  determinism requirement, not a convenience.
- **If an ability both produces an effect and applies a molding delta**: the effect
  resolves **first**, then the molding delta is applied. An ability never benefits
  from its own stat change; the payoff arrives on the *next* use. *(Decision: the
  alternative compounds within a single use and makes damage hard to attribute, which
  Pillar 5 permits but Pillar 1 does not.)*
- **If cooldowns would tick**: cooldowns decrement once at round close, never at a
  half boundary. A cooldown-2 ability used in the first half is unavailable for the
  whole of the next round, not merely the second half.

### Degenerate strategies

- **If a team stalls by passing immediately every half**: it concedes a free
  unanswerable Last Word each half while the opponent holds objectives uncontested.
  Stalling loses on points. No dedicated anti-stall rule is needed.
- **If a team opens at initiative 1 to lock the ceiling low** — the exploit the
  prototype found — the lockout applies only to the half that team opened. The
  opponent sets their own ceiling in the half they open. **The two-half structure
  makes the lockout symmetric, which is what neutralises it.**

> **Constraints this section imposes on other GDDs**: the Death, Dying Round &
> Respawn GDD must honour the healing-rescue window; the Molding GDD must apply
> deltas after effect resolution.

## Dependencies

### Hard — the ladder cannot function without these

| System | Interface | Status |
|---|---|---|
| **Ability Definition Schema** | Supplies `initiative` (1–4), `cooldown`, `targeting_rigidity_tier`, `pattern_offsets`. This GDD defines the required fields; the Schema GDD implements them | Undesigned — **provisional** |
| **Round Phase Sequencer** | Owns round and half boundaries; emits `half-opened`, `ability-resolved`, `last-word`, `half-closed`, `round-closed` | Undesigned — ADR-owned |
| **Hex Grid & Spatial Model** | Resolves `pattern_offsets` against board occupancy to produce `targets(a)` | Undesigned — ADR-owned |
| **Champion Data & Stat Model** | Owns HP and the Ready / Spent / Dying / Dead state used by F1 | Undesigned |
| **Damage & Combat Resolution** | Applies an ability's effect before the next ladder step | Undesigned |

### Soft — the ladder works without these, and is enriched by them

| System | Interface | Note |
|---|---|---|
| **Molding** | Applies a stat delta after each ability resolves | Ladder is fully playable with molding disabled |
| **Status Effects** | Evaluated in the round-closure status phase | Without it, the Dying state is unreachable but the ladder is unaffected |
| **Blitz Clock** | Converts a timeout into a pass | Ladder is playable untimed; deferred to Vertical Slice |
| **Economy & Items** | Alters stats read by damage resolution | No ladder rule references items |
| **Objectives & Scoring** | Consumes round-close events | The ladder does not read score |

### Depended on by

| System | What it expects from the ladder |
|---|---|
| **AI Opponent** | A legal action set at each step, and a cheaply cloneable state. The monotonic ceiling (F2) is what makes search tractable |
| **Initiative Ladder UI** | Current ceiling, legal action set, Spent champions, whether a Last Word is pending |
| **Resolution Playback** | An ordered event stream, one entry per resolved ability |
| **Death, Dying Round & Respawn** | Death check precedes the status phase, and the healing-rescue window is honoured |
| **Post-Match Review** | The full ladder history per round, including answers that were legal but unused |

> **Bidirectional consistency:** none of these GDDs exist yet, so no back-references
> can be verified. When each is authored it must list this system in its own
> Dependencies section. Recorded in Open Questions.

## Tuning Knobs

All values are data-driven and must never be hardcoded (see
`.claude/docs/coding-standards.md`).

| Knob | Default | Safe range | Too high | Too low |
|---|---|---|---|---|
| `max_initiative` | 4 | 3–6 | More tiers than players can hold in mind; rigidity tiers run out of distinct geometry | At 2 the ladder collapses to a single answer window and the mechanic disappears |
| `actions_per_champion_per_half` | 1 | 1–2 | At 2, ladders roughly double and the F5 round budget breaks | Below 1 is undefined. Scoping this per *round* instead was measured and rejected — see Core Rule 2 |
| `halves_per_round` | 2 | 1–2 | Above 2 the round becomes long and the opener advantage returns unevenly | At 1 the 70% first-mover advantage returns |
| `last_word_actions` | 1 | 0–1 | Above 1, passing becomes near-suicidal and nobody ever passes | At 0 passing is a free combo-breaker — the problem this rule exists to fix |
| `last_word_ceiling_offset` | 0 | −1 to 0 | — | At −1 the Last Word must undercut the ceiling, weakening pass-baiting and making passing safer |
| `base_power` | 3 | 3–5 | Champions die in two exchanges; the ladder never descends far | Exchanges never resolve; matches run past the round budget |
| `M(i)` power multipliers | [1.0, 1.3, 2.2, 4.4] | see F3 | High tiers dominate whenever the board lines up | Play collapses to low initiative, as the prototype measured |
| `k` (exposure weight) | 0.25 | 0.20–0.30 | Over-penalises high initiative; tier 4 becomes unplayable | Under-values the answer window; high initiative becomes free |
| `applicability(i)` targets | [1.00, 0.90, 0.55, 0.30] | see F4 | Tier 4 usable too often, becoming a default rather than an opportunity | Tier 4 effectively never legal; the tier is decoration |
| `t_decide` (blitz clock) | 6s | 4–10s | Match exceeds the 15-minute budget | Players cannot evaluate the legal set; decisions become guesses |
| `cooldown` range | 0–4 | 0–4 | Kits empty out and ladders end by exhaustion | Abilities are spammable and holding a reserve stops mattering |

### Knobs that interact

- **`M(i)` × `applicability(i)` × `k`** are not independent — they are three terms of
  the same balance equation (F4). Changing one requires re-solving for flat effective
  value. Change them together or not at all.
- **`actions_per_champion_per_round` × `halves_per_round` × `last_word_actions`** all
  drive ladder length, and ladder length is the match-length dial (F5). Raising any
  of them shortens the number of rounds a 15-minute match can hold.
- **`base_power` × `cooldown`** together set how many rounds a champion survives,
  which sets how often the Dying state is reachable.

> **Owned elsewhere, referenced here:** the Dying penalty belongs to the Death, Dying
> Round & Respawn GDD; respawn timing belongs there too. Do not duplicate them.

## Visual/Audio Requirements

The visual anchor is **Stylized Arena** — low-poly 3D whose rule is *"every model
exists to be recognized instantly, not admired"* (`design/gdd/game-concept.md`). The
art bible does not exist yet, so these are requirements for it to satisfy rather than
decisions drawn from it.

**The descending ceiling must be legible without reading a number.** The ladder's
central rule is that options shrink as the exchange deepens. That contraction should
be felt in the presentation — abilities above the ceiling visibly falling out of
availability as the ceiling drops, so the player perceives the narrowing rather than
computing it.

**Each resolution is a discrete beat.** Resolutions are strictly sequential and never
simultaneous (Edge Cases). Playback must preserve that: one ability, one beat, one
readable consequence, with enough separation that a viewer can attribute each effect
to the ability that caused it. Pillar 1's promise that every defeat is legible is
delivered here, not in the rules.

**The Last Word needs its own signature.** It is the only unanswerable action in the
game and the emotional peak of most exchanges. It requires a distinct audio and
visual treatment — a change in framing or pacing, not merely a louder hit — so the
player registers that the half is ending on this action.

**Tier-4 abilities should look like an opportunity taken.** Fixed patterns are rarely
legal, so when one fires it is the payoff of several rounds of positioning. Its
presentation should carry more weight than tier 1–3, and the fixed hex pattern should
be previewed on the board in absolute orientation before commitment.

**Dying champions must be unmistakable.** A champion at ≤0 HP that is still acting is
the single most confusing state in the game to a new player. It needs a persistent,
unambiguous visual state — not a subtle tint — and the round it has left should be
communicated, not inferred.

**Restraint is a requirement, not a budget compromise.** Per Pillar 1 and the visual
rule, no effect may obscure board state for even a moment. Where spectacle and
readability conflict, readability wins — and in a game with ten units and a ladder to
evaluate, they will conflict often.

**Audio:** initiative tier should be audible. A tier-4 resolution and a tier-1
resolution should not sound alike, so an experienced player can follow an exchange by
ear while reading the board.

📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is
approved, run `/asset-spec system:initiative-ladder` to produce per-asset visual
descriptions and generation prompts from this section.

## UI Requirements

The game concept names this the **onboarding cliff**: *"a player who does not
understand answer windows will experience the game as arbitrary."* The ladder's rules
are simple; its *state* is not, and the UI carries that burden.

**The core requirement — "what can I answer with, right now?"** At every decision the
player must be able to see, without hunting: the current ceiling; which of their
twenty abilities are legal at or below it; which champions are Spent; and which
abilities are unavailable for cooldown versus for initiative versus for lack of a
legal target. Those three unavailability reasons are different decisions and must not
look alike.

**Required elements**

| Element | Requirement |
|---|---|
| Ceiling indicator | Current ceiling, always visible, with its descent legible as it drops |
| Legal-action set | All legal abilities across all five champions, in one place — not five separate bars the player must scan |
| Unavailability reasons | Visually distinct for cooldown / above ceiling / no legal target |
| Spent champions | Unmistakably distinct from Ready ones; the difference is invisible on the board otherwise |
| Half indicator | Which half is in progress, and which team opened it |
| Action allocation | How many champions remain unspent — the resource split across halves is a core decision and must not require counting |
| Last Word state | When a Last Word is pending, that it is unanswerable, and what may be played into it |
| Tier-4 pattern preview | The fixed hex pattern shown in absolute orientation before commitment |
| Blitz clock | Time remaining, with a clear warning threshold, since expiry equals a pass and concedes a Last Word |
| Ladder history | The exchange so far this half, in order, so the player can see how the ceiling arrived where it is |

**Explicitly forbidden:** hiding any of the above behind a menu, hover, or toggle.
Pillar 1 requires all state be inspectable, and `technical-preferences.md` records
that *"the UI must expose full board state — every ability, cooldown, initiative
value, stat, and molding delta — without hiding anything behind a menu."* Hover may
*enrich*, never *reveal*.

**Input:** ability hotkeys map to Q / W / E / R per `technical-preferences.md`,
borrowing MOBA muscle memory. Selecting among five champions plus four abilities plus
a pass, under a blitz clock, is a genuinely hard input problem and is the reason this
is a `/ux-design` deliverable rather than a paragraph here.

> **📌 UX Flag — Initiative Ladder**: This system has substantial UI requirements. In
> Pre-Production, run `/ux-design` for the combat HUD and the ladder interface
> **before** writing epics. Stories should cite `design/ux/[screen].md`, not this GDD.
> The systems index already lists Initiative Ladder UI as a separate MVP system.

## Acceptance Criteria

Every criterion is independently verifiable without reading this document. Simulation
criteria run in xUnit with no Godot boot (see `.claude/docs/technical-preferences.md`).

### Core rules

1. **GIVEN** a ladder with ceiling 3, **WHEN** a team attempts an initiative-4
   ability, **THEN** the action is rejected as illegal and the game state is unchanged.
2. **GIVEN** a ladder with ceiling 3, **WHEN** a team plays an initiative-3 ability,
   **THEN** it resolves and the ceiling becomes 3.
3. **GIVEN** a champion that acted in the first half, **WHEN** the second half opens,
   **THEN** that champion is Ready again and offers legal actions, subject to cooldowns.
3b. **GIVEN** a champion that acted earlier in the *same* half, **WHEN** the ladder
   returns to its team, **THEN** that champion is Spent and offers no legal actions.
4. **GIVEN** a half in progress, **WHEN** a team passes, **THEN** the opponent is
   offered exactly one action at ≤ ceiling, and after it resolves the half ends with
   no further action permitted.
5. **GIVEN** a team has just taken a Last Word, **WHEN** the opponent attempts to
   respond, **THEN** the response is rejected and the half is closed.
6. **GIVEN** the responding team has no legal action at or below the ceiling,
   **WHEN** the ladder reaches them, **THEN** the half ends and **no** Last Word is
   granted.
7. **GIVEN** a round has completed both halves, **WHEN** the round closes, **THEN**
   the death check runs before the status phase, verified by event order.
8. **GIVEN** a champion at 3 HP, **WHEN** it takes 5 damage mid-ladder, **THEN** it
   remains alive and able to act for the rest of the round, and is dead after the
   round-close death check.
9. **GIVEN** a champion at −2 HP mid-ladder, **WHEN** it is healed to 4 HP before
   round close, **THEN** it survives the death check and never enters Dying.
10. **GIVEN** a champion driven to ≤0 HP by the status phase, **WHEN** the next round
    begins, **THEN** it is Dying, may act, and dies at that round's death check
    unless healed above 0 first.
11. **GIVEN** a tier-4 fixed-pattern ability, **WHEN** the champion is rotated or the
    board is mirrored, **THEN** the pattern offsets are unchanged in absolute board
    orientation.
12. **GIVEN** a tier-4 pattern overlapping one enemy and one ally, **WHEN** it
    resolves, **THEN** both are affected identically.
13. **GIVEN** an ability with a molding delta, **WHEN** it resolves, **THEN** its own
    effect is calculated using the pre-molding stat value, and the delta is applied
    afterwards.
14. **GIVEN** an ability on cooldown 2 used in the first half, **WHEN** the next
    round opens, **THEN** the ability is still unavailable.
15. **GIVEN** the blitz clock expires during a decision, **WHEN** the timeout fires,
    **THEN** the outcome is identical in every respect to a deliberate pass.

### Formulas

16. **GIVEN** the F1 predicate, **WHEN** any of the five conditions is false, **THEN**
    `legal()` returns false — verified with one test per condition.
17. **GIVEN** any half, **WHEN** the sequence of resolved initiatives is recorded,
    **THEN** it is monotonically non-increasing (F2).
18. **GIVEN** `base_power = 3` and the default `M`, **WHEN** raw power is computed
    per tier, **THEN** the results are 3, 3.9, 6.6, 13.2 (F3).
19. **GIVEN** the default `M`, `applicability` and `k`, **WHEN** effective value is
    computed per tier, **THEN** all four results fall within 0.94–1.02 (F4).
20. **GIVEN** 5 decisions at 6s and 10 resolutions at 1.5s, **WHEN** round duration is
    computed, **THEN** it equals 45s, and a 900s match yields 20 rounds (F5).

### Determinism and performance

21. **GIVEN** an identical initial state and an identical action sequence, **WHEN**
    the round is resolved twice, **THEN** the serialised end states are
    **byte-identical**. This is a blocking gate — Pillar 1 and asynchronous PvP both
    depend on it.
22. **GIVEN** a resolved round, **WHEN** the simulation is run headless via
    `dotnet test`, **THEN** it completes without loading the Godot runtime.
23. **GIVEN** a typical mid-round position, **WHEN** the AI searches to depth 3,
    **THEN** it returns within the 1.5s AI decision budget
    (`.claude/docs/technical-preferences.md`) — the prototype measured ~1,900 nodes
    per decision, so this should hold with substantial headroom.

### Cross-system

24. **GIVEN** a full round, **WHEN** the event stream is captured, **THEN** it
    contains exactly one `ability-resolved` event per resolved ability, in resolution
    order, sufficient for Resolution Playback to reconstruct the round.
25. **GIVEN** a completed match, **WHEN** Post-Match Review requests round history,
    **THEN** each round reports the actions taken *and* the legal actions that were
    available and unused.

## Open Questions

| # | Question | Why it matters | Owner | Resolve by |
|---|---|---|---|---|
| 1 | ~~Does passing survive the Last Word rule?~~ **RESOLVED 2026-08-14.** Re-measured in `ladder_v2.py` with two halves *and* the Last Word: passing with options held at 7.7%, versus 8.6% under the original single-half rule. 68% of halves end by a deliberate pass rather than exhaustion | Passing remains a decision; the ladder does not run to exhaustion | — | Closed |
| 1b | **Is the mirror-match win asymmetry a rule property or a harness artefact?** Team 0 wins ~72–77% of mirror matches, and this does **not** move when the match opener is alternated. Damage dealt is symmetric (10238 vs 9726), so the divergence is in scoring or agent tie-breaking, not in combat | The 70% figure was originally read as a first-mover advantage and partly motivated the two-half rule. That reading is now doubtful. The two-half rule is still justified — it makes the initiative-1 ceiling lockout symmetric — but on different grounds | Design + prototype | **Before implementation.** Isolate the asymmetry before trusting any balance number from this harness |
| 2 | **Is `applicability(i)` achievable with real hex geometry?** F4 assumes tier-4 patterns are legal in ~30% of board states. That is an assumption about geometry, not a decision | If real patterns are legal 60% of the time, tier 4 dominates; at 10%, the tier is decoration | Design + Balance Harness | During Ability Definition Schema |
| 3 | **Is movement an initiative-1 action costing the champion's round action?** Assumed here. Under Core Rule 5, positioning is now expensive and strategically central | Movement cost directly determines how often tier-4 abilities can be set up — it is the other half of question 2 | Movement & Targeting GDD | Before Movement GDD is approved |
| 4 | **What is the Dying penalty?** Referenced but not defined here | Determines whether the dying round is a real tactical window or a formality | Death, Dying Round & Respawn GDD | Before Death GDD is approved |
| 5 | **Should the Last Word be capped below the ceiling** (`last_word_ceiling_offset = −1`)? | Reduces the reward for baiting a pass, making passing safer. Depends entirely on the answer to question 1 | Design | After question 1 is measured |
| 6 | **How is a Last Word communicated under time pressure?** The player has seconds to understand that an unanswerable action is available | The onboarding cliff is steepest at exactly this moment | `/ux-design` | Pre-Production |
| 7 | **Do 20 rounds at 45s hold with real content?** F5's budget is arithmetic, not measurement | If real ladders run longer, either match length or the ladder must give | Balance Harness | Vertical Slice |
| 8 | **Back-references from dependent GDDs.** None of the systems listed in Dependencies exist yet, so bidirectional consistency is unverified | `/consistency-check` cannot validate a one-sided dependency graph | Each dependent GDD | As each is authored |
