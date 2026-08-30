# Systems Index: AUGURY (working title)

> **Status**: Draft
> **Created**: 2026-08-14
> **Last Updated**: 2026-08-14
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

AUGURY is a turn-based tactics game with MOBA structure: one player commands five
drafted champions on a hex map, and every ability carries an **initiative** value
that governs when it may be played and what may answer it. The mechanical scope is
therefore dominated by a single deep system — the initiative ladder — surrounded by
the systems that feed it (draft, opening phase, molding, items) and the systems
that make it legible (HUD, ladder UI, resolution playback).

Two properties of the concept shape the whole decomposition. First, **Pillar 1
(Chess, Not Dice)** forbids randomness and demands that all state be visible, which
pushes an unusual amount of weight into presentation: the simulation is simple to
state and hard to *show*. Second, **single-player vs AI is a mandatory shipping
requirement**, which makes the AI opponent a first-class system rather than an
afterthought — and makes the action economy a decision with direct consequences for
AI tractability.

The project has no content treadmill. There is no meta-progression, no unlockable
roster, and no narrative. The roster *is* the content, so the systems that gate
content velocity — Ability Definition Schema and Content Authoring Pipeline — carry
more leverage than their size suggests.

---

## Systems Enumeration

Systems marked **(inferred)** were not named in the concept document; they were
derived from what the named systems require.

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Hex Grid & Spatial Model (inferred) | Core | MVP | Not Started | — *(ADR, not GDD)* | — |
| 2 | Deterministic Simulation Core (inferred) | Core | MVP | Not Started | — *(ADR, not GDD)* | — |
| 3 | Round Phase Sequencer (inferred) | Core | MVP | Not Started | — *(ADR, not GDD)* | Deterministic Simulation Core |
| 4 | Champion Data & Stat Model (inferred) | Core | MVP | **Revised** (pending review) — unparked 2026-08-17 | [design/gdd/champion-and-ability-schema.md](champion-and-ability-schema.md) | Deterministic Simulation Core, Map & Terrain, Movement & Targeting, Sigils & Beacons |
| 5 | Ability Definition Schema (inferred) | Core | MVP | **Revised** (pending review) — unparked 2026-08-17 | [design/gdd/champion-and-ability-schema.md](champion-and-ability-schema.md) | Hex Grid, Champion Data, Movement & Targeting, Map & Terrain, Sigils & Beacons |
| 6 | Movement & Targeting | Gameplay | MVP | **Drafted** (pending review) | [design/gdd/movement-and-targeting.md](movement-and-targeting.md) | Hex Grid, Map & Terrain, Initiative Ladder, Champion Data |
| 7 | Damage & Combat Resolution (inferred) | Gameplay | MVP | Not Started | — | Champion Data & Stat Model |
| 8 | Initiative Ladder & Action Economy | Gameplay | MVP | **Designed** (pending review) | [design/gdd/initiative-ladder.md](initiative-ladder.md) | Ability Definition Schema, Round Phase Sequencer · *modified by* **Sigils & Beacons** |
| 9 | Status Effects | Gameplay | MVP | Not Started | — | Ability Definition Schema, Round Phase Sequencer, Damage |
| 10 | Death, Dying Round & Respawn | Gameplay | MVP | Not Started | — | Round Phase Sequencer, Damage, Status Effects |
| 11 | Molding | Gameplay | MVP | Not Started | — | Champion Data & Stat Model, Ability Definition Schema |
| 12 | Map & Terrain | Gameplay | MVP | **Designed** (pending review) | [design/gdd/map-and-terrain.md](map-and-terrain.md) | Hex Grid |
| 13 | Objectives & Scoring | Gameplay | MVP | Not Started | — | Map & Terrain, **Sigils & Beacons** (beacon destruction cost) |
| 14 | Draft | Gameplay | MVP | Not Started | — | Champion Data, Ability Definition Schema |
| 15 | Opening Phase | Gameplay | MVP | **Drafted** (pending review) | [design/gdd/opening-phase.md](opening-phase.md) | Draft, Champion & Ability Schema, Map & Terrain, Movement & Targeting, Sigils & Beacons |
| 16 | AI Opponent | Gameplay | MVP | Not Started | — | Effectively the entire simulation |
| 17 | Board & Unit Presentation | UI | MVP | Not Started | — | Hex Grid, Map & Terrain, Champion Data |
| 18 | Combat HUD & State Inspection | UI | MVP | Not Started | — | Champion Data, Molding, Status Effects |
| 19 | Initiative Ladder UI (inferred) | UI | MVP | Not Started | — | Initiative Ladder, Combat HUD |
| 20 | Resolution Playback (inferred) | UI | MVP | Not Started | — | Initiative Ladder, Board Presentation, Status Effects |
| 21 | Draft UI | UI | MVP | Not Started | — | Draft |
| 22 | Economy & Items | Economy | Vertical Slice | Not Started | — | Champion Data, Objectives & Scoring |
| 23 | Jungle & Neutral Powers | Gameplay | Vertical Slice | Not Started | — | Map & Terrain, Status Effects |
| 24 | Blitz Clock | Core | Vertical Slice | Not Started | — | Deterministic Simulation Core |
| 25 | Shop UI | UI | Vertical Slice | Not Started | — | Economy & Items |
| 26 | Post-Match Review (inferred) | UI | Vertical Slice | Not Started | — | Resolution Playback, Deterministic Simulation Core |
| 27 | Content Authoring Pipeline (inferred) | Meta | Vertical Slice | Not Started | — | Ability Definition Schema, Champion Data |
| 28 | Balance Simulation Harness (inferred) | Meta | Vertical Slice | Not Started | — | AI Opponent, full simulation |
| 29 | Audio | Audio | Vertical Slice | Not Started | — | Resolution Playback |
| 30 | **Sigils & Beacons** (combo system) | Gameplay | MVP | **Drafted** (pending review) | [design/gdd/sigils-and-beacons.md](sigils-and-beacons.md) | Initiative Ladder, Ability Definition Schema, Map & Terrain, Opening Phase |
| 30 | Tutorial & Onboarding | Meta | Alpha | Not Started | — | Nearly everything |
| 31 | Persistence & Settings (inferred) | Persistence | Alpha | Not Started | — | Deterministic Simulation Core |
| 32 | Accessibility (inferred) | Meta | Alpha | Not Started | — | All UI systems |
| 33 | Asynchronous PvP | Meta | Full Vision | Not Started | — | Deterministic Simulation Core, entire simulation |

