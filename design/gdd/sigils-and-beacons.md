# Sigils & Beacons

> **Status**: Drafted (pending review)
> **System**: Combo system — Feature layer, MVP
> **Depends on**: Initiative Ladder · Ability Definition Schema · Map & Terrain ·
> Opening Phase
> **Blocks**: Ability Definition Schema (sigil fields) · AI Opponent (chain search) ·
> Objectives & Scoring (beacon destruction cost)
>
> **Reading the markers.** ⚠ marks a value or claim assumed rather than derived.
> ▸ marks something this document deliberately does **not** decide, naming the system
> that owns it.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps:
> `Initiative Ladder, Ability Definition Schema`

## Overview

A **chain** lets two champions of the same team resolve two abilities as a **single
ladder step**, with no gap for the opponent to act in between. A chain is legal when both
abilities carry the same **active sigil**. Sigils reach an ability two ways: an ability
may have a **printed sigil**, fixed for that ability in every match, or an empty **typed
slot** that goes live only while its champion stands inside a **beacon** carrying that
sigil. Beacons are visible board objects, placed mostly during the Opening Phase, and
destroyed rather than expired.

The system exists to solve one problem. A player who spends rounds manoeuvring for a
payoff can currently be denied it **procedurally, for free**, in two separate ways —
detailed in rule 1. Neither denial requires the opponent to move a champion, hold a
position, or read anything. The design principle adopted in response is that **counterplay
to a combo must be spatial and anticipatory, not procedural and reactive**: the opponent
must answer a combo by doing something on the board, ideally before it starts, rather than
by declining to participate.

A chain resolving as one indivisible step removes both denials at a stroke. What replaces
them is a set of counterplays that all cost the opponent something real — kill a chain
partner beforehand, break the beacon, punish the huddle a beacon forces, or refuse the
ground and give up the score it ticks.

## Player Fantasy

**The feeling is that the moment you built cannot be taken from you cheaply.**

The Initiative Ladder's fantasy is *being right about a person* — reading an opponent and
having the answer. This system is the other half: **being right about the board**. You
spent two rounds walking a champion somewhere that looked absurd, planted a beacon in a
place your opponent glanced at and dismissed, and now the shape is there and it is yours.

The specific thing to protect is that the payoff is not *safe* — it is *unstoppable once
begun*. Those are different, and the difference is the whole design. Your opponent watched
the beacon go down. They knew which two champions had the sigil. They could have killed
one, broken the beacon, stood somewhere else, or dropped a pattern on the huddle. Every
one of those costs them something. What they cannot do is shrug, play a cheap card, and
make the last two rounds of your play illegal.

For the opponent, the fantasy is not frustration but **reading a threat forming**. A
beacon is a public declaration: *I intend to fight here*. That should feel like watching
someone set a trap in front of you and having to decide, with full information, whether
the ground is worth it.

## Detailed Rules

### 1. The problem this solves

Two denials exist in the ladder as specified, and they are distinct.

**Denial A — the pass cuts your turn count.** Play alternates, so a team plays only as
many abilities as the opponent grants by answering. A three-ability sequence needs two
answers; an opponent who simply passes gives one Last Word and closes the half. *Two*-piece
sequences already survive, because the Last Word covers them.

**Denial B — the ceiling crash.** An answer must be at initiative ≤ the last initiative
played. A team that opens at 3 and holds a second initiative-3 ability can be answered
with an initiative **1**, dropping the ceiling to 1 and making the follow-up **illegal for
the remainder of the half**. The ceiling is symmetric — the answering team capped itself
too — but that trade favours whoever has less they want to do, which is always the
defender.

Denial B is the sharper of the two and the one this system principally addresses.

> **Partly superseded, 2026-08-17.** `movement-and-targeting.md` moved movement off the
> ladder into a basic-action phase that closes before the exchange begins, so **positions
> are locked during a ladder exchange**. The cheapest and most universal ceiling crash —
> stepping a champion out of the pattern — no longer exists, because nobody can step
> anywhere. Denial B survives only through deliberate low-initiative *abilities*, which is
> a chosen cost rather than something every champion holds for free. Denial A is
> untouched. This system remains necessary for both, but the problem it faces is smaller
> and sharper than when it was designed.

### 2. Sigils

There are ⚠ **3 sigils** in the game. A sigil is an opaque tag; it carries no effect of
its own and exists only to determine what chains with what.

