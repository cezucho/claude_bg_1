# Questions Claude Answered That the User Hasn't

> Standing authority, granted 2026-09-27: *"If there is any decision I haven't made … make
> it yourself, just write it down."* Every call below was made by Claude without the
> project owner's input. Each is reversible, and each says how.
>
> **How to use this file.** Skim the **Decision** column. Anything that feels wrong, say
> the ID ("reverse D-007") and it gets changed. Entries marked **[measure]** are guesses
> the self-play harness is expected to confirm or replace.

| ID | Question | Decision | Why | Cost of being wrong / how to reverse |
|---|---|---|---|---|
| D-001 | What does scoring award? | Kill **3**, each owned tower **+1 per round close**, nexus = instant win, target **50** **[measure]** | Follows the user's hints: points for kills and tower holding, nexus as terminator. Kills worth more than a round of one tower so fighting always pays | Numbers only. `mvp-rules.md` §13/§15 constants |
| D-002 | What makes the nexus "hard to reach"? | Nexus is **invulnerable while its team owns either of its two home towers** | The user wanted nexus destruction to require "a lot of steps". A gate is legible, cheap, and makes the comeback a two-stage plan (take both towers, then burn) | If comebacks never happen, loosen to "either tower lost"; if they're too common, add HP |
| D-003 | Can basic attacks damage structures? (Movement Q3) | **Yes** | The user said "you can always move toward a structure and try to destroy it" — that only works if basics hit structures | Raises the ladder-opt-in risk. Harness watches ladder-opened share (Movement criterion 12) |
| D-004 | Friendly fire in patterns? The ladder GDD says yes; the user said "there's no friendly fire" | **No friendly fire** | The user's statement is later and explicit. Patterns hit only enemies | Tier 3–4 become safer to cast; if they dominate, re-enable friendly fire and lower `M` |
| D-005 | Defender scaling curve | Damage × `1000 / (1000 + 500 × defenders)`, min 1 | Implements the user's "tougher while defended, never immune": 1 defender = −33%, 2 = −50% | Constant `DefenderWeight` |
| D-006 | Do towers have a destroyed state? (Map Q9) | **No — captured only.** At 0 HP a tower flips and resets | Keeps the match reversible; one state per tower is simpler to read | Add a destroyed state later if towers flip-flop too much |
| D-007 | What is the Dying penalty? (Ladder Q4) | A Dying champion's `POW` reads at 50% | Makes the dying round a real window without removing agency | Constant `DyingPowPermille` |
| D-008 | Basic attack damage | `max(1, 2×POW/1000 − ARM)` **[measure]** | Must be weak because it cannot be answered on the ladder | Constant `BasicBase` |
| D-009 | Beacon on a tower/nexus hex? (Sigils Q3) | **Allowed** | Keeps `PlaceBeacon` always legal, as the Opening Phase GDD states; the user leaned "maybe it can" | If on-tower beacons dominate, make `PlaceBeacon` illegal on structure hexes |
| D-010 | How are beacons broken; are action-phase beacon abilities in MVP? | Durability **2**, removed only by enemy **basic attacks**. No action-phase beacon abilities in MVP | Breaking a beacon costs a whole half's basic budget, never a cheap low-initiative ability — the trap Sigils rule 7 warns about | Add a beacon-placing ability kind later |
| D-011 | Does the opening ability start on cooldown? (Opening Q2) | **No** | The must-play rule can force an ability; putting it on cooldown punishes the same forced move twice | One flag in the opening resolver |
| D-012 | May a chain be the Last Word? | **Yes** (confirms the ⚠ in Sigils) | A chain is one ladder step, the Last Word grants one step | Harness watches the pass rate (Sigils criterion 14) |
| D-013 | Who opens round 1 and who places first? | **B opens round 1; A places first in the opening** | Implements the Opening GDD's assumption that the two commitments fall on different teams. ADR-0006 said "trailing team opens" — the ladder GDD's plain alternation was kept as simpler and symmetric | Swap one constant |
| D-014 | Passive trigger vocabulary | The four triggers in `mvp-rules.md` §10, five effect kinds, depth-1 | Smallest set that answers basic attacks (`Retaliate`) and makes passives differ by champion | Add triggers as champions need them |
| D-015 | One-champion-alive basic phase (Movement Q2) | A team takes as many basics as it has champions with a legal basic, up to 2 | Simplest rule with no dead end | — |
| D-016 | Respawn timer | `1 + ⌊round/8⌋` rounds **[measure]** | "Lengthening respawn" appears in earlier discussion; this keeps early deaths cheap and late aces decisive | Constant |
| D-017 | Do friendly champions block? (Movement Q4) | **Yes**, as the GDD specifies | Not re-decided — noted because it's the rule most likely to feel bad | Flag `FriendliesBlock` |
| D-018 | What ends a match that never reaches 50? | **Round 30 safety cap**: higher score wins, tie = draw | Protects the harness from infinite games; should almost never fire | Constant |
| D-019 | ADR-0004 lists `Ability/Pass/LastWord/Decline` with a `Reposition` ability slot | Superseded: command kinds now also cover **draft, opening, basic move, basic attack**; no Reposition slot | Movement left the ladder (Movement & Targeting) | ADR-0004 needs an amendment when the command set stabilises |
| D-020 | Draft order | Snake A,B,B,A,A,B,B,A,A,B; mirror picks allowed | Standard and fair; mirrors keep a small roster playable | — |
| D-021 | What can abilities do in the MVP? | Effects: Damage, Heal, Shield, Poison, Displace (push/pull), Dash (self move). Up to two effects per ability | The schema's MVP set minus nothing; `Status` is Poison only | Add effect kinds as content needs them |
| D-022 | Starter roster | 5 champions, one per role, then 10 (two per role) once the engine is stable. Deliberately plain | The roster is where the user's taste matters most; placeholders should be easy to throw away | Replace JSON files in `assets/data/champions/` |
| D-023 | What is "Ready" for a champion in its spawn hex? | It cannot act on the ladder and cannot be targeted until it moves onto the board with a basic | Spawn rows are "outside the game" (Map & Terrain) | — |
| D-024 | Tower threat damage | 2 per enemy within 1 hex, status phase **[measure]** | The user decided towers both damage and score | Constant |
