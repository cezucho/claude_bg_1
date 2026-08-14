# Game Concept: AUGURY (working title)

*Created: 2026-08-14*
*Status: Draft*

> **Working title only.** "AUGURY" is a placeholder carried over from the
> concept-generation phase. Renaming costs nothing at this stage.

---

## Elevator Pitch

> It's a turn-based tactics game where you command all five champions of a MOBA
> team — drafting them, positioning them, and shaping them mid-match — against an
> opponent who can answer every single thing you do before the round ends.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Turn-based tactics / MOBA hybrid |
| **Platform** | PC (Steam / Epic) |
| **Target Audience** | MOBA theorycrafters, chess and tactics players — see Target Player Profile |
| **Player Count** | Single-player vs AI at launch; asynchronous PvP architected for, deferred post-launch |
| **Session Length** | 10–15 minutes per match; 30–60 minute sessions of consecutive matches |
| **Monetization** | Premium (assumed — not yet decided) |
| **Estimated Scope** | Large (18–24 months, solo) |
| **Comparable Titles** | *Frozen Synapse*, *Into the Breach*, *XCOM* (2012+), *League of Legends* |

---

## Core Fantasy

**You out-think an opponent with a team you built, shaped, and commanded yourself.**

In a real MOBA, most of the drama comes from five humans failing to cooperate, and
most of the skill ceiling is mechanical execution — last-hitting, animation
cancelling, reaction time. This game removes both. What is left is the part
players who love MOBAs actually talk about afterward: the draft that set up the
win, the composition that scaled, the moment someone read the engage and answered
it perfectly.

There is no teammate to blame and no execution to fumble. If you lose, you were
out-thought, and you can see exactly where.

---

## Unique Hook

**It's like XCOM with a MOBA draft, and also every action can be answered before
the round closes — because abilities carry an initiative value and both sides
trade down a descending ladder.**

The initiative ladder is the load-bearing invention. Alternating turns normally
mean one side acts freely while the other watches. Here, any ability I play opens
a response window: you may answer with any ability at equal or lower initiative,
from any champion on your board. I may then answer *that*, at equal or lower
initiative again, and so on until someone passes or runs dry.

Three consequences fall out of it:

1. **Engaging is dangerous again.** Committing to a strike invites a counter
   before the round resolves. That is a MOBA teamfight collision, reproduced in a
   turn-based structure without hidden orders.
2. **Every ability gets two design axes.** Power and initiative. A devastating
   ability at high initiative opens a wide answer window; a weak one at initiative
   1 is nearly unanswerable but must wait for the ladder to descend. Champion
   identity emerges from the shape of a kit's initiative curve — the counterpuncher
   who lives at 1, the committer who has to open at 4 and eat the response.
3. **All five champions stay live at all times.** Responses may come from any
   champion, so no unit is ever idle, and the opponent must account for every
   ability still available on the board.

The secondary hook is **molding**: champions arrive from the draft unfinished, and
using an ability applies a small permanent stat change for the rest of the match.
The same five champions become different machines depending on how they were
played. Build-crafting happens through play, not through a shop.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Challenge** (obstacle course, mastery) | **1** | Perfect information means every defeat is legible and therefore teachable; the skill ceiling is entirely cognitive |
| **Expression** (self-expression, creativity) | 2 | Draft composition plus molding paths — the same five champions can be shaped into very different endgame teams |
| **Discovery** (exploration, secrets) | 3 | Emergent synergies between kits, initiative curves, and molding routes; understanding the system *is* the content |
| **Fantasy** (make-believe, role-playing) | 4 | Commanding a team as its single mind — a fantasy MOBAs promise but never deliver |
| **Sensation** (sensory pleasure) | 5 | Deliberately restrained — readability is prioritised over spectacle |
| **Fellowship** (social connection) | 6 | Absent at launch by design; enters only with post-launch PvP |
| **Narrative** (drama, story arc) | N/A | Explicitly excluded (see anti-pillars) |
| **Submission** (relaxation, comfort zone) | N/A | Explicitly excluded — this is a game about concentration |

### Key Dynamics (Emergent player behaviors)

- Players will draft for **initiative curves**, not just for abilities — noticing
  that a team with no low-initiative answers loses every exchange it starts.
- Players will **hold abilities in reserve** as insurance against the ladder,
  creating a bluffing layer with no hidden information in it.