An ability has **at most one printed sigil** and **at most one typed slot**. Most abilities
have neither. Both are **fixed properties of the ability, identical in every match** — they
are never generated, drafted, or randomised. Pillar 1 forbids randomness outright, so this
is not merely a preference; a rolled sigil was never available. Fixed sigils are also what
make the draft legible: a player can learn what a champion chains with, once, and know it
forever.

A **slot is typed** — it names one specific sigil it accepts. A slot is not a wildcard. An
untyped slot that accepts whatever a beacon offers was measured and rejected in rule 8.

### 3. Active sigils

An ability's sigil is **active** when it can be used for a chain:

- A **printed** sigil is always active, anywhere on the board, with no precondition.
- A **slotted** sigil is active only while the champion playing that ability stands within
  a **friendly beacon** whose sigil matches the slot.

This is the only place beacons enter the system, and it is deliberately *upstream* of the
chain rule rather than inside it.

### 4. The chain rule

> Two abilities resolve as a **single ladder step** when all of the following hold:
>
> 1. They are played by **two different champions** on the same team.
> 2. Both champions have an **action available** this half.
> 3. The two abilities share at least one **active sigil**.
> 4. The **first** ability played satisfies the current ladder ceiling.

**The rule does not mention beacons**, and must not. Beacons are one *source* of active
sigils; the chain rule is stated purely in terms of sigils being active. This is not
tidiness — it settles a question that would otherwise need its own ruling. Because each
ability independently requires *its own* sigil to be active:

| Chain | Who must stand in a beacon zone |
|---|---|
| printed + printed | **Neither.** Works anywhere on the board. |
| printed + slotted | **Only the slotted champion.** |
| slotted + slotted | **Both** — and both in a zone of the same sigil. |

Nobody decides this. It falls out.

**A chain is one ladder step but two champion actions.** A two-link chain spends two of a
team's five champions in a single beat, leaving three against the opponent's five. That
action economy is the primary brake on chaining, and it is why a chain is a commitment
rather than a free upgrade.

⚠ Chain length is capped at **2** for MVP. See `chain_length_cap`.

### 5. Chains may ascend

The ladder otherwise only ever descends, which means a combo's payoff must be *weaker*
than its setup — backwards, and the reason combos barely exist today. **Within a chain,
the second ability may be at any initiative, including above the first and above the
current ceiling.** A cheap initiative-2 displacement may chain into an initiative-4 fixed
pattern.

**After the chain, the ceiling is set to the highest initiative in it** (F3). Chaining into
a tier-4 therefore hands the opponent the right to answer with anything they own. The
ladder's central bargain — big abilities invite big answers — is preserved exactly; the
chain only lets a team spend its two actions in the order that makes sense.

Note what this does to denial B: a team whose ceiling was crashed to 1 can still open a
chain at 1 and ascend out of it. The crash becomes a cost rather than a veto.

### 6. Beacons

A **beacon** is a board object belonging to the team that placed it. It occupies a hex,
carries exactly one sigil, and is **visible to both players at all times** — as everything
in this game is.

- **Zone**: radius **1** — the beacon's hex and its neighbours (F4).
- **It does not block movement or occupancy.** A beacon is a marker, not terrain. A
  champion of either team may stand on a beacon's hex. This keeps rule 3 of Map & Terrain
  intact: the board has no impassable hexes.
- **It fills matching slots** for friendly champions standing in its zone (rule 3).
- **It is destroyed, never expired.** A beacon persists until broken.

### 7. Placing and breaking beacons

**Beacons are not a per-team allowance.** There is no beacon count. A team has as many
beacons as it has abilities that place them, and such abilities are rare because a beacon
is powerful.

> **The mechanism now exists.** `opening-phase.md` rule 2 defines `PlaceBeacon(role,
> sigil)` as one of the two opening instruction kinds: the beacon appears on the hex that
> role occupies *at the moment the instruction resolves*, so placing it before or after
> moving that role puts it in different places.

**Placement is mostly Opening Phase.** An ability that places a beacon during the Opening
Phase pays for it with a ⚠ cooldown carried into the action phase — the champion begins
the match having already spent something.