> **Design-owned vs architecture-owned.** Systems 1, 2, and 3 are technical
> foundations, not design problems. They are specified through ADRs produced by
> `/create-architecture` and `/architecture-decision`, not through `/design-system`.
> Spending design sessions on them would answer the wrong kind of question.

---

## Categories

| Category | Description | Systems in This Project |
|----------|-------------|-------------------------|
| **Core** | Foundation systems everything depends on | Hex Grid, Deterministic Simulation Core, Round Phase Sequencer, Champion Data & Stat Model, Ability Definition Schema, Blitz Clock |
| **Gameplay** | The systems that make the game fun | Initiative Ladder, Movement & Targeting, Damage, Status Effects, Death/Respawn, Molding, Map & Terrain, Objectives & Scoring, Draft, Opening Phase, Jungle, AI Opponent |
| **Economy** | Resource creation and consumption | Economy & Items |
| **Persistence** | Save state and continuity | Persistence & Settings |
| **UI** | Player-facing information displays | Board Presentation, Combat HUD, Initiative Ladder UI, Resolution Playback, Draft UI, Shop UI, Post-Match Review |
| **Audio** | Sound and music systems | Audio |
| **Meta** | Systems outside the core game loop | Tutorial & Onboarding, Accessibility, Content Authoring Pipeline, Balance Simulation Harness, Asynchronous PvP |

> **Removed categories**: *Progression* and *Narrative* are absent by design. The
> anti-pillars forbid meta-progression and narrative campaigns — see
> `design/gdd/game-concept.md`. Their absence here is deliberate, not an oversight.

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function. Without these you cannot test "is this fun?" | First playable prototype (months 1–4) | Design FIRST |
| **Vertical Slice** | Required for one complete, polished experience | Vertical slice (months 5–9) | Design SECOND |
| **Alpha** | All features present in rough form | Alpha (months 10–15) | Design THIRD |
| **Full Vision** | Polish, edge cases, post-launch features | Release (months 16–24) | Design as needed |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Hex Grid & Spatial Model** — pure spatial math. Every positional rule in the
   game resolves through it, and nothing it depends on exists.
2. **Deterministic Simulation Core** — fixed-point math, state container, round
   state machine, replay serialization. Pillar 1 and post-launch PvP both rest here.

### Core Layer (depends on foundation)