- Players will develop **instinct for molding paths** before they can articulate
  the rule — the intended expression of Pillar 5.
- Players will learn to **read AI archetypes** the way they would read a human,
  which is the same skill PvP will later demand.
- Players will discover that the **opening phase is a chess opening** — a small
  library of standard openings and refutations should emerge from the community.

### Core Mechanics (Systems we build)

1. **Draft** — bans and picks from the full roster, available from the first match.
2. **Opening phase** — each champion uses one ability in a special opening version
   to claim position; that ability starts the tactical phase on cooldown.
3. **Initiative ladder combat** — descending response chain, then end-of-round
   death check, then status resolution.
4. **Molding** — each ability use applies a small permanent stat change for the
   remainder of the match.
5. **Items** — a small, sharp set bought from an automatic per-turn stipend; the
   counter-play lever against what the opponent drafted.

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | The draft, five champions' worth of abilities, and a molding path chosen turn by turn — an enormous decision space with no correct answer | **Core** |
| **Competence** (mastery, skill growth) | Perfect information makes every loss legible; nothing is hidden behind randomness, so improvement is always attributable | **Core** |
| **Relatedness** (connection, belonging) | The weak axis. Mitigated only by champion characterization and AI opponents with enough personality to read as adversaries. Strengthens if PvP ships | **Minimal** |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Killers/Competitors** — *primary*. Beating an opponent's plan is the entire
  reward structure. Ladder of AI archetypes now, human opponents later.
- [x] **Explorers** — *strong secondary*. The system itself is the territory:
  synergies, initiative curves, molding routes, opening theory.
- [ ] **Achievers** — *deliberately weak*. There is no collection and no unlock
  progression; the anti-pillars forbid both.
- [ ] **Socializers** — not served.

### Flow State Design

- **Onboarding curve**: unresolved and important. The initiative ladder is the
  hardest thing to teach and the thing that makes the game good. Likely approach
  is a reduced-champion tutorial that introduces the ladder before the draft, but
  this needs prototyping.
- **Difficulty scaling**: a ladder of AI archetypes with distinct drafting and
  engagement personalities — difficulty grows through *opponent legibility*
  rather than stat inflation.
- **Feedback clarity**: perfect information plus a post-match review of the round's
  ladder, showing which answers were available and unused.
- **Recovery from failure**: a match is 10–15 minutes with no meta-progression to
  lose, so the cost of defeat is time only. Immediate rematch is always correct.

---

## Core Loop

### Moment-to-Moment (30 seconds)

Read the board. Choose one champion's ability and its target, weighing the answer
window that ability's initiative will open. Commit and watch it resolve. Then
decide whether to spend an answer of your own or pass and close the round.

The intrinsic satisfaction is the **exchange** — the ladder descending, each side
spending its best available answer, and the round settling into a position neither
player fully chose.

### Short-Term (5–15 minutes)

One complete match, in three phases: draft, opening, tactical combat. Roughly 25
turns, bounded by the 10–15 minute target. Ends when a team accumulates the
required points.

### Session-Level (30–120 minutes)

Several matches back to back, climbing the AI archetype ladder or replaying a
matchup that beat you with a different draft. Natural stopping point is any
completed match; the reason to return is a composition you want to try.

### Long-Term Progression

**Knowledge, not power.** Nothing unlocks. What grows is champion fluency, matchup
literacy, initiative intuition, item timing, and a personal library of openings.
The player who has been playing three months beats the newcomer with the same
roster and the same items available to both.

### Retention Hooks

- **Curiosity**: unexplored compositions; molding paths not yet tried; an AI
  archetype whose drafting logic you haven't cracked.
- **Investment**: none by design — deliberately forfeited to protect *Mastery,
  Not Power*.
- **Social**: absent at launch; the natural home of a post-launch PvP mode.
- **Mastery**: the primary hook. Concrete evidence of improvement against a ladder
  of opponents that never gets easier on its own.

---

## Game Pillars

### Pillar 1: Chess, Not Dice

No randomness anywhere in the game. Every ability, stat, initiative value, and
position is knowable by both sides at all times. The only uncertainty is what a
mind will choose.

*Design test*: Given a mechanic that resolves with a random roll and one that
resolves through prediction, always take prediction.

### Pillar 2: The Draft Opens, Tactics Decide

