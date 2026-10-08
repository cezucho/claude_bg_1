# AUGURY — MVP Rules (First Playable)

> **Status**: Authoritative for the build · 2026-09-27, **v2 changes 2026-10-04 (next section)** · numbers retuned after 500 self-play matches (D-025, D-026)
> **Author**: Claude, under standing authority from the project owner
> **Purpose**: One sheet that states every rule the simulation implements. Where it
> conflicts with a GDD, **this sheet wins for the first playable** and the conflict is
> logged in `design/claude-decisions.md` with its ID.
>
> This is deliberately not an 8-section GDD. Playtesting will rewrite most of it; its job
> is to be complete and unambiguous, not polished. ⚠ marks a number chosen without
> measurement — the self-play harness exists to replace those.

---

## v2 — what changed (2026-10-04) · this section wins over the sections below

Agreed with the owner in `design/v2-direction.md`. Roster, spells and measurements are in
`design/v2-champions.md`; decisions are D-038 to D-048.

- **Board: Field 7** (`assets/data/boards/board_field7.json`), 7 rows, front lines 6 rows
  apart, nexus on the start line. **Towers are solid**: no champion may enter a tower hex by
  any means (`towersBlock`).
- **Winning: the nexus HP race** (D-038). Each nexus has 60 HP. At every round close each
  tower a team holds fires at the enemy nexus for 1. Every death costs the victim's nexus 3.
  An open nexus can be attacked directly. The match ends when a nexus falls; the round-30
  cap is a safety net.
- **Kit: three abilities (Q W E) plus a summoner slot (R).** Each champion also has a
  **signature opening** for slot R; a spell never brings an opening.
- **Summoner spells.** After the draft, each team gives each champion one of eight shared
  spells, with no spell twice in a team. The picks are hidden from the other team and
  revealed when the opening begins. Spells sit on the ladder like abilities, have no sigils,
  never mold, and have cooldowns of 4–6 rounds:
  - Flash, Barrier, Ignite, Heal;
  - Exhaust, Teleport (usable from spawn), Smite, Cleanse.
- **Opening casts.** An instruction may be a cast, e.g. `mid casts E`. It fires the ability
  in that slot of whoever plays that role, so it may be another champion's ability.
  - **Aim:** it aims itself by D-043's rules.
  - **No target:** it fizzles.
  - **Cost:** either way the ability goes on cooldown for at least one round (D-044). If it is
    already on cooldown, nothing happens.
  - **Damage:** opening damage can't kill; it leaves at least 1 HP (D-042).
  - **Moves stay strict:** an opening that would move a champion off the board, into another
    champion, a tower or a wall, or move a rooted champion, can't be played.
  - **Per champion:** exactly one opening casts.
  - **Statuses:** the opening counts as a half for half-based statuses.
- **Statuses.** Each verb, then how long it lasts:

  | Verb | Effect | Lasts |
  |---|---|---|
  | **Root** | Can't move, dash, swap or be moved; abilities that move the caster can't be played | Halves |
  | **Burn** | Takes N each time it resolves an ability or basic attack; shields don't block it | Rounds; ticks at upkeep |
  | **Mark** | The next hit, from anyone, deals +N | Until hit, or cleared at upkeep |
  | **Exhaust** | Deals half damage | Halves |
  | **Unstoppable** | Immune to root, push and pull; removes root | Halves |
  | **Wound** | Healing on it is halved | Rounds |
  | **Wall** | An empty hex becomes impassable | N round closes; at most 4 walls, the oldest falls first |
  | **Swap** | Caster and an ally trade places | Instant |
  | **Pull ally** | An ally moves up to N toward the caster | Instant |
  | **Cleanse** | Removes root, burn, poison, mark, exhaust and wound | Instant |

  Death clears every status.
- **Conditional damage (synergy payoffs).** Against a rooted, burning, poisoned, marked or
  exhausted target, a payoff ability hits **×1.5, and always at least +2** (D-052). The bonus
  is checked before the hit spends a mark.
