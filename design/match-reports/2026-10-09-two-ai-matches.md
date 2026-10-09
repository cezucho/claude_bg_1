# Two AI Matches, Told Round by Round

> **Date**: 2026-10-09 · **Build**: 15 champions, synergy as a champion trait (D-056), payoffs ×2
> with a floor of +3, roster balanced to 43–55% (D-057), Field 7 with solid towers, combo-playing AI.
> **Source**: `tools/Augury.Tools chronicle 12 draft=great-decent` and `chronicle 14 …`. The
> chronicles and full play-by-play logs are in `production/qa/evidence/v2/`.
> **How to read it**: the nexus score is written **A–B**, and the higher number is ahead — it is
> the HP left. Each nexus starts at 60. Every round close, each tower a team holds takes 1 off the
> enemy nexus, and every death costs the dead champion's own nexus 3.

In both matches team A drafted for the most synergy pairs it could get ("great"), and team B
drafted toward one or two pairs ("decent").

---

## Match 1 — the great draft falls behind, then grinds it back (seed 12)

### The drafts

| | Team A — "great", 5 pairs | Team B — "decent", 2 pairs |
|---|---|---|
| Top | Bulwark | Anchor |
| Jungle | Ember | Viper |
| Mid | Tempest | Pyre |
| Bottom | Mortar | Ranger |
| Support | Bastion | Oriel |
| Plan | **Fire and walls.** Ember sets enemies alight for Mortar's artillery (Wildfire). Bulwark's and Mortar's walls give Tempest's Gust something to slam into (Crush). Tempest and Bastion exhaust for Ember (Smother) | **Pin and dive.** Anchor and Ranger root a target for Viper (Lockdown). Pyre adds fire with no partner to cash it in. Oriel keeps everyone alive |
| Spells | Teleport, Smite, Ignite, Heal, Exhaust | the same five |

Both AIs took the same spell for the same role. The AI's spell preferences are fixed per role
(D-046), so every match looks like this. That is worth noting: with real players, spells would
be a second, smaller draft.

### Opening — trading statuses, no damage

Ember opened by setting Anchor alight, and Tempest's three-cast opening chipped Anchor too.
Viper lunged in and poisoned Ember. Pyre and Ranger then burned Ember on the way back.
Bastion's opening Judgement sweep landed. Ember and Mortar both hit the fallback and only
stepped one hex.

Nobody lost much (129 HP against 130), but both front lines entered round 1 burning.
**Even at 60–60**, and with every tower still in its owner's hands.

### Rounds 1–3 — the first blood, and a double kill

| Round | Score A–B | What happened |
|---|---|---|
| 1 | 58–58 | Tempest **Ignites** Ranger, and Mortar's first **payoff** lands on the burning Ranger. Viper **Smites** A's home tower to soften it |
| 2 | 53–56 | Pyre catches Ember: **first blood to B**. Mortar keeps hitting burning targets, three payoffs in two rounds. Tempest's Gust **slams** Viper into a wall |
| 3 | 48–48 | B's plan finally works: Anchor roots Mortar, and **Viper's payoff** hits it twice (+3, +5). Ranger kills Mortar. Tempest answers by **killing both Viper and Pyre** |

The first three rounds trade evenly: 2 kills for each side, and no tower has changed hands. Each
team's synergy fired. Wildfire did so every round. Lockdown fired once, decisively.

### Rounds 4–9 — B takes the lead, and towers start to move

| Round | Score A–B | What happened |
|---|---|---|
| 4 | 46–46 | Bulwark **Teleports** forward, and Ember Smites B's home tower |
| 5 | 43–44 | **The first captures**, both in one round: A takes the **centre tower**, B takes **A's home tower** |
| 6 | 40–42 | Mortar burns Anchor down with two payoffs, but Oriel and Ranger heal B back up (13 healing) |
| 7 | 31–37 | **B's best round.** Ranger kills Ember *and* Tempest; Mortar takes Viper back. A loses 9 nexus HP in one round. **B leads by 6** |
| 8 | 29–34 | A answers on the map instead: it takes **B's home tower** while B is regrouping |
| 9 | 27–28 | Towers trade again: B takes the centre back, A retakes its own home tower. A also lands a **direct hit on B's nexus**, now open. Back to even |

This is the middle of the match, and the momentum changes hands three times in five rounds.
- Kills swing the score fast: each death costs 3. But kills usually come in trades, so a round's
  net swing is rarely more than 4–7.
- Towers swing it slowly but for good: a tower held is 1 HP every round until it's lost.
- B's lead from the round-7 double kill was gone two rounds later. It wasn't defended; A spent
  those rounds on towers.

### Rounds 10–15 — the great draft closes it out