The draft is roughly a third of the game. It determines which synergies are
*available* to you — never whether you win.

*Design test*: If a composition can win without correct in-match play, that
composition is too strong and gets changed.

### Pillar 3: Champions Arrive Unfinished

A drafted champion is a set of possibilities, not a finished unit. How you spend
its four abilities shapes its stats, and by the endgame it has become something
specific — chosen by you, not bought from a shop.

*Design test*: If two competent players pilot the same champion into the same
endgame, that champion needs more branching.

### Pillar 4: Five Minds, One Machine

Synergy is positional and timed, never a flat bonus. The team should feel
assembled rather than added up, and every champion should be live at every moment
through the answer it might hold.

*Design test*: If an ability performs the same alone as it does alongside
teammates, redesign it.

### Pillar 5: Small Choices, Felt Not Seen

Individual decisions are subtle and compound quietly. The player develops instinct
before they can articulate the rule.

*Design test*: If a mechanic makes the optimal choice obvious, deepen it. If it
hides information the player is entitled to, cut it.

> **Pillars 1 and 5 are in permanent tension** — total transparency versus
> non-obvious consequence. That tension is what chess has and what most tactics
> games lack, and it is where the real design work will live.

### Anti-Pillars (What This Game Is NOT)

- **NOT a roguelike**: no runs, no permadeath, no escalating gauntlet, no
  meta-progression between matches. This would violate *Mastery, Not Power* and
  pull the project toward a content treadmill it cannot sustain.
- **NOT real-time**: no APM, no reflexes, no execution skill. Mechanical skill is
  the specific thing this game exists to remove from the MOBA formula.
- **NOT a hero-collector**: no gacha, no unlock grind. The entire roster is
  available from the first match, because drafting is meaningless without
  knowledge of the whole pool.
- **NOT a narrative campaign**: no branching story, no cutscenes. Champion
  characterization exists to serve readability, not plot.
- **NOT a live-service**: no battle pass, no seasonal treadmill, no daily quests.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| *League of Legends* | Draft, bans, QWER ability structure, itemization as counter-play, team-fight tension, roles including the jungler | One player commands all five; no farming, no last-hitting, no execution skill; 15 minutes instead of 40 | Validates the enormous appetite for drafting and theorycrafting — an audience badly served by games that require mechanical skill to access it |
| *XCOM* (2012+) | Economy of action — a unit does very little per turn, and depth comes from combination rather than from a long menu | Abilities can be answered mid-round via the initiative ladder; no randomness at all | Proves modern, elegant turn-based tactics can be mass-market readable |
| Chess | Perfect information about capabilities and positions; zero information about intent; blitz clock pressure | Five units acting per side, mutable stats, an economy | The reference model for what "perfect information" means here — explicitly *not* the Into the Breach telegraph model |
| *Frozen Synapse* | Simultaneity of consequence in a turn-based frame | Achieved through the initiative ladder rather than hidden simultaneous orders | Validates that turn-based games can deliver genuine mind-games |
| *Into the Breach* | Clarity of presentation; small maps; tight tactical puzzles | No telegraphed enemy intent — that is the key departure | A partial reference only; the puzzle framing is explicitly not the goal |
| *Legends of Runeterra* | Alternating actions with response windows, proven teachable to a mass audience | Applied to a spatial hex battlefield with persistent units | Precedent that the ladder structure is learnable, not just clever |

**Non-game inspirations**: blitz chess as a spectator format — the drama of
forced, timed decisions under total information.

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 22–40 |
| **Gaming experience** | Mid-core to hardcore |
| **Time availability** | 30–60 minute weeknight sessions; wants a complete competitive experience inside one of them |
| **Platform preference** | PC, mouse and keyboard |
| **Current games they play** | *League of Legends* (often lapsed), *XCOM*, chess, *Into the Breach*, auto-battlers |
| **What they're looking for** | MOBA strategic depth — draft, synergy, itemization, teamfights — without mechanical execution, without teammates, and without a 40-minute commitment per match |
| **What would turn them away** | Randomness in outcomes; grinding or farming; gacha or unlock gates; matches that run long; a losing position that cannot be contested |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | **Godot 4.6** — already pinned in `docs/engine-reference/godot/`. Turn-based tactics with dense UI is squarely in its comfort zone; no physics or rendering demands that would favour Unity or Unreal; user has existing Godot experience |
| **Key Technical Challenges** | AI that plays the initiative ladder competently (see Technical Risks); fully deterministic resolution; UI dense enough to expose all state Pillar 1 requires while staying readable; hex grid pathfinding and range/area-of-effect templates |
| **Art Style** | 3D stylized, low-poly — see Visual Identity Anchor |
| **Art Pipeline Complexity** | Medium — a champion roster is the bulk of the art cost, and low-poly with silhouette-first design keeps per-champion cost bounded |
| **Audio Needs** | Moderate — clear ability and impact feedback matters more than a score |
| **Networking** | None at launch. Architecture must keep a clean separation between deterministic game simulation and presentation so asynchronous PvP can be added post-launch without a rewrite |
| **Content Volume** | 8 champions (MVP) → ~16 (target ship); ~20–30 items; 3 maps; content is roster depth, not level count |
| **Procedural Systems** | None |