- **Slam.** A push stopped short deals extra damage: 3, or 4 for Mortar's Shell (D-049, D-052).
  It can be stopped by a wall, a solid tower, a champion, the edge of the board, or a root.
  Unstoppable targets aren't moved and never slam.
- **Speed is never below 1** (D-053). Molding can slow a champion but never strand it.


## 1. Match flow

```
DRAFT → OPENING PHASE → ROUND 1 … ROUND N → MATCH OVER
                          │
                          ├─ HALF 1: basic phase → ladder phase   (round opener first)
                          ├─ HALF 2: basic phase → ladder phase   (other team first)
                          └─ ROUND CLOSE: death check → status → scoring → upkeep
```

- **Teams**: A (front line at rank −4, advances toward +R) and B (rank +4, advances
  toward −R). Five champions each, one per **role**: Top, Jungle, Mid, Bottom, Support.
- **Round opener** alternates. **B opens round 1** (D-013); A opens round 2, and so on.

## 2. Draft

- Each champion in the roster has a fixed role. Each team picks **one champion per role**.
- Pick order is snake: A, B, B, A, A, B, B, A, A, B (D-020). A picked champion leaves the pool
  for **both** teams — no mirror picks (owner's call, 2026-09-28). Only if a roster is too small
  to fill a role that way are mirror picks allowed, so the draft never stalls.
- With a roster of one champion per role the draft is a formality; it becomes a choice
  when the roster grows.

## 3. Opening Phase

Implements `opening-phase.md` as written, with the open questions answered:

- Starting hexes by role — A: Top `(0,−4)`, Jungle `(1,−4)`, Mid `(2,−4)`, Bottom `(3,−4)`,
  Support `(4,−4)`; B at the **mirror images** — Top `(−4,4)`, Jungle `(−3,4)`, Mid `(−2,4)`,
  Bottom `(−1,4)`, Support `(0,4)`. Each role starts on the same side of the board as its
  counterpart: both tops on the left, both supports on the right (owner's call, 2026-09-28;
  ADR-0005, second amendment). The mirror is `(q,r) → (q+r, −r)`.
- Teams alternate, one opening play each. **The team that does *not* open round 1 plays
  first** — so A plays first, B second (D-013).
- A play = choose an unacted champion and one of its four abilities. The ability's three
  instructions execute in order.
- Instructions: `Move(role, direction)` one hex, team-relative; `PlaceBeacon(role, sigil)`
  on that role's current hex.
- **Available** iff all three instructions can execute in sequence. **Must play** an
  available ability if any exists.
- **Fallback**: if no unacted champion of the team has an available ability, each remaining
  unacted champion of that team moves one hex in a direction of the player's choice (or
  stays, if fully enclosed), one at a time, and that team's opening is over.
- **Spending an ability in the opening does not put it on cooldown** (D-011).

## 4. The basic phase (start of each half)

Implements `movement-and-targeting.md`:

- Basics alternate: half opener, other, opener, other.
- Each team takes **2 basics per half, by 2 different champions**, compulsory. If a team
  has fewer champions with a legal basic, it takes as many as it can (D-015).
- A basic is one of:
  - **Move** — a champion (on the board or in its spawn hex) moves to any unoccupied
    playable hex whose path length through unoccupied playable hexes is 1…`SPD`.
  - **Basic attack** — a champion on the board hits one enemy champion, enemy or neutral
    structure, or enemy beacon within `RCH` (straight-line distance). Damage in §7.
- Champions block movement, friendly and enemy (D-017 records that friendlies block as
  the GDD specifies). Beacons do not block.
- After a basic move ends within an enemy's `RCH`, that enemy's `OnEnemyEntersReach`
  passive fires (§10).
- When the fourth basic resolves (or no more are possible), positions are locked for the
  half.

## 5. The ladder phase

Implements `initiative-ladder.md` Core Rules 1–7:

- Half open: ceiling = 4; every living champion on the board is **Ready**.
- The half opener plays one ability at any initiative, or passes.
- Play alternates. An answer must be at initiative ≤ ceiling; the ceiling becomes the
  initiative just played.