> **Reordered 2026-08-16.** Map & Terrain moved ahead of the stat model and the ability
> schema, out of the Feature layer. It was placed late because it depends on little;
> that was a mistake, because almost everything else depends on *it*. Board size sets
> champion density, density sets pattern applicability, and applicability is what prices
> ability power (ladder F4). Reach, movement speed and ability range are all measured in
> hexes, and a hex means nothing until the board has a size. Designing the stat model
> first produced numbers that silently encoded a radius-4 board nobody had chosen.

1. **Map & Terrain** — depends on: Hex Grid. **Design this first.** Board shape and
   radius, symmetry, tower/objective placement, jungle regions, terrain types
2. **Movement & Targeting** — depends on: Hex Grid, Map & Terrain, Deterministic Simulation Core
3. **Champion Data & Stat Model** — depends on: Deterministic Simulation Core, Map & Terrain
4. **Damage & Combat Resolution** — depends on: Champion Data & Stat Model
5. **Ability Definition Schema** — depends on: Hex Grid, Map & Terrain, Champion Data, Movement & Targeting
6. **Round Phase Sequencer** — depends on: Deterministic Simulation Core
7. **Blitz Clock** — depends on: Deterministic Simulation Core

### Feature Layer (depends on core)

1. **Initiative Ladder & Action Economy** — depends on: Ability Definition Schema, Round Phase Sequencer
2. **Status Effects** — depends on: Ability Definition Schema, Round Phase Sequencer, Damage
3. **Death, Dying Round & Respawn** — depends on: Round Phase Sequencer, Damage, Status Effects
4. **Molding** — depends on: Champion Data & Stat Model, Ability Definition Schema
5. **Objectives & Scoring** — depends on: Map & Terrain
6. **Jungle & Neutral Powers** — depends on: Map & Terrain, Status Effects
8. **Economy & Items** — depends on: Champion Data & Stat Model, Objectives & Scoring
9. **Draft** — depends on: Champion Data & Stat Model, Ability Definition Schema
10. **Opening Phase** — depends on: Draft, Ability Schema, Movement & Targeting, Map & Terrain
11. **AI Opponent** — depends on: effectively the entire simulation

### Presentation Layer (depends on features)

1. **Board & Unit Presentation** — depends on: Hex Grid, Map & Terrain, Champion Data
2. **Resolution Playback** — depends on: Initiative Ladder, Board Presentation, Status Effects
3. **Combat HUD & State Inspection** — depends on: Champion Data, Molding, Status Effects, Economy
4. **Initiative Ladder UI** — depends on: Initiative Ladder, Combat HUD
5. **Draft UI** — depends on: Draft
6. **Shop UI** — depends on: Economy & Items
7. **Post-Match Review** — depends on: Resolution Playback, Deterministic Simulation Core

### Polish Layer (depends on everything)

1. **Tutorial & Onboarding** — depends on: nearly every system
2. **Audio** — depends on: Resolution Playback
3. **Accessibility** — depends on: all UI systems
4. **Persistence & Settings** — depends on: Deterministic Simulation Core
5. **Content Authoring Pipeline** — depends on: Ability Definition Schema, Champion Data
6. **Balance Simulation Harness** — depends on: AI Opponent, full simulation
7. **Asynchronous PvP** — depends on: Deterministic Simulation Core, entire simulation

### Bottleneck Systems

Ordered by how much depends on them. All are cheap to specify now and expensive to
change later:

1. **Deterministic Simulation Core** — everything
2. **Ability Definition Schema** — ladder, molding, statuses, draft, opening phase, AI, content pipeline. Gates content velocity for the whole project
3. **Hex Grid & Spatial Model** — all spatial systems
4. **Champion Data & Stat Model** — molding, items, damage, HUD
5. **Round Phase Sequencer** — ladder, statuses, death and respawn

---

## Recommended Design Order

Only design-owned systems appear here. Architecture-owned systems (Hex Grid,
Deterministic Simulation Core, Round Phase Sequencer) are specified via ADR in
parallel and should be settled before implementation begins.