---

## Visual Identity Anchor

**Direction: Stylized Arena (low-poly 3D).**

**Visual rule**: *Every model exists to be recognized instantly, not admired.*

The 3D is not there to impress. Its job is to recreate the familiar MOBA feeling —
recognisable champion archetypes on a readable battlefield — while staying as
simple as it can be. Simplicity is an explicit design goal here, not a budget
compromise.

**Supporting principles**:

1. **Readability outranks fidelity.** *Design test*: if an effect obscures board
   state for even a moment, dim it or cut it. Pillar 1 promises the player
   complete information; a VFX that hides a health bar breaks that promise.
2. **Silhouette-first champion design.** *Design test*: if two champions are
   confusable at gameplay zoom in monochrome, one of them gets redesigned. With
   ten units on screen and a ladder to evaluate, identification must be instant.
3. **Borrow the MOBA visual vocabulary.** *Design test*: when inventing a visual
   language for a status, range indicator, or ability type, prefer the convention
   a League player already knows over an original one. Familiarity buys onboarding
   time that the initiative ladder desperately needs.

**Colour philosophy**: two saturated allegiance colours carry team identity and
nothing else does. The arena itself stays desaturated so that units, ability
telegraphs, and range indicators own all the saturation on screen. Initiative
values and molding state read through consistent hue coding rather than an
accumulation of icons.

---

## Risks and Open Questions

### Design Risks

- **Snowballing.** Scaling respawn timers plus a points race plus a 15-minute
  match is a snowball waiting to happen: get ahead, hold objectives, kill them
  again while their respawns lengthen. MOBAs survive this with comeback mechanics
  and 40 minutes of runway; this game has neither. Likely mitigations are catch-up
  income or scaled points for the trailing team, but this is unsolved.
- **Molding legibility.** Pillars 1 and 5 collide here. Stat drift small enough to
  be "felt not seen" may simply register as noise, leaving players unable to
  attribute outcomes to their molding choices — which would break Pillar 1's
  promise that every defeat is legible.
- **Ladder pacing.** If a round's exchange runs long, matches will not fit inside
  15 minutes and the moment-to-moment loop will drag.
- **Onboarding cliff.** The initiative ladder is the best thing in the design and
  the hardest thing to teach. A player who does not understand answer windows will
  experience the game as arbitrary.

### Technical Risks

- **AI competence — the highest-severity risk in the project.** Single-player vs
  AI is a mandatory shipping requirement, so the AI must draft, mold, position,
  and play the ladder well enough to be worth beating. The descending ladder helps
  enormously — each response narrows the legal action set, so the search space
  shrinks as the chain deepens — but a competent five-champion tactical AI remains
  the single thing most likely to determine whether the game is good. Prototype
  early, not late.
- **Determinism.** Pillar 1 requires bit-exact reproducible resolution, which is
  also a precondition for asynchronous PvP later. Retrofitting determinism is
  expensive; it has to be an architectural constraint from the first line of code.
- **Resolution edge cases.** Simultaneous displacement, abilities resolving on
  units that have already acted, and the interaction between the delayed death
  check and the status phase all need airtight, legible rules.

### Market Risks

- Comparable titles (*Frozen Synapse*, *Into the Breach*, *Bad North*) were cult
  and critical successes in the tens of thousands of copies, not breakouts. This
  is a niche premium PC title and success should be defined accordingly.