- **Pass** → the other team gets the **Last Word**: exactly one ability (or chain) at
  ≤ ceiling, unanswerable, or it declines. Then the half ends.
- If the team to act has **no legal ability**, the half ends immediately, **no Last Word**.
- Each champion acts **once per half**.
- A champion reduced to ≤0 HP mid-ladder stays alive and may still act (Rule 7).
- Champions in their spawn hex cannot act on the ladder and cannot be targeted.

### 5.1 Legality of an ability (ladder F1)

`initiative ≤ ceiling ∧ champion Ready ∧ on board ∧ cooldown = 0 ∧ targets ≠ ∅`.

### 5.2 Chains (`sigils-and-beacons.md`)

- Two abilities, two different Ready champions of the acting team, sharing an **active**
  sigil, resolve as **one ladder step**.
- Only the **first** ability must satisfy the ceiling; the second may be any initiative.
- After a chain the ceiling = the higher of the two initiatives.
- A chain is legal as a Last Word (D-012).
- Printed sigil = always active. Slot sigil = active only while its champion stands within
  1 hex of a **friendly** beacon of that sigil.

## 6. Targeting

| Initiative | Rigidity | How targets are chosen |
|---|---|---|
| 1–2 | Free | One target within range: enemy champion, or damageable structure (Damage abilities); ally champion or self (Heal/Shield); empty hex (Dash) |
| 3 | Rotatable | Pattern offsets from the caster in the team's frame (mirrored for B), rotated to one of six facings |
| 4 | Fixed | Pattern offsets from the caster in the team's frame — mirrored for B (ADR-0005, second amendment) |

- Free range = `clamp(RCH + ability.RangeBonus, 1, 3)`.
- **No friendly fire** (D-004): patterns affect only enemy champions and enemy/neutral
  structures, never allies.
- **No line of sight.**
- A pattern is legal if at least one enemy champion or damageable structure lies in it.

## 7. Damage and healing

- **Ability damage** = `max(1, ⌊3 × Power × POW ÷ 1 000 000⌋ − ARM)`, where `Power` is the
  ability's permille power (~`M(i)`: 1000 / 1230 / 1640 / 3300).
- **Dying** attackers deal half: `POW` is read at 50% (D-007).
- **Shield** absorbs damage before HP (after `ARM`). Cleared at upkeep.
- **Basic attack** = `max(1, ⌊2 × POW ÷ 1000⌋ − ARM)` ⚠ (D-008).
- **Heal** = `⌊3 × Power × POW ÷ 1 000 000⌋`, capped at max HP.
- HP may go below zero; nothing dies until the death check.

## 8. Structures

| Structure | HP ⚠ | Start owner |
|---|---|---|
| Tower `(0,−2)`, `(2,−2)` | 16 | A |
| Tower `(0,2)`, `(−2,2)` | 16 | B |
| Tower `(0,0)` | 16 | neutral |
| Nexus A (3 hexes) | 25 shared | A |
| Nexus B (3 hexes) | 25 shared | B |

- Damaged by basic attacks and by **Damage** effects of abilities (free-targeting or a
  pattern covering a structure hex). A nexus takes damage **once** per ability however
  many of its hexes are covered.
- **Defender scaling**: damage to a structure is multiplied by
  `1000 ÷ (1000 + 500 × defenders)`, floored, minimum 1, where defenders = champions of
  the team opposing the attacker within 1 hex of the structure (D-005). Defence slows,
  never stops.
- **Towers are captured, not destroyed**: at 0 HP a tower flips to the attacker's team and
  resets to full HP (D-006).