| Round | Score A–B | What happened |
|---|---|---|
| 10 | 25–19 | **A's best round.** Mortar's payoffs on the burning Anchor (+3, +8) finish it; Tempest kills Ranger. A leads by 6 |
| 11 | 20–16 | Pyre picks off Tempest |
| 12 | 14–11 | A hits the open nexus again; B retakes its home tower and kills Bulwark |
| 13 | 11–9 | Quiet round. B heals up to 97 HP on the board, against A's 26, but can't turn it into towers |
| 14 | 6–3 | A takes the **centre tower** again, and now holds **3 towers to B's 2**. Each side trades a kill |
| 15 | 4–0 | The round closes, and three towers' siege finishes B's nexus. **A wins** |

**The decisive factor was the centre tower, not the fights.** From round 14 A held three
towers. At round close that meant 3 HP off B's nexus against 2 off A's, every round, with
both nexuses already low.

At the end B had more HP on the board (78 against 68) and had won as many fights. It lost the
race for the third tower.

### Momentum, at a glance

A's lead in nexus HP (A minus B) at the end of each round:

| Opening | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 0 | −3 | 0 | 0 | −1 | −2 | **−6** | −5 | −1 | **+6** | +4 | +3 | +2 | +3 | +4 |

- **B led for most of the middle**, rounds 5–9. Its lead peaked at 6 after the round-7 double
  kill.
- **A took over in two rounds.** It took B's home tower in round 8, retook its own in round 9,
  then won the fight in round 10: a **7-point swing** from −1 to +6, the biggest of the match.
- **After that A never led by more than 6, nor by less than 2.** The match stayed close until
  the siege finished it.

---

## Match 2 — the "best" draft on paper is stomped (seed 14)

| | Team A — "great", 5 pairs | Team B — "decent", 2 pairs |
|---|---|---|
| Lineup | Anchor, Talon, Lens, Gunner, Seer | Briar, Ember, Pyre, Mortar, Oriel |
| Plan | **Marks everywhere.** Lens and Seer mark for Anchor and Talon (4 pairs); Anchor roots for Gunner | **Fire.** Ember and Pyre burn; Mortar cashes in on everything burning; Oriel heals |

**What happened:**
- **A's marks paid off three times in five rounds.** Each mark is used up by the first hit from
  anyone, and A's two mark-wanters were rarely first.
- **B's fire paid off nine times in the same five rounds.**
  - Ember and Pyre opened by burning Anchor twice.
  - From then on every A champion was burning: Anchor, Gunner, Talon, Lens and Seer.
  - Mortar hit each of them for extra.
  - Burn lasts two rounds, so one cast feeds Mortar for a long time.
- **Rounds 4–5 decided it.** Briar killed Talon and Lens, and Mortar killed Anchor and Seer.
  A was down to 8 HP on the board at 34–43.
- **Round 6 opened A's nexus.** B already held the centre (since round 1); now it took A's home
  tower. A had briefly taken one of B's home towers in round 5, but that couldn't stop it.
- **Rounds 7–9 were a direct assault.** B hit the open nexus for 10, then 11 HP a round.
  - In round 8 A fought back: Lens killed Ember and Mortar, and A took the centre tower.
  - It was too late. **B won 30–0 in round 9.**
- A never led at any point. A nexus attacked directly drains faster than siege or kills.

**The lesson:** five pairs lost to two, because the two were the right two. Wildfire is the
strongest plan in the game right now, and Called Shot the weakest.

---

## What the two matches say about the game

1. **Momentum changes in two ways.**
   - Fights move it fast: each death costs 3. Kills usually come in trades, so a round's net swing
     is at most about 7.
   - Towers move it slowly: one point a round, but every round.
   - An **open nexus** is the third way, and the fastest: once a team loses a home tower, the
     enemy can hit the nexus directly for 10 or more a round (Match 2, rounds 7–9).

   Leads from fights evaporate within two or three rounds unless they're turned into towers.
2. **Towers decide matches.**
   - Match 1 was decided by who held three towers at the end.
   - Match 2 was decided by losing a home tower and with it the nexus.
   - Most matches end by siege (about 80% in self-play), and the rest by direct attacks on an
     open nexus.
3. **The draft matters through *which* plan, more than *how many* pairs.**
   - Burn lasts two rounds and spreads to many targets, so Wildfire pays off constantly.
   - Marks are used up by the first hit, and roots last a round, so their plans fire rarely.
4. **Spells are everywhere.** Smite opens towers, Ignite feeds anyone's burn plan, and Heal and
   Exhaust swing fights. Ignite gives every team access to burn, which helps explain why Wildfire
   is so strong.
5. **A match runs about 15 rounds**, about 13 on average in self-play. Both nexuses lose 2–3 HP
   a round to siege, so the end is always a race.