- The pitch requires explanation. "MOBA without the MOBA parts" is a positioning
  problem in a storefront where attention is measured in seconds.
- Turn-based tactics is a crowded shelf; the initiative ladder is the only thing
  that distinguishes this game at a glance, and it is not visible in a screenshot.

### Scope Risks

- **Roster balance grows quadratically.** Every champion must be balanced against
  every other champion, in both draft and ladder terms. 16 champions is 120
  pairings; the temptation to keep adding champions must be resisted.
- **Two economies to balance.** Molding and items both shape champion power, and
  interactions between them multiply the tuning surface.
- Three phases (draft, opening, tactical) are effectively three games with three
  UIs, three tutorials, and three balance problems.

### Open Questions

Each of these is deliberately unanswered here and belongs in a system GDD via
`/design-system`, or in the prototype:

- **Action economy per round** — what limits how many abilities a champion or a
  team may spend in one ladder? The likely candidate is XCOM's discipline (each
  champion acts once per round, capping a ladder at ten resolutions), but this
  directly determines whether matches fit the 15-minute target and must be settled
  in the prototype.
- **Movement's relationship to the ladder** — is movement an action with an
  initiative value, a separate phase, or free?
- **Equal-initiative exchanges** — an answer may match the previous initiative
  rather than undercut it; what prevents ping-pong at initiative 1?
- **Negative HP and the dying round** — what debuffs apply to a champion at or
  below zero HP, and can healing rescue them before the next death check?
- **Map composition** — lanes and minions are unresolved. Current recommendation
  is to cut minions entirely (they exist to be farmed, and farming is gone) while
  keeping lane-shaped corridors as terrain, with a jungle between them holding
  neutral powers worth contesting.
- **Points economy** — what awards points, at what rate, and what the target total
  is.
- **Gold** — proposal is an automatic equal per-turn stipend plus bonuses for kills
  and held ground, so no one can out-earn an opponent through mechanical skill.

---

## MVP Definition

**Core hypothesis**: *The initiative ladder produces MOBA-like teamfight tension
inside a turn-based game, and molding makes the same five champions play
differently from match to match.*

Both halves must hold. If the ladder is tense but molding is invisible, the game
is a good tactics game without its second hook. If molding is expressive but the
ladder is fiddly, the core is wrong.

**Required for MVP**:

1. Initiative ladder combat — descending response chain with an enforced action
   economy
2. End-of-round death check followed by a status phase, including the dying round
3. Molding — abilities applying permanent in-match stat changes
4. Draft and the opening phase
5. 8 champions with deliberately contrasting initiative curves
6. 1 map, points-based win condition
7. 1 AI archetype, good enough to be worth beating

**Explicitly NOT in MVP**:

- Items (the second economy — validate one before adding two)
- Jungle powers and multiple maps
- Multiple AI personalities
- Any form of PvP
- Audio and visual polish beyond readability
- Meta-progression of any kind (forbidden by anti-pillar, permanently)

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 8 champions, 1 map, 1 AI archetype | Ladder, molding, draft, opening phase, points win | Months 1–4 |
| **Vertical Slice** | 8 champions, 1 polished map, 2 AI archetypes | MVP + items + jungle powers, full UI, readable art | Months 5–9 |
| **Alpha** | ~16 champions, 3 maps, several AI archetypes | All systems present, rough balance, tutorial | Months 10–15 |
| **Full Vision** | ~16 champions, 3 maps, AI ladder | Balanced, taught, polished, shippable | Months 16–24 |

Post-launch, in priority order: asynchronous PvP, roster expansion, replays.

---

## Next Steps

- [ ] Fill in CLAUDE.md technology stack based on engine choice (`/setup-engine` — Godot 4.6)
- [ ] Create the visual identity specification (`/art-bible`, seeded by the Visual Identity Anchor above)
- [ ] Validate this document (`/design-review design/gdd/game-concept.md`)
- [ ] Decompose concept into systems (`/map-systems`)
- [ ] Author per-system GDDs (`/design-system`), starting with initiative ladder combat
- [ ] Create first architecture decision records (`/architecture-decision`) — determinism and simulation/presentation separation first
- [ ] Prototype the initiative ladder and the AI that plays it (`/prototype`)
- [ ] Validate core loop with playtest (`/playtest-report`)
- [ ] Plan first milestone (`/sprint-plan new`)
