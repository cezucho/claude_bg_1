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

**2. Action economy.** Each champion may take **one action per round**, across both
halves combined. A champion that acts in the first half cannot act in the second. A
team therefore has at most 10 actions per round and must decide how to split five
champions across two halves.

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
| **Champion — Ready** | Round begins | The champion takes its action | Eligible to act or to answer |
| **Champion — Spent** | Champion acted this round | Round closes | Cannot act or answer for the rest of the round |
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

[To be designed]

## Edge Cases

[To be designed]

## Dependencies

[To be designed]

## Tuning Knobs

[To be designed]

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