**Action-phase placement exists but is rarer still.** ⚠ Strictly fewer abilities should
offer it. The reason is an information argument rather than a power one: an Opening-Phase
beacon is placed **blind**, before anyone knows where the match will actually be fought,
while an action-phase beacon is placed knowing exactly where all ten champions stand. The
same object is worth far more when placed with information, so the price scales with the
information rather than with the object. The two kinds of ability should not be balanced
against each other; they are different goods.

**Beacons are destroyed by spending champion actions on them.** ⚠ Beacon durability should
be priced at roughly **what the chain it enables costs — about two champion-actions**. The
intent is a race: two champions converging to use a beacon while two converge to break it,
with both sides spending the same currency.

> **The trap, stated so it is not walked into.** Beacon destruction must **never** be a
> cheap, low-initiative action. A one-action initiative-1 "break beacon" would recreate
> precisely the cheap-procedural-denial-of-an-expensive-setup problem this entire system
> exists to escape — the design folding back into the thing it fixed. If beacon-breaking
> is ever tuned downward, this is the failure mode to check for first.

### 8. Why slots are typed, not wild

An earlier draft let a slot accept whatever sigil a beacon carried. Measured across
200,000 random teams (`dotnet run --project tools/Augury.Tools sigils`), a single beacon
then made **4.7 to 9.7 of a team's ten champion duos** mutually chainable in *every*
configuration tested. One beacon turned half the team into a combo engine.

Typed slots behave. At 3 sigils, 15% of abilities printed and 25% slotted:

| | no beacon | one beacon |
|---|---|---|
| team can chain at all | 56.4% | 98.9% |
| chainable duos (of 10) | **1.01** | **4.08** |
| drafts with no chain at all | — | 1.1% |

Chains are scarce by default and **manufactured** by board play — a fourfold beacon lift,
which is what keeps beacons load-bearing rather than decorative. In kit terms this is
**3 printed, 5 slotted and 12 plain abilities out of 20**, which also settles the
legibility question: most abilities carry no sigil iconography at all.

### 9. The huddle tax

A slot+slot chain requires both champions inside one 7-hex zone. A fixed tier-4 pattern is
5 hexes. The exposure this creates is the cost of manufacturing a chain, and it is
measured (`dotnet run --project tools/Augury.Tools beacon`):

| tier-4 shape | in-zone pairs catchable | board pairs catchable | safe pairs in zone |
|---|---|---|---|
| compact (blob, wedge) | **81%** | 16% | **4 of 21** |
| line | 24% | 8% | 16 of 21 |

Two champions huddled for a chain are roughly **5× easier to catch** than two standing
freely. But four of the twenty-one in-zone pairs remain safe, so *where inside the zone*
the two champions stand is its own positional game rather than a formality.

▸ **Tier-4 pattern shape is the lever here, and belongs to the Ability Definition
Schema.** A compact tier-4 punishes huddles; a line barely does. If beacon combos need
more risk, the correct response is to print more compact tier-4 shapes, not to change
anything in this document.

### 10. Beacons and objectives

A beacon's zone is 11.5% of the board at most, so refusing one is never ruinous — which is
what keeps "route around it" honest rather than oppressive. But refusing is never *free*
either, because the ground worth denying is the ground that ticks score.

A tower has 6 playable approaches. Measured against all five towers:

| Beacon placement | Approaches left outside the zone |
|---|---|
| **on** the tower hex | **0 of 6** — contesting it at all means entering the zone |
| **beside** the tower | **3 of 6** — but the tower hex itself is still in the zone |

So beside-placement taxes **occupying** the tower while leaving room to fight for it from
the far side; on-placement taxes **contesting** it at all. ▸ Whether a beacon may legally
sit on a tower or nexus hex is **open** (question 3).

**Beacons are also the tempo the map currently lacks.** Map & Terrain notes that with
minion waves deferred, nothing makes one round better than another for contesting a tower.
A beacon supplies exactly that — a place and a moment where a fight is favourable — and it
is player-generated rather than schedule-generated. ▸ Whether this removes the need for
minion waves entirely is owned by **Objectives & Scoring**.

## Formulas

### F1 — Active sigils of an ability

```
active(ability A, champion X) =
      { printed(A) }                         if A has a printed sigil
    ∪ { slot(A) }                            if A has a slot
                                             AND ∃ beacon b :
                                                   team(b) = team(X)
                                                 ∧ sigil(b) = slot(A)
                                                 ∧ distance(hex(X), hex(b)) ≤ beacon_radius
```