> **One deliberate inversion.** Strict dependency order places Ability Definition
> Schema before Initiative Ladder. It is flipped below, because the schema's
> required fields fall out of the ladder's rules — you cannot know what an ability
> record needs until you know how initiative governs play. Designing the schema
> first would mean guessing at it and rewriting it.

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Initiative Ladder & Action Economy | MVP | Feature | game-designer, systems-designer | L |
| 2 | Champion Data & Stat Model + Ability Definition Schema | MVP | Core | systems-designer | M |
| 3 | Molding | MVP | Feature | systems-designer | M |
| 4 | Damage & Combat Resolution | MVP | Core | systems-designer | S |
| 5 | Status Effects | MVP | Feature | systems-designer | M |
| 6 | Death, Dying Round & Respawn | MVP | Feature | systems-designer | S |
| 7 | Movement & Targeting | MVP | Core | game-designer, level-designer | M |
| 8 | Map, Terrain & Objectives | MVP | Feature | level-designer, game-designer | M |
| 9 | Draft | MVP | Feature | game-designer | M |
| 10 | Opening Phase | MVP | Feature | game-designer | M |
| 11 | AI Opponent | MVP | Feature | ai-programmer, game-designer | L |
| 12 | Combat HUD & Initiative Ladder UI | MVP | Presentation | ux-designer | L |
| 13 | Resolution Playback | MVP | Presentation | ux-designer, technical-artist | M |
| 14 | Board & Unit Presentation | MVP | Presentation | art-director | M |
| 15 | Draft UI | MVP | Presentation | ux-designer | S |
| 16 | Economy & Items | Vertical Slice | Feature | economy-designer | L |
| 17 | Jungle & Neutral Powers | Vertical Slice | Feature | game-designer | M |
| 18 | Blitz Clock | Vertical Slice | Core | game-designer | S |
| 19 | Post-Match Review | Vertical Slice | Presentation | ux-designer | M |
| 20 | Balance Simulation Harness | Vertical Slice | Meta | tools-programmer, economy-designer | M |

Effort estimates: S = 1 session, M = 2–3 sessions, L = 4+ sessions.

---

## Circular Dependencies

- **Action Economy ↔ Initiative Ladder Resolution — RESOLVED BY MERGING.** The
  ladder's legality rule (an answer must be at initiative ≤ the previous ability)
  and the action economy (how many abilities a champion or team may spend in one
  round) define each other: the ladder's termination behaviour *is* the action
  economy. Authored as a single GDD. Splitting them would produce two documents
  that each defer the hard question to the other.

No other cycles found.

### Design-Time Feedback Loop (not a code dependency)

The AI depends on the ladder, but **the action economy determines whether the AI is
tractable at all**. A ladder capped at roughly ten resolutions per round is
searchable; an uncapped one may not be. The AI's search cost must therefore be an
input to the Action Economy design session, not a consequence discovered afterward.
Concretely: estimate the branching factor *before* committing to an action economy.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Initiative Ladder & Action Economy | Design | The entire game rests on an untested combination of mechanics. If the exchange is fiddly rather than tense, nothing downstream can save it | Prototype before the GDD is finalised — the design should be informed by a playable version, not the reverse |
| AI Opponent | Technical (highest severity) | Single-player is a mandatory shipping requirement, and a five-champion tactical AI over a ladder is genuinely hard. Search cost is set by a decision made in another system | Prototype alongside the ladder. Pursue readable archetypes over optimal play — an opponent you learn to predict is both more tractable and better design |
| Deterministic Simulation Core | Technical | Retrofitting determinism is expensive and gates asynchronous PvP permanently | ADR before the first line of code; determinism tests (identical inputs → byte-identical outputs) from day one |
| Molding | Design | Pillars 1 and 5 collide here. Stat drift small enough to be "felt not seen" may simply register as noise, leaving players unable to attribute outcomes to their choices | Prototype with exaggerated values, then tune down until it is barely felt but still attributable |
| Economy & Items | Balance | Scaling respawns + a points race + a 15-minute match is a snowball waiting to happen, with no runway to recover | Balance Simulation Harness; design catch-up mechanisms deliberately rather than patching them in later |
| Initiative Ladder UI | Design | The onboarding cliff. A player who does not understand answer windows experiences the game as arbitrary | Usability-test with someone who has never seen the game, early |
| Resolution Playback | Design | The simulation resolves a chained exchange instantly; if the player cannot follow it, Pillar 1's promise of legibility fails in presentation rather than in rules | Prototype playback pacing alongside the ladder prototype |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 33 |
| Design docs started | 1 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 1/21 |
| Vertical Slice systems designed | 0/8 |

---

## Next Steps

- [ ] Design MVP-tier systems in the order above (`/design-system initiative-ladder` first)
- [ ] Run `/design-review design/gdd/[system].md` on each completed GDD
- [ ] Produce ADRs for the three architecture-owned systems (`/create-architecture`)
- [ ] Prototype the initiative ladder and its AI early (`/prototype initiative-ladder`)
- [ ] Run `/review-all-gdds` when all MVP GDDs are complete
- [ ] Run `/gate-check pre-production` before committing to production