- **Nexus gate**: a nexus is **invulnerable until its team has lost at least one of its two
  home towers** (D-025, loosened from D-002's both-towers gate after measurement). Retaking
  the tower closes it again. Destroying a nexus ends the match immediately, whatever the score.
- **Towers shoot**: in the status phase each owned tower deals 2 ⚠ damage to every enemy
  champion within 1 hex.
- Champions may stand on tower and nexus hexes.

## 9. Beacons

- Placed only by opening `PlaceBeacon` instructions in the MVP (action-phase beacon
  abilities are deferred; D-010).
- Radius 1 zone; fills friendly champions' matching slots (§5.2).
- Do not block movement. May sit on tower or nexus hexes (D-009). A new beacon on a hex
  that already holds one replaces it.
- **Durability 2**; each enemy **basic attack** on it removes 1. Abilities cannot damage
  beacons (D-010). A beacon is not a structure and scores nothing.

## 10. Passives

Every champion has one passive. Triggers (D-014):

| Trigger | Fires |
|---|---|
| `OnDamaged` | When this champion takes damage from an enemy ability or basic attack |
| `OnEnemyEntersReach` | When an enemy ends a basic move within this champion's `RCH` |
| `OnAllyDies` | At the death check, for each friendly death |
| `OnRoundClose` | In upkeep |

Passive effects: `Retaliate(n)` — n damage to the source if within 1 hex; `Strike(n)` — n
damage to the triggering enemy; `HealSelf(n)`; `ShieldSelf(n)`; `EmpowerSelf(stat, δ)` —
permanent drift. **Damage dealt by a passive never triggers another passive** (depth 1).
Simultaneous passives resolve in champion order: acting team first, then role order.

## 11. Molding

Each ability declares `MoldUp (stat, +δ)` and `MoldDown (stat, −δ)`, applied **after** the
effect resolves. Drift clamps to `−1000…+2000` permille (`RCH` capped at `+1000`, i.e. 3
hexes). `VIT` drift changes max HP; current HP is capped to the new max but never raised.

## 12. Status effects

MVP has one: **Poison** (`amount`, `rounds`). It ticks in the status phase; a new poison
replaces a weaker one (higher amount wins). Stat `RES` does not exist.

## 13. Round close

Strict order (ADR-0006):

1. **Death check** — every on-board champion at ≤0 HP dies, and every champion that was
   **Dying** dies unless healed above 0. Each death deals **3 damage to the dead champion's
   own nexus** ⚠ (D-038). `OnAllyDies` passives fire.
2. **Status phase** — poison ticks, then tower shots. A living champion driven to ≤0 here
   enters **Dying** instead of dying.
3. **Siege** — every tower a team holds fires at the **enemy nexus for 1** ⚠, whether or not
   that nexus is open (D-038).
4. **Upkeep** — cooldowns −1, shields cleared, respawn timers −1; `OnRoundClose` passives.
5. **Win check** — see §15.

## 14. Death, Dying and respawn

- A dead champion leaves the board. **Respawn timer** = `1 + ⌊round ÷ 8⌋` rounds ⚠ (D-016).
- On respawn it appears in its **spawn hex** (off-board, behind its role's front-line hex)
  at full HP, keeping all molding drift, cooldowns reset. It enters play with a basic move.
- **Dying**: lives one more round, attacks at half `POW`, dies at the next death check unless
  healed above 0.

## 15. Winning

> **Revised 2026-10-04 (D-038, owner's decision).** Points are gone: what used to score now
> damages the enemy nexus, so every match ends with a nexus falling.

- **Destroy the enemy nexus** (**60 HP** ⚠). It is drained three ways: kills (§13.1), siege
  from held towers (§13.3), and direct attacks once it is open (§11).
- **Destroyed by a direct attack** → the attacker wins immediately.
- **Destroyed at round close** (siege and kills) → that team loses. If both fall at the same
  round close, the nexus with more HP left (less negative) wins; equal is a draw.
- **Round 30 safety cap** → the team whose nexus has more HP left wins; equal is a draw (D-018).

## 16. What is deliberately absent

Items, gold, minion waves, jungle creeps, hex statuses, enemy-addressed opening
instructions, action-phase beacon placement, statuses other than poison, a blitz clock.

## 17. Data files

Both live in `assets/data/`, are strict JSON, and are validated on load — a breach fails loudly.

**`rules_config.json`** — every tunable number, keyed exactly as in `RulesConfig`:

| Key | Value | Meaning | Decision |
|---|---|---|---|
| `roundCap` | 30 | Safety cap; more nexus HP left wins, equal draws | D-018 |
| `killSiege` | 3 | Damage to a champion's own nexus when it dies | D-038 |
| `towerSiege` | 1 | Damage each held tower deals the enemy nexus per round close | D-038 |
| `towerHp` | 16 | Tower HP; resets when captured | D-026 |
| `nexusHp` | 60 | One pool across a team's three nexus hexes; sets match length | D-038 |
| `nexusGateTowers` | 1 | Home towers a team must lose before its nexus opens | D-025 |
| `towerShot` | 2 | Damage a tower deals each adjacent enemy in the status phase | D-024 |
| `basicBase` | 2 | Basic attack damage before POW and ARM | D-008 |
| `abilityBase` | 3 | Ladder F3 `base_power` | ladder F3 |
| `defenderWeight` | 500 | Permille added to a structure's damage divisor per defender | D-005 |
| `dyingPowPermille` | 500 | POW a Dying champion reads, permille | D-007 |
| `beaconDurability` | 2 | Enemy basic attacks a beacon survives | D-010 |
| `respawnBase`, `respawnEvery` | 1, 8 | Respawn = base + round ÷ every | D-016 |
| `basicsPerHalf` | 2 | Basics per team per half | Movement & Targeting |
| `board` | "classic" | Board layout: built-in `classic` or `assets/data/boards/board_[name].json` | D-040 |
| `towersBlock` | false | Towers are impassable — owner's decision, **off until the champion rewrite** | D-039 |
| `friendliesBlock` | true | Friendly champions block movement | D-017 |
| `roundOneOpener` | "B" | Opens round 1; the other team places first in the opening | D-013 |

**`champions/champion_NN_name.json`** — one champion per file, loaded in file-name order
(the number fixes each champion's index). Schema:

```jsonc
{
  "id": "warden", "name": "Warden", "role": "Top|Jungle|Mid|Bottom|Support", "glyph": "W",
  "stats": { "vit": 34000, "pow": 900, "arm": 1000, "rch": 2000, "spd": 2000 },  // permille
  "tradeStat": "vit",                      // receives (10 − Σ initiative) × 150
  "passive": { "name": "...", "trigger": "OnDamaged|OnEnemyEntersReach|OnAllyDies|OnRoundClose",
               "effect": "Retaliate|Strike|HealSelf|ShieldSelf|EmpowerSelf", "amount": 1, "stat": "pow" },
  "abilities": [ {                          // exactly four, Q W E R
    "name": "Rebuke", "initiative": 1, "cooldown": 1,
    "target": "Enemy|Ally|EmptyHex",        // initiative 1–2 only
    "rangeBonus": 0,                        // added to RCH, clamped 1–3
    "pattern": [[1,0],[1,-1]],              // initiative 3–4 only; canonical forward frame
    "effects": [ { "kind": "Damage|Heal|Shield|Poison|Displace|Dash",
                   "power": 1000, "amount": 1, "rounds": 2 } ],   // one or two
    "scalesFrom": "pow", "moldUp": ["arm", 25], "moldDown": ["spd", 60],
    "printedSigil": 0, "slotSigil": -1,     // 0–2, or −1 for none
    "opening": ["move top forward-right", "move jungle forward-left", "beacon support 2"]
  } ]
}
```

Directions for `move` are `forward-left`, `forward-right`, `left`, `right`, `back-left`,
`back-right`. Forward and back are relative to the team's own forward direction; **left and
right are the same for both teams** — left is the top-lane side of the board, as seen on the
broadcast view — because team B's frame is a mirror, not a rotation. The validator enforces every
rule in the schema GDD's rule 9: initiatives non-decreasing, total 9–11, at most two per
tier, patterns only on initiatives 3–4 (4–6 hexes at 4), the cross rule, three opening
instructions, tier-1 abilities ranged unless they dash.