**Variables:** `printed(A)` ∈ {none} ∪ sigils · `slot(A)` ∈ {none} ∪ sigils ·
`beacon_radius` = 1.
**Output:** a set of 0, 1 or 2 sigils.
**Example:** an ability with printed sigil `II` and no slot returns `{II}` wherever its
champion stands. An ability with no printed sigil and a slot typed `I` returns `{}` in
open ground and `{I}` while its champion stands within 1 hex of a friendly `I` beacon.

### F2 — Chain legality

```
chainable(A on X, B on Y) ⟺  X ≠ Y
                            ∧ team(X) = team(Y)
                            ∧ available(X) ∧ available(Y)
                            ∧ active(A, X) ∩ active(B, Y) ≠ ∅
                            ∧ initiative(A) ≤ ceiling
```

**Note** that only `A`, the first ability, is tested against the ceiling. `B` is
unconstrained — that is rule 5.
**Example:** ceiling is 1. `A` is initiative 1 with printed sigil `II`; `B` is initiative 4
with printed sigil `II` on a different, unacted champion. Legal. The pair resolves as one
step and the ceiling becomes 4.

### F3 — Ceiling after a chain

```
ceiling' = max(initiative(A), initiative(B))
```

**Range:** 1–4. **Example:** a 2 → 4 chain leaves the ceiling at 4, so the opponent may
answer with any ability they hold, including their own tier-4.

### F4 — Beacon zone

```
zone(b) = { h : distance(h, hex(b)) ≤ beacon_radius  ∧  h ∈ playable board }
```

**Output size** is position-dependent, because the zone clips against the board edge:

| Placement | Zone size | Share of board |
|---|---|---|
| interior (incl. any tower) | 7 hexes | 11.5% |
| nexus hex | 5 hexes | 8.2% |
| lane mouth / side corner | 4 hexes | 6.6% |

**This makes rule 7 self-enforcing.** A beacon planted safely at home controls **43% less
ground** than one carried into the middle. Late, dangerous placement is rewarded by
geometry alone — no separate rule is needed to make it "harder but better".

## Edge Cases

| # | Situation | Resolution |
|---|---|---|
| 1 | A chain's second champion has already acted this half | Chain is illegal (F2). The first ability may still be played alone. |
| 2 | The first ability of a chain displaces its own chain partner out of the beacon zone | Chain legality is evaluated **once, at declaration** (F2), and is not re-checked between the two resolutions. The chain completes. Re-checking would make a team's own displacement effects sabotage their combos, which is absurd and unteachable. |
| 3 | The first ability kills the second ability's only legal target | The second ability **fizzles and its action is spent**. Chains are declared as a unit and greed is not refunded. |
| 4 | A beacon is destroyed "between" the two abilities of a chain | Cannot arise. A chain is one indivisible ladder step; there is no point between them at which anything else resolves. |
| 5 | A champion stands in two friendly beacon zones with different sigils | Both slots' sigils are available. F1 returns a set; a champion may satisfy either. |
| 6 | A champion stands in an **enemy** beacon's zone | Nothing happens. F1 requires `team(b) = team(X)`. See open question 2 — the alternative is deliberately interesting. |
| 7 | Two beacons of the same team and same sigil overlap | Legal, and pointless. The union of the zones is what matters; a champion in either is served. |
| 8 | A chain is played as the **Last Word** | ⚠ **Permitted.** The Last Word grants one *ladder step*, and a chain is one ladder step. This makes holding a chain a strong reason to bait a pass — an intended amplification of a line the Initiative Ladder already rewards. See open question 1. |
| 9 | A team has no legal single action but does have a legal chain | The chain is playable. A chain is a ladder step, so its availability alone prevents the half from ending by exhaustion. |
| 10 | A beacon-placing ability is played in the Opening Phase by a champion that then dies | The beacon persists. Beacons belong to the **team**, not to the champion that placed them, and nothing about a beacon references its placer after placement. |
| 11 | A champion tries to walk onto a beacon's hex | Permitted. Beacons do not block movement or occupy the hex for any purpose (rule 6). |
| 12 | A beacon is placed in jungle | Permitted, and strong — jungle is a road only the jungler walks, so a jungle beacon is expensive for the enemy to reach. ▸ Whether this is *too* strong is for the Balance Harness. |
| 13 | A beacon is placed on a spawn row hex | Illegal. Spawn rows are off the playable board and untargetable by anything (Map & Terrain rule 5). |
| 14 | Both chain abilities target the same hex | Legal. They resolve in declared order; the second sees the board as the first left it. |
| 15 | A chain would ascend past initiative 4 | Cannot arise. Initiative is bounded at 4 by the Ability Definition Schema. |

## Dependencies

| System | Direction | What passes across |
|---|---|---|
| **Initiative Ladder** | ← consumes, → modifies | A chain is a single ladder step. Modifies the descending-ceiling rule (F3) and the Last Word (edge case 8). |
| **Ability Definition Schema** | → requires | Must carry `printed_sigil` and `slot_sigil` fields per ability, and owns tier-4 pattern shapes, which set the huddle tax (rule 9). |
| **Map & Terrain** | ← consumes | Board geometry, tower positions, jungle, spawn rows. Beacons supply the tempo that document flags as missing. |
| **Opening Phase** | ← consumes, → requires | Owns beacon placement before round 1 and the cooldown that placement costs. |
| **Objectives & Scoring** | → requires | Owns beacon destruction cost, and whether beacons remove the need for minion waves. |
| **AI Opponent** | → requires | Must search chains, not only single actions — this materially enlarges the action space and is a risk to the 1.5 s decision budget. |
| **Champion Data & Stat Model** | ← consumes | Action availability per half. |

## Tuning Knobs

| Knob | Default | Safe range | Affects | Failure at the edges |
|---|---|---|---|---|
| `sigil_count` | ⚠ 3 | 3–5 | Chain scarcity, draft legibility | At 5, natural chains fall to 1.65 duos and dead drafts rise; at 2, everything chains with everything |
| `printed_sigil_rate` | ⚠ 15% | 10–30% | How draft-determined combos are | At 35% a team holds 4.1 chainable duos with no beacon and beacons stop mattering |
| `slot_rate` | ⚠ 25% | 15–35% | How beacon-determined combos are | At 15%, 6.4% of drafts cannot chain at all; at 35% one beacon reaches 5.2 duos |
| `beacon_radius` | **1** | 1–2 | Ground controlled, huddle exposure | At 2 a zone is 19 hexes (31% of board) and routing around it stops being possible |
| `chain_length_cap` | ⚠ 2 | 2–3 | Action economy, ladder health | At 3 a chain spends 3 of 5 champions in one step; likely self-punishing, but untested |
| `chain_ascent_cap` | ⚠ unlimited | +1 … unlimited | How completely a chain defeats the ceiling crash | Capped at +1, denial B returns for any combo spanning more than one initiative step |
| `beacon_durability` | ⚠ 2 actions | 1–3 | The use-vs-break race | At 1 action, breaking is cheaper than chaining and beacons never survive to matter |
| `opening_beacon_cooldown` | ⚠ TBD | — | Cost of a blind beacon | Owned jointly with Opening Phase |

### Knobs that interact

- **`printed_sigil_rate` × `slot_rate`.** These two set where combos come from — draft or
  board. They are not independent: raising printed above ~25% makes beacons redundant
  regardless of slot rate, because a team already chains freely without one.
- **`beacon_radius` × tier-4 pattern size.** The huddle tax exists because a 7-hex zone is
  close to a 5-hex pattern. At radius 2 the zone is 19 hexes and a tier-4 can no longer
  meaningfully punish a huddle, so the cost of a slot+slot chain silently disappears.
- **`beacon_durability` × `chain_length_cap`.** Breaking a beacon must cost about what
  using it costs. If a chain spends 2 actions and a break costs 1, denial strictly
  dominates.

## Acceptance Criteria

### Rules

1. **GIVEN** two abilities with the same printed sigil on two unacted champions of one
   team, **WHEN** a chain is declared anywhere on the board, **THEN** it is legal with no
   beacon present (rule 3).
2. **GIVEN** an ability with a slot typed `I` whose champion is **not** in an `I` beacon
   zone, **WHEN** a chain is attempted with it, **THEN** it is illegal (F1).
3. **GIVEN** a slot+slot chain, **WHEN** only one of the two champions stands in the
   matching zone, **THEN** the chain is illegal — each ability needs its own sigil active.
4. **GIVEN** a ladder ceiling of 1 and a legal chain from initiative 1 into initiative 4,
   **WHEN** it resolves, **THEN** both abilities resolve and the ceiling becomes 4 (F2, F3).
5. **GIVEN** a chain in progress, **WHEN** the opponent attempts any action between its two
   abilities, **THEN** no such opportunity exists — the step is indivisible.
6. **GIVEN** a chain whose first ability displaces its own partner out of the beacon zone,
   **WHEN** it resolves, **THEN** the second ability still resolves (edge case 2).
7. **GIVEN** a beacon, **WHEN** a champion of either team moves onto its hex, **THEN** the
   move is legal (rule 6).
8. **GIVEN** a beacon whose placing champion has died, **WHEN** the team attempts a chain
   in its zone, **THEN** the beacon still functions (edge case 10).

### Geometry

9. **GIVEN** a beacon at any interior hex, **WHEN** its zone is enumerated, **THEN** it
   contains exactly 7 hexes; on a lane mouth, exactly 4 (F4).
10. **GIVEN** a beacon on a tower hex, **WHEN** the tower's approaches are enumerated,
    **THEN** all 6 lie inside the zone; beside it, exactly 3 lie outside (rule 10).

### Balance — for the harness, not for unit tests

11. **GIVEN** 200,000 random teams at the default knobs, **WHEN** chainable duos are
    counted, **THEN** the mean is **≥ 0.8 and ≤ 2.0** with no beacon and **≥ 3.0 and
    ≤ 5.0** with one, and **< 3%** of drafts hold no chain at all (rule 8).
12. **GIVEN** the default knobs, **WHEN** the beacon lift is computed, **THEN** it is
    **≥ 2×**. Below that, beacons are decoration and the system's spatial half has failed.
13. **GIVEN** two champions positioned for a slot+slot chain, **WHEN** compact tier-4
    coverage is measured, **THEN** **> 60%** of in-zone pairs are catchable and **≥ 2**
    pairs are safe. The tax must be real *and* playable around (rule 9).
14. **GIVEN** simulated halves, **WHEN** their endings are classified, **THEN** the share
    ending by deliberate pass remains **≥ 50%**. The Initiative Ladder measured 68%; if
    chains drive it below half, chaining has eaten the alternation the ladder exists for.
15. **GIVEN** simulated matches, **WHEN** chains are counted, **THEN** a chain occurs in a
    **minority** of halves. A chain in most halves means combos have become the default
    line rather than a moment.

### Cross-system

16. **GIVEN** the AI at the default knobs, **WHEN** it searches a half including chain
    actions, **THEN** it stays within the 1.5 s decision budget.
17. **GIVEN** an identical board and identical kits, **WHEN** a chain resolves twice,
    **THEN** the outcomes are byte-identical (ADR-0002).

## Open Questions

| # | Question | Why it matters | Owner | By when |
|---|---|---|---|---|
| 1 | **May a chain be played as the Last Word?** Currently yes (edge case 8) | Makes baiting a pass much stronger. The ladder measured 68% of halves ending by deliberate pass; if holding a chain makes passing unsafe, that figure drops and passing stops being a real decision | Initiative Ladder + Balance Harness | Before ladder prototype round 4 |
| 2 | **May a champion use an *enemy* beacon's sigil?** Currently no (edge case 6) | If yes, a beacon becomes contested ground rather than owned ground, and destroying one stops being obviously correct — you might prefer to take it. Genuinely interesting, and it would deepen the race in rule 7 | Design | Vertical Slice |
| 3 | **May a beacon sit on a tower or nexus hex?** | The difference between rule 10's two rows being a real choice or only one existing. On-tower placement makes contesting unavoidable, which may be too strong for the strongest hex on the board | Design | Before Ability Schema is authored |
| 4 | **Does `chain_ascent_cap` need to exist at all?** Unlimited ascent means a ceiling of 1 still permits a tier-4 finisher | It is the mechanism that defeats denial B, so capping it partly reintroduces the problem. But unlimited may make the ceiling meaningless for teams holding chains | Balance Harness | Before Objectives GDD |
| 5 | **Do beacons remove the need for minion waves?** They supply the player-generated tempo Map & Terrain flags as missing | If yes, an entire deferred system never needs building | Objectives & Scoring | Vertical Slice |
| 6 | **Can the AI search chains inside budget?** The action space grows from *n* single actions to roughly *n* + (chainable pairs × abilities) | The 1.5 s budget is the performance number that actually matters for this game | AI Opponent | Before AI GDD is approved |
