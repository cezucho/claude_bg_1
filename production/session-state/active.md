# Active Session State

*Last updated: 2026-11-22*

## Current Task

**PATTERN SKIRMISH — 25-CHAMPION SPREAD DEFAULT + BALANCE — 2026-11-22.** Owner: make spread + 1 move + maneuvers + W loosening (r5/10) + R loosening (lvl 2/4) the default; redo the other 15 champions' abilities; balance; asked whether drafts want different geometries; asked for thoughts on condition-triggered maneuver passives. Done (D-083): all 25 in SPREAD/R_STAGES with shape families; defaults on; spreadTune for 25 (5 passes + average). Measured: best formation ball 96% → 61/59/48%; qFamily switch 49/42/49% (off); 1,000-match check gives win rates 41–56% (Seer 41%), sides 48–53%, ~14 rounds. Next: owner plays; Seer redesign; condition-triggered maneuver passives (proposal given in chat, not built); slower tops.

**PATTERN SKIRMISH — W LOOSENING + MANEUVERS — 2026-11-22.** Owner: W's loosen with time (R by chosen levels); measure movement passives instead of a 2nd move. Built (D-082): `wLoosen` (rounds 5/10), `maneuver` (W/R cast → step or swap next turn; AI plans it; click ally to swap). Measured 4×200: maneuver ≈ second move for patterns (2.37 vs 2.43) but slower (62% vs 74% by nexus); W loosening doubles W casts mid-game; one move + maneuver + W loosening = 70% by nexus, 15.2 rounds, comeback 25%, 47–53%. Defaults unchanged. Next: owner decides defaults; champion-specific movement passives; slower tops.

**PATTERN SKIRMISH — LOOSENING R + BALANCE — 2026-11-22.** Owner: ceiling ~8 OK; keep 2 moves/turn for now (prefers movement passives later; slower tops idea); build R loosening with levels, measure, then balance. Done (D-081): R_STAGES strict → medium (lvl 2) → loose (lvl 4), AI loosen value, UI notes, phase metrics; balance via 3 passes (`spreadTune`). Measured: R casts 2.4/3.1/2.1 by phase, ceilings 7.0/7.8/9.0, win rates 42–58% → 46–54%, 73% by nexus, 14.3 rounds. Next: owner plays; candidates: movement passives replacing the 2nd move, slower tops, easing W, expanding to 25 champions.

**PATTERN SKIRMISH — SPREAD PATTERNS — 2026-11-22.** Owner: a good draft makes a ball that lights nearly everything, so nobody moves; wants exact patterns two hexes out, best shape 5–6 lit, rotation. Built behind `RULES.spread` (draft-screen switch): two-out cells, `free` hexes, `pullTo`, 10 champions with `SPREAD` W/R, `patternAudit()`, shape measures, `movesPerTurn`, `aiProgress` (D-080, README "Spread patterns"). Measured: standard ceiling 12/15; spread first draft 6.9 but R's needing two exact far allies never fire → stall; final (one far ally + empty hex) ceiling 7.8, no dead R, R 10/match, allies beside 0.95 vs 1.27, 82% by nexus with 2 moves/turn, win rates 31–69%. Open for owner: ceiling target vs playability, W harder than R, 2 moves/turn, balance. Artifact not republished.

**PATTERN SKIRMISH — LOOK AND FEEL — 2026-11-21.** Owner: make the UI look like a publishable game, autonomous choice of fonts/colours/images. Done in `prototypes/pattern-skirmish/index.html` (theme layer at the end of the stylesheet, README "Look and feel"): Lilita One + Nunito, bronze/gold panels, painted backdrop, crest, element gems (Nano Banana, `assets/data/art/pattern_ui.json`; generator got a `backdrop` kind), title screen, scoreboard HUD with nexus bars and tactic pips, drawn stat icons, result banner. Screenshots 1500x900 and 1280x720, 4 scripted matches without errors. Not committed. Next: owner looks; candidates: status/chance icons still emoji, sound.

**PATTERN SKIRMISH — SECOND PLAYTEST: Q BACK, CINEMATIC PULSE, TERRAIN, FORMATIONS — 2026-11-21.** Owner played the quieter pulse: 0.8 casts/pulse works (teams group up). Asked for slimmer UI/bigger board, level-up hover-card fix, Q as a real ability again (passive kept), cinematic casts + status feedback, second elements for weak champions, visible river/hills, Easy formation helper. Owner chose: medium Q, supports + no-fit champions get second element, terrain for existing zones only (jungle is only a role), formations computed for the drafted team. Built all in `prototypes/pattern-skirmish/index.html` (README "Second playtest"): `qCast`/`qCd 3`/`QPAT`, `moreEls`/`SECOND_EL` (Oriel+Storm, Brook+Light, Ash+Shadow, Moss+Frost, Shiver+Storm, Pyra+Light, Umbra+Fire), two-beat casts (wind-up zoom + name + chip glow), green poison numbers, flashing status icons, terrain textures river/hillt/pit (`assets/data/art/pattern_terrain.json`, Nano Banana, first try), formations overlay + best opening card (Easy). Measured 840: 1.44 casts/pulse, win rates 43–55%, sides 51%. 12 scripted human matches total, no errors. Not committed. Next: owner plays.

**PATTERN SKIRMISH — A QUIETER PULSE — 2026-11-21.** Owner: Q fires every round for everyone (23/25 Q's = "ally beside", cd 2 = once per round), a pulse is a wall of casts; wants rarer, bigger abilities, passives, end-of-round deaths with finishers, calmer round 1. Owner approved my set and said "go". Built in `prototypes/pattern-skirmish/index.html`, each behind a `RULES` switch: `passives` (Q slot = passive, table `PASSIVE`, 25 passives), `rare` (W/R patterns in `RARE` with extra conditions foes/river/deep/allyHurt, ×2 damage/heal/shield, cd 4/6, cast = caster headline), `deathPhase` (0 HP = broken: stays, half attack, no casts, no damage taken; dies at end of round unless healed; 7 finisher abilities + Viper passive), `openCards: 3` + `openBack` (uncarded champions start beside own nexus), `shieldCap 0.5`. 4 TUNE passes. Measured 840 matches: casts/pulse 3.03 → 0.81, no-cast pulses 7% → 45%, round-1 damage 82 → 62, first kill 1.51 → 1.66, 13.5 broken/match (1.5 saved, 2.6 finished, 11.2 die at check), win rates 45–56%, sides 49–50%. 3 cards alone did nothing (beside-nexus start did). 12 scripted human matches, no errors. Harness: Python Playwright `sim.py`/`par.py` in session scratchpad (not saved in repo). README section "A quieter pulse". Not committed. Artifact not republished. Next: owner plays; open: AI does not play around the death phase; held back: choice passives mid-pulse, range cap 2.

**PATTERN SKIRMISH — TACTIC POINTS + FINAL FIGHT — 2026-11-21.** Owner asked for research on Esports Godfather, then to build its two borrowable ideas. Built: tactic points (+1/round, cap 8; one card per own turn before moving: Siege 2, Dragon/Baron 3, Rest 3, Rush back 2, Final fight 6 with 3+ enemies dead; regroup cards off), final fight after round 18 instead of score. Measured (150/200): default 14.4–14.7 rounds, 47–55% nexus, 19–23% Final by card (won 97% by caster), 25–30% timed final, comeback 20–22%; first kill still round 1.5 (no phases). Owner: Final fight card cut (RULES.finalCard false), phases not pursued. Committed.

**PATTERN SKIRMISH — REGROUP CARDS COUNT THE FORMATION — 2026-11-20.** Owner: first cards traded geometry for an easy choice. Now place first, then the card ends the regroup and counts positions (Siege 3 per champion in range +1 if pattern complete; Rest only beside an ally; Fortify per survivor near the building); live values, hover rings. AI regroup tries one formation per objective. Measured 2×200: 14.6–14.7 rounds, 69–72% by nexus (flat cards 80%, none 62%), comeback 19–24%. Owner: slower pace accepted for now; Fortify cut (after cut: 14.6 rounds, 70% by nexus, comeback 19%). Not committed. Artifact NOT republished (owner said no).

**PATTERN SKIRMISH — READABILITY, PATHS, REGROUP CARDS, COACH — 2026-11-20.** Owner played and asked for: draft with all portraits + hover + broadcast ban/pick slots; Q W R visible behind sprites/towers; AI vs AI stops at the end; visible AI moves; MARLOW tips with a difficulty setting (Easy: draft highlights, hints without the answer); regroup that shows what changes; growth: two paths per champion, then one champion grows at a time with icons. Built all in `prototypes/pattern-skirmish/index.html` (see README section "Readability, paths, regroup cards, coach"). Regroup = objective cards (Siege / Dragon / Baron / Rest; losers Rush back / Fortify / Rest). Measured 3×200: 13.4–13.7 rounds, 82–86% by nexus, comeback 18–27%, paths Power 52 / Tempo 47 / Striker 51 / Warden 48. Easy AI weaker (37% vs ~45%); deeper search not stronger, so Hard = Normal AI without coach. Bug fixed: Done during card animation froze the match. Not committed. Next: owner plays; open: real Hard AI, Fortify/monster cards unused.

**PATTERN SKIRMISH — BANS — 2026-11-19.** Owner asked for bans in the draft and how they change element drafting. Built `RULES.bans` (`none`, `1`, `2`, `1+1`, `2+2`; switch on the draft screen; max 2 bans per role; AI bans by element fit). Measured 5 schemes × 2000 matches: all-Frost teams 14% → 0% with 1+1; win-rate distance from 50% 6.3 → 3.8 (2.7 with 2+2); pattern drafter vs random stays ~57%; W casts 21.9 → 18.9 (17.3 with 2+2). Default set to 1+1. 30 scripted matches, no errors. Artifact republished. Not committed. Next: owner plays; walls/terrain experiment still pending; AI bans weak champions (Flint) because it bans by fit, not strength.

**PATTERN SKIRMISH — GEOMETRY ABILITIES + STAGGERED START — 2026-11-19.** Second review first (old vs new game, same harness): control clearly better (one-move AI vs random mover 65% → 96%; dice spread 3.1 → 0.7 against move spread ~3.1), phases not (first kill round 1.3–1.6, flat). Owner: phases may not matter; wants geometry-changing abilities; walls later. Built: push / pull / swap / step-aside / rescue on 13 abilities (all certain), dashed preview arrows, staggered start (Q, then W, then R), 6 rebalance passes. Measured: ~10 forced moves/match (was ~3), control unchanged (95% / 86%, dice 0.7), round-1 damage 49 → 42, 86% end by nexus, comeback 16%, random-draft win rates 45–56%, pattern-drafter 39–62% (Frost on top; Oriel 39%, Flint 40%). 31 scripted matches, no errors. Artifact republished. Not committed. Next: owner plays; then walls/terrain as a separate experiment; Frost group and Oriel/Flint need a look.

**PATTERN SKIRMISH — BALANCE, EASIER PATTERNS, PLANNING AI — 2026-11-19.** Owner asked for all three. Done in `prototypes/pattern-skirmish/index.html`: 20 patterns eased (new "n allies beside it" kind), poison 2→1, planning AI (plays each move out, then the enemy's best reply, on a restorable copy of the match; smarter level-ups), `TUNE` factor per champion found by 7 automatic passes. Measured: planning AI beats old AI 74–78%; win rates 33–76% → 43–56% (random draft), 43–59% with the pattern drafter (Frost group on top); pattern drafter vs random 57–62%; final 2000 matches: 12.8 rounds, 86% by nexus, A 48%, comeback 16%, 3.2 regroups, 0.84 Barons, Baron taker 78%. 34 scripted human matches, no errors. Artifact republished. Not committed. Next: owner plays. Known: melee champions cast little (out of reach); AI wins by trading, not by building shapes; comebacks still low.

**PATTERN SKIRMISH — 25 CHAMPIONS — 2026-11-19.** Owner asked for 15 more champions (5 per role), Nano Banana art (one version each), and numbers on regroups, comebacks and Baron. Done: 15 champions in `prototypes/pattern-skirmish/index.html` (standard growth path via `autoTree`), `tools/asset_gen/gen_sprites.py` got `--spec`/`--out`, briefs in `assets/data/art/pattern_sprites.json`, 15 images kept first try. Measured (400 matches × 3 seeds): 3.7 regroups/match, comebacks 18–20%, 0.94 Barons/match, regroup toward Baron 26%, Baron taker wins 80%, Baron while behind ~1 match in 6 (about half won), sides 48–51%, 79–82% end by nexus. Findings: champion win rates 32–73% (Fire/poison champions on top), pattern drafting worth only +3–5 points, several W/R patterns almost never fire (Gale, Bulwark W, Fang R, Lens R…). Artifact republished. Not committed. Next: owner plays; candidates: balance pass, redraw dead patterns, regroup lead 2 vs 3.

**PATTERN SKIRMISH — NEW PROTOTYPE — 2026-11-19.** Owner asked for a free review of the auto skirmish's flow (synergies too simple, stagnation after a won fight, corner pits). Review measured on a scratch copy: no phases (first kill round 1.6, flat damage), stances do the moving, monsters untargetable, any connected shape gives 54% of the best synergy; pits one hex inward double Dragon kills. Owner rejected March/Momentum (measured: a march keeps the shape only 30% of the time) and proposed: elements, 3 abilities with easy→hard patterns, chances inside abilities, a regroup phase after a decisive fight with range tied to the death timer. Built as `prototypes/pattern-skirmish/` (index.html + art + README); `prototypes/auto-skirmish/` untouched. In it: 6 elements, Q/W/R per champion with patterns and cooldowns (2/3/4 pulses), statuses moved into abilities, level-ups upgrade abilities, regroup (lead 2, range 1 + death timer, max 5, loser plans respawn hexes, timers unchanged), pits inward, Dragon = one level-up, no gold/stances/specials, HP ×1.25, 18 rounds. Measured (400 AI matches): 79–81% end by nexus (54% with regroup off), A 48–55%, comeback 15%, 3.7 regroups/match, casts Q 44 / W 21 / R 11 vs 39 basics. 25 scripted human matches through the interface: no errors. Not committed. Next: owner plays; open questions in the prototype README (regroup lead 2 vs 3, loser's options, mirrored patterns for team B, gold).

**AUTO SKIRMISH — CASTERS — 2026-11-18.** Owner: pulses too quick to follow; asked to rate a two-caster live commentary idea, then "go ahead" with Claude's changes (tiers, cap, predictions, recap, corner feed, headline pause, on-board warnings, replay). Done (D-079). Game logic untouched (headless numbers identical). Artifact republished. Next: owner watches; tune line wording and density.

**AUTO SKIRMISH — OPENING HAND — 2026-11-18.** Owner: opening cards in the panels still force scrolling. Done (D-078): one strip under the board with the playing team's unplayed cards in short form; board shrinks a little meanwhile; statuses share the chances row; Baron-in-round-0 chip bug fixed. Worst case 756/768 px over a whole match. Artifact republished.

**AUTO SKIRMISH — COMPACT PANELS — 2026-11-18.** Owner: no scrolling to see the teams; growth path on hover; chances as icon + number. Done (D-077): compact cards with emoji icon rows, hover card with all details; fits 1366×768. Artifact republished. Next: owner plays.

**AUTO SKIRMISH — BOARD TURN, EFFECTS, MIDDLE TOWERS — 2026-11-18.** Owner: rotate the board (nexus bottom-left / top-right, diagonal movement), bigger hexes not champions, bigger status icons, simple graphics for attacks/spells/statuses, towers next to the Dragon make no sense. Done (D-076): 30° turn + squash, hexes 1.4x, code-drawn effects per action kind, statuses drawn on sprites, one tower per team on the middle line, centre tower removed. Measured 300 matches: sides 50%, Dragon 0.84/match (was 0.62), comebacks 20% (was 14%). Artifact republished. Next: owner plays.

**AUTO SKIRMISH = MAIN LINE + FIRST ART PASS — 2026-11-18.** Owner: pulled latest; make the auto skirmish the main version; copy the Nano Banana key from the golf game (done: `tools/asset_gen/.env.local`, git-ignored); a minimal Rogue-Legend-like look, one image per thing, no effects, to judge look, pulse length and flow. Built (D-075): `gen_sprites.py` + `skirmish_sprites.json`, 15 images (NB2 for characters with Anchor as style reference, NB2 Lite for tower/nexus/grass), all kept first try; art view on by default (isometric squash, grass tiles, sprites depth-sorted, breathing/lunge/hit flash in CSS), portraits on cards, pulse timer + match clock in the strip. Watch mode: pulses 3–4 s, no console errors. Next: owner plays and judges the look; then style tweaks, effects, maybe per-team tint.

**AUTO SKIRMISH — RESPAWN WINDOW + MONSTERS — 2026-11-18.** Owner asked how respawns grouped (explained: same-round deaths always together; "together" rule pulled in earlier deaths) and wanted a float-like window plus Dragon/Baron. Built (D-074), default = window 1 + monsters. Measured: window = no grouping for comebacks (15%); together rule 21–23%; Baron taken while behind wins 67% but rare (~6% of AI matches), Baron taker 89%, steals 14%, monsters ~1 per match. Next: owner plays; knobs: window size vs comebacks, Baron strength, AI appetite for monsters.

**AUTO SKIRMISH — COMEBACKS — 2026-11-18.** Owner: no comebacks; no towers near the nexus; one move per turn can't push or retreat as a group. Built (D-073): team stances, base towers outside the board, home-only regen, home zone, respawning together; all in the destroy preset, now default. Measured: comebacks 16% → 22% (only respawn-together + base towers moved it), tower within 2 rounds of a won fight 58%, 79% by destruction, 13.1 rounds, sides 49%, greedy AI break-even. Next: owner plays; candidates to reach 25–35%: shorter early death timers, AI that overextends, stronger base towers with a longer limit.

**AUTO SKIRMISH — DESTROY THE NEXUS — 2026-11-17.** Owner: a nexus building you destroy should feel better on the small board. Built behind the "Nexus" switch (also on the draft screen) with preset nexus 30 / towers 10 / 14 rounds / death step 2 (D-072). Measured: first version stalled (25% destroyed); preset 84% destroyed, 10.8 rounds vs 7.7, sides 52%. Default still score rules. Not measured: destroy + once-per-round pulse together. Next: owner plays both.

**AUTO SKIRMISH — GOLD, CONTROL, OPENING LEVELS, SPECIALS — 2026-11-17.** Owner asked for gold+items and more statuses, then opening level-ups (after cards 2/3/5) and charged secondary attacks; graphics parked (plan in `design/art/visual-pipeline-plan.md`). Built all four behind switches (D-071), measured alone and together: sides even, gold lead at round 5 → 81–84% win, control doesn't shorten pulses (3.1 s all on), ~15 specials per match. Human flow tested: opening level-up overlay, shop overlay, level-ups. Next: owner plays.

**AUTO SKIRMISH — PACE — 2026-11-17.** Owner prefers the small board; worried a pulse after every move is long. Done (D-070): small board default; idle champions get no beat; fast/slow beat rhythm (0.22 s plain, 0.7 s chance/kill); same-initiative champions shown as one beat; "Pulse: once per round" switch with a longer preset (16 rounds, levels 3/5/8/11, siege 3, death step 4). Measured: pulse 3.9 s → 3.0 s; once-per-round preset 43 s of watching per match vs 47 s, 90% end by nexus, won fight → win 76%. Next: owner compares the two pulse modes.

**AUTO SKIRMISH — HOLDING A WON FIGHT — 2026-11-17.** Owner: after a won team fight the respawned losers reset the advantage. Built (D-069): destructible side towers 15 HP gating nexus siege, death timer +1 round every 3 rounds, 3 HP regen when not hit last pulse, pushing AI (`AIP.push` 4); fight metrics in `simulate()` (swing-back, kills in the 2 rounds after a won fight, tower next round). Result on 1000 matches: swing-back 17%→7% (small), winners out-kill losers after a fight, 1/3 of won fights → tower next round, sides 48–49%. Watch: a won fight decides the match 82–84%. Next: owner plays.

**AUTO SKIRMISH — 2026-11-17.** Owner's new direction after playing Rogue Legend: percentages that grow, simple two-option level-ups, auto-battle after each move; keeps draft by role and a movement-only opening (two cards per champion). Built `prototypes/auto-skirmish/index.html` + README: 10 champions with tags, auras, bonds, per-champion targeting rules, 14 chance effects, seeded RNG, one move then a pulse by initiative, hover preview of synergy gains/losses + targets, level-ups at rounds 2/4/6/8, bounty, Field 7 + small board, watch mode, `simulate()`. First numbers (300 AI matches): Field 7 9.1 rounds, 60% end by nexus, 10.5 deaths; small 6.6 rounds, 94% by nexus; sides ~fair. D-068 (determinism pillar relaxed by owner). Other prototypes untouched. Next: owner plays both boards; tune pace, decide the map size.

**PROGRAMMED ROUNDS — HINTED CARDS — 2026-11-09.** Owner: v2 work got too convoluted; continue the programmed-rounds prototype autonomously. Keep draft + scripted opening; endgame = secret cards per champion, each with a public open effect on placement (the hint) and a secret main action by initiative; a lot should change in one round. Done in `prototypes/programmed-rounds/index.html`: one card per alive champion per round, snake placing, open effects (step/swap/mend/guard/chip/shove/siege) with player choices, secret aim after the open effect, enemy panel shows "could be X or Y", resolution 4→1 with shields/heals first; new roster numbers (HP 12–18, dmg 3–7), nexus 30, kill 5, 6 rounds, respawn after one round; AI places one card at a time, reads hints (never cards), symmetric (30/30 mirror test). Headless harness via URL hash (`#sim`, `#log`, `#uitest`, `#symtest`) run with Chrome `--dump-dom`. Findings in the README: ~33% HP/round, ~2 deaths/round, ~5 rounds; turn order worth 10–15 points and fragile; Bulwark area-exhaust was 95% (fixed); Ranger > Gunner remains. D-067. Not committed. Next: owner plays Field 7 vs AI; candidates: bluffing AI, fewer dead cards, a stable fix for the first-move edge.

**PROGRAMMED ROUNDS — SCRIPTED OPENING — 2026-10-24.** Owner: the plain-move opening was not what was meant; wants v2's scripted opening (each ability a script of moves for several roles, one script per champion casts). Done in `prototypes/programmed-rounds/index.html`: `OPENINGS` table (30 scripts, three routes per champion), strict moves, must-play, fallback step, one casting script per champion, auto-aim, no-kill floor, hover preview, AI picks scripts by try-and-undo on the real state (`teamPosScore` + cast value); `applyEffects` factored out of the reveal; Field 7 now the main game's geometry (back-row start, nexus line, fronts six rows apart) and the default; `debugCheckOpenings()` validates scripts. Finding: fronts four rows apart → scripts collide on the river row (mirror keeps row 0 fixed), half the champions fall back; six rows apart → full opening, ≤1 fallback, lines end 1–2 hexes apart. Small board: 11/30 scripts blocked from the start, needs its own scripts. D-066. Headless: both boards run AI matches; human draft → scripted plays by button → program → round 2; no script errors. Artifact republished. v2 untouched. Next: owner plays Field 7; candidates: small-board scripts, swamps, armed programs.

**PROGRAMMED ROUNDS — OPENING ADDED — 2026-10-22.** Owner played the prototype: programming while the armies are far apart feels random; keep the opening, start secret rounds when the units are close; map maybe not smaller. Done in `prototypes/programmed-rounds/index.html`: visible alternating opening (one champion per play, N steps, everyone once) before round 1; board switch small (37) / Field 7 (59, mirrored); per-hex threat count (enemy champions in reach next round) on by default; opening AI uses it. Finding: on the 37-hex board speed 2 + range 3 reaches nearly everything from round 1 — no approach existed. D-065. Headless check: both boards complete AI matches; human draft → opening clicks → program → round 2. Artifact republished. v2 untouched. Next: owner plays both boards and says which feels like "meeting in the middle"; candidates after that: shorter reach / larger map, the scripted opening instructions from v2, armed programs.

**PROGRAMMED ROUNDS PROTOTYPE — 2026-10-21.** Owner asked for an HTML prototype. Built `prototypes/programmed-rounds/index.html` (single file, no server): 10-champion draft with give/want, 37-hex board, energy budget, commitment by tier, simultaneous tiers 4→1 with control→damage→deaths, lost/fizzled, exhaust-as-net, caught alone, 8 rounds, greedy AI committing blind, watch mode, counters strip. README per prototype rules. Published as an artifact. Not in it: opening, quests, spells, ceiling toggle. v2 untouched. Next: owner plays and reports feel.

**PROGRAMMED ROUNDS BRAINSTORM — 2026-10-21.** Owner's idea (secret program per champion per round, descending-initiative ladder with discards, smaller map); owner asked to think, not build; v2 untouched. Three specialist reports (core loop, pace, Neuroshima) → `design/v3-programmed-rounds.md`: agreements (secret commit, one ability per champion + descending pair, drop halves/basics/pass/chains/spells, 37-hex board, staged reveal, undercut exploit), disagreements with Claude's calls (simultaneous tiers default, energy budget, commit by tier), 12-rule prototype candidate, metrics. Evidence in `production/qa/evidence/v3/`. D-064. Next: owner reacts; a prototype would be a separate rules mode + board beside v2.

**MOMENTUM — 2026-10-18/20.** Owner wants League-style swings (ambush the carry → take the objective). Measured: carry kills were a non-event (43% win). Four levers shipped (D-062/D-063): respawn per stack, death costs half stacks, caught alone ×1.5, death check each half → carry-kill win 60–66%; opening/draft targets hold. Shrine drafted (not built) in v2-direction. Owner's Marvel Snap idea analysed there: options A (lethality dial — measured: noise), B (chains grow with the round), C (every round an opening: secret programs, reveal, resolve). Recommendation B then prototype C. 115 tests. Next: owner reacts to shrine + B/C.

**OPENING MUST MATTER — 2026-10-11.** Owner: after the draft choose a strategy; a good opening should make the endgame easier; quests (A1) + terrain (B1) go; objectives parked; targets: good vs random opening 60–65%, good vs tragic draft ~65/35. Done: exposure + towers ignore wanted defenders (D-058); quests with 2/4 upgrades, 15 authored and tuned (D-059); hills/swamps on Field 7 (D-060); opening-end quest check, 1 basic per half, +8% POW per stack, planning opening AI (D-061); browser shows terrain, quests, stars. Results: good opening 61%, great vs tragic draft 71.5%, 12.4 rounds. 113 tests. Next: owner playtests; objectives later.

**DRAFT MUST MATTER + MATCH REPORT — 2026-10-09.** Owner: good vs tragic draft ~65/35; wanted a readable account of an AI match. Done: per-champion in-match stats (D-055); give/want synergy model, champion "wants" trait ×2 floor 3, tragic drafts now possible (D-056); balancing pass 43–55% (D-057); drafter styles great/decent/tragic, `selfplay draft=X-Y`, `payoffScale`; `chronicle` tool + `design/match-reports/2026-10-09-two-ai-matches.md`. Result: great vs tragic 52% (synergy worth +7; towers decide matches; Wildfire dominant, marks/roots weak). 109 tests. Next: owner chooses how to reach 65/35 (reliable marks/roots, synergy affects towers, or kills worth more).

**SYNERGY ROUND 2 — 2026-10-08.** Owner: synergy medium-leaning-small, combo AI, fix Mortar. Done: payoffs ×1.5 floor +2, slam 3/4 (D-052); Mortar SPD 2 + global speed floor 1 (D-053); depth-3 combo AI default (wins 62% vs old; setup pricing measured useless, off) (D-054); payoff/slam counters in selfplay. Draft re-run: side bias gone; synergy drafter 51%; payoffs 7.0 vs 4.9/match; Mortar 33–39%; Wildfire & Hold the Line weak. Results in `design/v2-champions.md`. 108 tests pass. Next: owner on whether ×1.5 is enough (proposal ×1.75 +3).

**15-CHAMPION ROSTER + DRAFT EXPERIMENT — 2026-10-08.** Owner asked for 15 champions with groups that work together, and to measure equal vs unequal drafts instead of balancing. Done: 5 new champions (Briar, Talon, Pyre, Mortar, Seer), slam + marked/exhausted bonuses (D-049), `assets/data/synergies.json` with 7 groups + synergy score (D-050), `SynergyDrafter` (AI + web), `selfplay N draft=random|synergy|mixed`. Results in `design/v2-champions.md` → "The draft experiment": spread 28–65%; synergy score alone doesn't predict wins (synergy drafter 48% vs random); group identity does (Lockdown/Called Shot win, Crush/Hold the Line lose — Mortar 28%). No balancing (D-051). 106 tests pass. Next: owner decides synergy strength, combo-aware AI, when to tune.

**v2 PLAYABLE — 2026-10-04.** Direction: `design/v2-direction.md`. Built on the owner's "design them and build on your own":
- **Game:** 10 v2 champions (3 abilities + summoner slot + signature opening), 8 spells, hidden simultaneous spell pick, opening casts (role+slot, fizzle, cooldown ≥1, can't kill), status verbs (root, burn, mark, wall, swap, pull ally, unstoppable, cleanse, exhaust, wound), Field 7 with solid towers shipped in `rules_config.json`.
- **Content source:** `design/v2-champions/roster_v2.py` writes `assets/data/champions/*.json` + `spells.json` after checking every opening (40/40 playable). Roster table + measurements: `design/v2-champions.md`.
- **Clients:** browser (spell-pick screen, status chips/pips, walls, cast steps in diagrams, hidden spells D-048), CLI (spell pick, statuses), tools (`openings`, `selfplay` with fallback depth).
- **Tests:** 102 pass (v1 rules on fixture roster `tests/fixtures/champions_v1` + classic; `V2RulesTests` on shipped content, "Board switching" collection).
- **Self-play (200):** 13.6 rounds, every match ends by nexus, champions 42–58% after balance pass D-047, round-1 opener wins 54–59% (accepted D-045), opening fallback 44% (mostly last champion only).
- **Decisions:** D-044…D-048 added. Evidence screenshots: `production/qa/evidence/v2/`.
- **Next:** owner playtests v2; open questions at the end of `design/v2-champions.md`.

**FIRST PLAYABLE BUILT — 2026-09-27.** The owner granted standing authority to finish the
design and build to a first playable, logging every unilateral decision.

- **Play (browser, 2026-09-28):** `dotnet run --project src/Augury.Web`, open
  http://localhost:5080. Broadcast layout: team panels left/right, map centre, hover
  ability → reach/targets, exact previews on hover, undo. Evidence in
  `production/qa/evidence/browser-client-walkthrough.md`. D-034…D-036 logged.
- **Play (terminal):** `dotnet run --project src/Augury.Cli` (see `PLAY.md`). Modes: vs AI
  as A or B, hotseat, watch.
- **Rules:** `design/mvp-rules.md` (authoritative for the build). **Decisions:**
  `design/claude-decisions.md` (D-001…D-042, each reversible). **Report:**
  `design/first-playable-report.md`.
- **Code:** `src/Augury.Sim` (content loader, blittable MatchState, full rules engine in
  `Rules/Game*.cs`, AI in `AI/Agent.cs`), `src/Augury.Cli` (terminal client),
  `tools/Augury.Tools selfplay|trace`. 67 tests pass.
- **Data:** `assets/data/rules_config.json`, `assets/data/champions/champion_NN_name.json`
  (10 placeholders, 2 per role).
- **Self-play (500, final rules):** A/B 48/52; 8.2% nexus endings, 12% of them from behind;
  13.4 rounds; 7 of 8 GDD acceptance criteria pass. Fails: halves ending by pass 19% vs
  ≥50% — attributed mostly to the greedy AI.
- **Key tuning finding:** the both-towers nexus gate never stayed open (home towers sit by
  the spawn row, defenders retake at once) → loosened to either tower (D-025), tower HP 16,
  nexus HP 25, target 60 (D-026).
- **Environment note:** the container recycles; .NET SDK is re-staged by downloading the
  Ubuntu debs with `apt-get download` into /tmp and `dpkg -x` into /tmp/dotnet-stage
  (`/tmp/dn.sh` sets DOTNET_ROOT=/tmp/dotnet-stage/usr/lib/dotnet).
- **Next:** owner playtests; answers to the six questions in the report decide what changes.

---

## Earlier Task

**OPENING PHASE — multi-champion instruction sets (user's idea, 2026-08-17). MEASURED,
NOT YET WRITTEN TO A GDD.**

*The proposal.* Each champion still spends one ability in the opening, but an opening
ability issues instructions to **several champions at once** — "jungler one north then one
north-east, top one north, bottom one west" — and may also place beacons or apply hex
statuses. Replaces the earlier idea of each champion simply moving toward the centre.

*The claim under test:* multi-champion instructions make the opening a **sequencing
puzzle** rather than five independent choices. Falsifiable — it is only true if the ORDER
of play changes where everyone ends up.

*Measured (`dotnet run --project tools/Augury.Tools opening`), 2,000 random ability sets,
all 120 orderings of each:*

| design | distinct outcomes / 120 | spread |
|---|---|---|
| independent, 1 self-move each | **1.7** | 0.06 hex |
| multi-champion, 2 instructions | 8.0 | 0.22 |
| multi-champion, 3 instructions | 20.5 | 0.37 |
| multi-champion, 4 instructions | 36.8 | 0.51 |

- **The independent version is 1.7 of 120 — order is essentially irrelevant.** Confirms
  the problem the user identified: it is a setup script, not a phase.
- Multi-champion instructions raise it ~20x, to 20-37 genuinely different formations.
- **The engine is exactly what the user predicted:** order matters *because instructions
  become illegal* (board edge, occupied hex), and which are illegal depends on what came
  before. Skips convert sequence into consequence.
- ⚠ **Caveat:** formations differ by only ~0.5 hex per champion. Many distinct outcomes,
  clustered close. Whether that is decisive is an inference from tier-4 patterns being
  fixed 5-hex shapes at 36% applicability, NOT measured.
- ⚠ Instructions were random, not authored — cuts both ways (authored abilities would
  interact more deliberately; random directions collide more often).

*My assessment: build it.* Converts the weakest phase into a real decision without adding
a subsystem. Two consequences worth pricing:
1. **It implements beacon placement**, which `sigils-and-beacons.md` deferred to the
   Opening Phase without saying how. An instruction set that includes "place a beacon at
   X" is the missing mechanism.
2. **Proposed (mine, not yet agreed): whichever ability a champion spends in the opening
   starts the match on cooldown.** Generalises the cost that Sigils & Beacons already
   assumed for beacon-placing abilities, and makes the opening matter in combat rather
   than only on the board.

*Strongest argument for it (user's):* an imperfect opening leaves chain partners badly
placed, and fixing it costs **basic actions — 2 per half for the whole team**. Opening
mistakes do not wash out; they are paid in the scarcest currency in the game.

*Costs.* Double authoring (40 ability definitions per 5 champions, not 20 — mitigable by
having only 2 of 4 abilities carry opening instructions). AI search ~123,000 sequences
(4^5 ability choices x 120 orderings) before beacons or targets, inside the same 1.5s
budget as the ladder.

*USER DECISIONS 2026-08-17:* **all four abilities carry opening instructions** ·
availability is **all-or-nothing and highlighted in the UI** · **you MUST play an available
ability if any exists** (no skipping, no choosing which champion — you choose among legal
(champion, ability) pairs across all unacted champions) · **fallback only when NO unacted
champion has any available ability**: then each remaining champion moves one hex in any
direction. No dead end is possible.

*Measured — the strictness trap (`opening` command).* Under **strict** all-or-nothing
(every instruction must be legal), the phase collapses as abilities get richer:

| instructions | strict: openings hitting fallback | lenient (first instruction only) |
|---|---|---|
| 2 | **45.0%** | 12.1% |
| 3 | **63.9%** | 7.7% |
| 4 | **81.8%** | 6.1% |

- **Strictness fights the thing that makes the idea good.** Richer abilities create the
  sequencing depth (36.8 distinct formations at 4 instructions vs 8.0 at 2), but are less
  often legal — so 82% of openings degenerate into the fallback, which IS the boring
  independent design this replaces.
- Lenient holds availability flat: **6.7 of 20 pairs legal at step 1**, **2.1 of 4** for
  the last champion at the squeeze.
- **The constructive puzzle is real and improves with depth:** 23-30% of illegal abilities
  become legal after someone else moves (22.9% at 2 instructions → 30.0% at 4). "Play X to
  unlock Y" works.
- ⚠ Caveat cutting against the strict verdict: instructions were RANDOM, and random
  directions from a front-line start are illegal far more often than authored ones. Strict
  may survive careful authoring — but then every ability must be hand-checked against the
  opening formation, and one careless ability tips a team into the scramble.

*CORRECTION 2026-08-17 — the strict numbers above were WRONG, and the user caught it.*
The random generator produced **incoherent** abilities (each instruction pulling a
different champion a different way) and, worse, a third of its directions pointed
**backward off the team's own front line** — something no designer would author. Re-run
with coherent, forward-and-lateral-only abilities whose instructions share one lateral
intent:

| instructions | strict fallback, RANDOM (wrong) | strict fallback, AUTHORED |
|---|---|---|
| 2 | 45.0% | **8.6%** |
| 3 | 63.9% | **17.2%** |
| 4 | 81.8% | **24.5%** |

**STRICT ALL-OR-NOTHING IS VIABLE. The user's rule stands as specified — no
key-instruction relaxation is needed.** My proposal below is withdrawn.

*And the draft axis the user described is REAL, with a threshold nobody predicted.*
Instructions name a ROLE, not a champion, so an ability's value depends on who was
drafted into that role. Aligned vs mismatched drafts, identical ability sets:

| instructions | aligned | mismatched | benefit |
|---|---|---|---|
| 2 | 2.37 | 2.38 | **none** |
| 3 | 2.37 | 2.52 | 6% |
| 4 | 2.51 | 2.71 | 7.4% |

- **At 2 instructions, drafting for opening synergy is worth NOTHING.** The axis switches
  on at 3. Instruction count is not only sequencing depth — it is what makes the opening
  kit worth drafting for at all. The user's "draft a weaker endgame champion because he
  places my team better" trade does not exist below 3.
- **RECOMMENDED: 3 instructions per opening ability.** Fallback 17.2%, draft axis live,
  20.5 distinct formations from ordering. At 4, fallback reaches a quarter of openings.
- ⚠ The alignment proxy is crude (want = sign of net push x 4) and the placement AI plays
  near-optimally in both conditions, so 6-7% is a **floor**, not an estimate.
- Small real cost found: aligned drafts fall back slightly MORE (8.6% vs 6.9%) — playing
  for position sometimes means choosing an ability that serves the formation over one that
  keeps options open.

*WITHDRAWN — the KEY INSTRUCTION rule.* Each ability names **one key
instruction**; the ability is available iff its key instruction is legal, and the others
execute if they can and skip if they cannot. Keeps availability **binary and
highlightable** (what the user asked for), keeps depth free, avoids the collapse, and adds
an authoring lever: a demanding key = powerful and situational, an easy key = reliable and
dull.

---


**STEP 3 of 3 DONE — CHAMPION & ABILITY SCHEMA UNPARKED, 2026-08-17.** All three parking
reasons resolved; doc now 612 lines, status "Revised (pending review)", index updated.

*Parking reason 1 (stats depend on undesigned systems) — PARTLY resolved, `RES` DELETED.*
`SPD` is now settled by Movement & Targeting (path length, basic budget). `RES` is not —
Status Effects still has no GDD. The parking note said both were "candidates for deletion,
not merely tuning", so applying that honestly means **the model drops to FIVE stats**
(VIT/POW/ARM/RCH/SPD). `RES` returns via Status Effects with a designed job, or not at
all. Flagged as open question 1, deliberately easy to reverse.

*Parking reason 2 (applicability conditional on board) — RESOLVED.* Note the irony: the
tool hardcoded radius 4 and the board turned out to BE radius 4, so the board-size
assumption was accidentally right. What was wrong was **objective placement**. Targets
now `[0.96, 0.84, 0.67, 0.36]`.

*Parking reason 3 (kit shapes too narrow) — REWRITTEN as the currency.* Sum 9-11 band,
≤2 per tier, non-decreasing. `stat_adjustment = (10 − Σ init) × C`, ⚠ C = 150 permille.
`[1,2,2,4]` now legal. Ladder/Anvil/Vice survive as examples, not as the whole space.
**Tension flagged (open question 2): ladder F4 already flattens effective value per
ability, so the trade may DOUBLE-COMPENSATE and C may belong at 0.** The case for C > 0
is that F4 prices abilities in isolation while kit *distribution* decides how deep into a
descending ladder a champion can act — which F4 never measures.

*Amendments absorbed:* RCH clamps at +1000 (hard ceiling 3 hexes — the only stat with a
special clamp) · fifth slot = passive, with 4 triggers, ascending-champion-index ordering,
cascade depth 1 · `printed_sigil`/`slot_sigil` fields at 15%/25% · tier-4 offsets
team-relative per amended ADR-0005 · tier-4 SHAPE noted as the huddle-tax lever.

*F2 damage fell:* tier-4 example 12 → **9 damage** (40% → 30% of a champion), because
M(4) dropped 4.00 → 3.30. Output range 1-21 → 1-17.

*Also new:* RCH gained a second job — basic attacks strike at RCH, so it is the only stat
improving what a champion does with the team's basic-action budget.

---

**STEP 2 of 3 DONE — APPLICABILITY RE-MEASURED against the real board, 2026-08-17.**
The harness weighted champion placement toward **three placeholder hexes** `(0,0) (0,-3)
(0,3)` invented before Map & Terrain existed. Now weighted toward the **five real towers**
`(0,0) (0,-2) (2,-2) (0,2) (-2,2)`.

*What moved.* Real towers sit **2 hexes apart** where placeholders sat **3 apart**, so
champions cluster tighter and patterns line up more often:
- Tier 3 (rot. 2-hex arc @ r2): 0.587 → **0.673**
- Tier 4 (fixed 5-hex): 0.305 → **0.363**
- Melee r1: 0.501 → **0.469** (fell — 5 attractors spread champions across more sites)

*Consequence — ladder F4 went OUT OF CONFORMANCE at ±9.7% against a ±2% target.* Tiers 3
and 4 were being paid for a scarcity they do not have. **`M` revised
`[1.0, 1.3, 2.0, 4.0]` → `[1.00, 1.23, 1.64, 3.30]`** (tiers 3-4 down ~18%), restoring
flatness to ±0.2% (0.897-0.900). The harness now prints the F4 conformance check itself,
so this cannot silently drift again.

*Also:* **tier 1's reference pattern changed** from "free targeting, range 4" to range 3,
because Movement & Targeting caps RCH at 3 and the old reference is no longer a legal
ability.

*What survived unchanged — all three qualitative findings.* Melee is still never the most
applicable tier; rotatable **reach** still dominates rotatable **area** (43.0% confined to
r2 vs 71.1% spanning r1-2); fixed-pattern applicability still rises ≈6pp per hex
(16.3 → 23.3 → 29.5 → 36.3 → 41.8, mean step 6.4pp). **The shape was right; only the
decimals were wrong.** Ladder GDD updated throughout, and its ⚠ "conditional on a board
that has not been designed" warning is now resolved.

*Remaining: step 3 = unpark the champion/ability schema.*

---

**STEP 1 of 3 DONE — ADR-0005 AMENDED 2026-08-17 (team-relative tier-4 patterns).**
Tier-4 patterns were specified as applied *verbatim* (world space). Because the board's
symmetry is a 180-degree rotation, the two teams face opposite ways, so a verbatim wedge
aimed at the enemy for team A aimed at team B's own nexus. Now: tier-4 offsets are
authored in a **canonical frame with forward = +R** and reoriented per team.
- Added `Hex.HalfTurn` and `Hex.ForForward` to `src/Augury.Sim/HexCoord.cs`.
- **The fix needed no new geometry**: a half-turn is exactly `Rotate(offset, 3)` — a
  rotation the six-facing system already expresses — and it is the same map that defines
  board symmetry, so the far team's pattern covers exactly the antipodal hexes.
- **Why rotational symmetry was load-bearing all along:** a chiral pattern's *mirror* is
  reachable by NONE of the six facings. On a mirror-symmetric board the two teams would
  hold different-shaped versions of the same ability and no orientation rule could fix it
  — every chiral tier-4 would need authoring twice. Asserted in
  `PatternOrientationTests.MirroredPattern_IsReachableByNoRotation`.
- 8 new tests, 40 passing. ADR validation criteria now checked off rather than open.
- Cross-referenced from Map & Terrain rule 4.

*Remaining: step 2 = re-run applicability against real tower positions; step 3 = unpark
the champion/ability schema.*

---

**MOVEMENT & TARGETING GDD WRITTEN 2026-08-17** — `design/gdd/movement-and-targeting.md`
(system #6, Drafted). **Two economies, and movement is off the ladder entirely.**

- **Basic economy:** 2 basic actions per team per half, by **two DIFFERENT champions**,
  **compulsory**. A basic = move one champion, or one basic attack.
- **Ability economy:** unchanged — 1 ability per champion per half, on the ladder.
- **Independent.** A champion that took a basic MAY also use an ability that half.
- **Basics resolve BEFORE the ladder. Positions are then LOCKED for the half.**
- **Champions block movement**, friendly included. Path length, not distance, is what SPD
  limits. **No line of sight** for targeting.
- **NEW: every champion has a passive ability** (4 actives + 1 passive). Passives exist so
  basic attacks — unanswerable on the ladder, since they resolve before it — have an
  automatic answer. Passives carry no sigil, so the 20-ability chain figure is unaffected.

*Measured (`dotnet run --project tools/Augury.Tools mobility`) — and I had to correct
myself here.* I first reported "one champion crosses in 1.0 round, the economy inverts,
fast or broad". That assumed one champion could take both basics. With the
different-champions rule the true picture is:
- Individual crossing speed is **unchanged** (2.0 rounds at SPD 2).
- Team-wide repositioning is **2.5x slower** (1.2 rounds vs 0.5).
- Crossing costs **50% of the team's basic economy** for its whole duration, and that
  share is **invariant in SPD** — speed buys time, never budget.
- A champion averages **0.8 basics/round** against 2.0 before.
- **Breadth is the scarce resource, not speed.**

*Closed three open questions across other GDDs:*
- Ladder Q3 (is movement an initiative-1 action?) → **no**, off the ladder.
- Map Q6 (impassable terrain?) → **no — the terrain is the other players.** A 5-champion
  wall adds 3 hexes of detour and leaves no hex unreachable, so bodies tax movement
  without recreating corridors.
- Sigils denial B is **partly superseded**: the universal cheap dodge no longer exists,
  because nobody can move during an exchange. Denial B survives only via deliberate
  low-initiative abilities; denial A untouched.

*Amendments owed to `champion-and-ability-schema.md` (noted in that file):*
1. **RCH must cap at 3, not 4** — reach 4 covers the whole board from the centre (100%),
   59% averaged. A radius-4 board cannot express 4 as a limit.
2. **Fifth ability slot for the passive**, plus trigger vocabulary and deterministic
   ordering for simultaneous triggers.

*Open:* one-champion-alive case (2 distinct champions unsatisfiable) · can basic attacks
damage structures (stalling risk) · should friendlies block (most likely to feel bad) ·
basic alternation order · shortest-path tie-break for determinism.

*Risk to watch — criterion 12:* the two-economy split makes **the ladder opt-in**. A team
ahead on points can basic-attack and decline abilities. Partly self-solving, since molding
comes from ability use so passivity forfeits stat growth. Harness bound: ladder opened in
≥70% of halves.

---

## Earlier Task

**Map & Terrain GDD written** — `design/gdd/map-and-terrain.md`. Board fixed:
radius-4 hexagon, 61 playable hexes, teams facing across opposite **edges**, 12
off-board spawn hexes, 180-degree rotational symmetry, one champion per hex, **no
lanes**, 5 towers, 2 jungle flanks, front-to-front 8 hexes.

**Spawn rows (user's idea, adopted).** Six off-board hexes per team behind each front
line. Every champion owns a designated hex; the jungler owns two and picks a flank.
Untargetable — outside the game entirely, so spawn camping is impossible by
construction. Entering play costs the champion's action (tunable to free).

*Why they exist:* ADR-0006 batches every death to round close, so multi-death rounds
are the normal shape of a good exchange, not a rare ace. A single shared respawn hex
would have broken in ordinary play. Dedicated per-champion hexes make simultaneous
respawn collision-free with no tie-break and no fallback placement path.

**Board flipped corner-facing to edge-facing** to fit them: a corner has only 3
off-board neighbours (cannot seat 5), an edge has 8. A radius-4 edge is exactly 5
hexes — one per champion — and radius 4 is therefore the *smallest* board that seats
a team abreast. A corner base would also have funnelled 5 respawning champions through
2-3 hexes, reintroducing the single-file problem that removing lanes solved.

Coordinates now read as **rank** (`R`, toward the enemy) and **file** (`Q - S`, across).
Both negate under the symmetry map, so every zone rule is written on absolute value.

The finding that decided it: on a hexagon the outer edge is a single ring, so any
edge-hugging lane is **1 hex wide at every radius** (verified at radius 4 and 6 with
`tools/Augury.Tools board`). Lanes therefore *create* the bot/support single-file
problem rather than solving it — and lanes were never asked for. Removing them makes
the problem vanish and lets one-champion-per-hex stand, which the rigidity tiers need.

**Constraint discovered — owed to ADR-0005 as an amendment.** Symmetry must be
180-degree rotational, never mirror. A mirrored tier-4 pattern is *chirally* different
and no rotation produces it, so under a mirrored board the two teams would have
differently-shaped versions of the same ability. Consequently tier-4 patterns must be
fixed relative to **the owning team's forward direction**, not world-absolute as
ADR-0005 currently says. Must land before any tier-4 ability is authored.

**User decisions this session:** 61 hexes (over my 91 recommendation) · towers both
damage and score · 5 towers, 2 per team plus a neutral centre · jungle is a **road for
the jungler**, not an obstacle, extra jungle effects explicitly deferred · edge-facing
board · spawn hexes fully untargetable · entering play costs the champion's action.

**Timing principle agreed for "decide now vs defer to vertical slice":** decide now
what changes the *shape of the data*; defer what changes only a *number*. A structural
choice deferred gets built twice; a numeric choice decided early was going to be tuned
anyway. Third category worth naming: questions play cannot answer at all (e.g. "are
spawn hexes targetable?" is rules-consistency, not feel) — those must be decided now.

**COMBO PROBLEM — sigils & beacons. GDD WRITTEN 2026-08-17: `design/gdd/sigils-and-beacons.md` (system #30, Drafted, pending review). Bidirectional deps wired into systems-index, initiative-ladder and map-and-terrain. Notes below are the working record.**

*Problem:* a player sets up a combo over rounds, and the opponent denies it procedurally
for free. Two distinct denial mechanisms, found by re-reading the ladder rules:
1. **Pass cuts turn count.** Play alternates, so you only get the actions the opponent
   grants by answering. A 3-piece combo needs two answers; they just pass. (2-piece
   combos already survive — the Last Word covers them.)
2. **Ceiling crash — the sharper one.** Answers must be ≤ last initiative played. Open
   Vortex at 3, they answer with a **1**, and your init-3 Meteor is now *illegal for the
   rest of the half*. Symmetric (they capped themselves too) but asymmetrically good for
   whoever has less to do — always the defender.

*Principle adopted:* counterplay to a combo must be **spatial and anticipatory**, not
**procedural and reactive**. Passing costs no position, no read, no commitment — that is
why it feels cheap.

*Mechanic converged on (USER'S refinement of my proposal, and better):*
- Abilities carry **0 or 1 printed sigil** and **0 or 1 typed slot**. Most have neither.
- Sigils are **fixed per ability, never generated** — also forced by Pillar 1 (no randomness).
- A **beacon** is a visible board object that **fills matching slots** in its area. It does
  NOT grant sigils on top of existing ones — my version did, and the harness shows that
  floods the board. The ability holds the *potential*; the beacon is the key.
- **Chain** = two same-active-sigil abilities on two different champions, resolving in ONE
  ladder step. Uninterruptible, so both denials die at once.
- **Chains may ASCEND** — the ladder never otherwise allows it, so today a combo's payoff
  must be weaker than its setup, which is backwards. Ceiling afterwards = highest
  initiative in the chain, so a big finisher still invites a full-power answer.
- Brake is action economy: a 2-chain spends 2 of 5 champions in one beat.

*Measured — `dotnet run --project tools/Augury.Tools sigils`:*
- Unit is the **champion duo** (5 champions = 10 duos), not the ability pair; a half has
  room for ~2 chains, so duo coverage binds.
- **WILD slots (take any beacon's sigil) are too generous** — one beacon reaches 4.7-9.7
  of 10 duos in every config. Confirms the user's instinct exactly.
- **TYPED slots behave.** Recommended: **3 sigils, 15% printed, 25% slots** → 1.01 duos
  natural, **4.08 with one beacon**, 1.1% dead drafts. Beacon lift 4x, so beacons are
  load-bearing rather than decorative.
- In kit terms that is **3 printed / 5 slotted / 12 plain** out of 20 abilities — which
  also answers the legibility worry, since most abilities carry no iconography at all.
- Caveat: bounds *draft-level potential* only. Live usage is lower (position, cooldown,
  availability, both champions unacted); random draft makes it a floor for skilled drafters.

*Rejected, with reasons:* combo meter (ambient/procedural — fights the spatial principle,
and stacks a second economy of restraint on the ladder). Rage-meter-on-death (rewards
being killed; we already have an *earned* comeback in the nexus terminator, and two
comeback systems make trading badly correct). Declared chains (user disliked; heaviest).

*Beacon decisions taken 2026-08-17:* **placement is mostly OPENING PHASE, with mid-match
placement possible via an ability and deliberately harder.** **Radius 1** (beacon hex +
6 neighbours).

*Measured — `dotnet run --project tools/Augury.Tools beacon`:*
- **The edge penalty makes the user's placement rule self-enforcing.** A radius-1 zone is
  7 hexes in the interior but clips against the board edge: 5 on a nexus hex, **4 on a
  lane mouth**. So a beacon planted safely at home controls 6.6% of the board versus
  11.5% in the middle — a **43% coverage penalty for playing safe**. Walking a beacon
  forward is rewarded by geometry alone; no extra rule needed to make late placement
  "harder but better".
- **On-tower vs beside-tower is a real placement decision, not a formality.** A tower has
  6 playable approaches. Beacon ON it → all 6 inside the zone, contesting is unavoidable.
  Beacon BESIDE it → the tower hex is still in the zone but **3 of 6 approaches stay
  free**. So beside-placement taxes *occupying* the tower while leaving *contesting* it
  possible; on-placement taxes both. Same for all five towers.
- Max coverage 11.5% of board, so refusing a zone is never ruinous — beacons shape ground
  without dominating it.

*Decisions 2026-08-17, second pass:*
- **THE CHAIN RULE MUST NOT MENTION BEACONS.** The rule is only: *two abilities with the
  same **active** sigil, on two different champions, resolve in one ladder step.* Printed
  sigils are always active; a slot is active only inside a matching beacon's zone. Beacons
  are a *source* of active sigils, not a term in the rule. This resolves the "must both
  champions be in range?" question automatically — each ability independently needs its
  own sigil live, so printed+slot needs one champion in the zone and slot+slot needs both.
  Not a choice; it falls out.
- **Beacon count is not a fixed number** — only certain abilities grant beacons, and they
  are rare because a beacon is powerful. An opening-phase beacon ability pays for itself
  with a cooldown carried into the action phase.
- **Action-phase beacon abilities are rarer still than opening-phase ones.** Rationale is
  an information argument: an opening-phase beacon is placed *in the dark*, before anyone
  knows where the fight goes; an action-phase beacon is placed knowing exactly where every
  champion stands. The same object is worth far more with information, so the price scales
  with information rather than with the object.
- **Beacons are DESTROYABLE, never expiring.** Intent is a race: two champions converging
  to use a beacon while two converge to break it.

*Measured — the huddle tax (`beacon` command):* a slot+slot chain forces both champions
inside one 7-hex zone, which is close to a fixed tier-4's 5-hex footprint.
- Compact tier-4 (blob/wedge): **81% of in-zone pairs catchable vs 16% of board pairs** —
  roughly **5x more exposed**. That is the "something for something", quantified.
- **But 4 of 21 in-zone pairs are still safe**, so positioning inside the zone is its own
  game, not a death sentence.
- **Pattern shape is the lever**: a *line* tier-4 catches only 24% of in-zone pairs. If
  beacons should be riskier, print more compact tier-4 shapes. Ability schema owns this.

*Flagged for Objectives/Ability schema — the trap to avoid:* **beacon destruction must not
be cheap or low-initiative.** A 1-action, initiative-1 "break beacon" would recreate
exactly the cheap-procedural-denial-of-expensive-setup problem that beacons exist to
escape — the whole design folding back on itself. Destruction should cost roughly what
the chain it denies costs (~2 champion-actions), which also matches the intended race.

*Still open:* whether a beacon may sit on a tower hex (user undecided) · chain length cap
(start at 2) · exact beacon durability in actions.

**Objectives brainstorm — decisions taken 2026-08-16:**
- **THE MATCH IS A POINTS RACE.** Everything scores: kills, tower control, possibly
  tower destruction, and the nexus. *(Corrected 2026-08-17 — I had wrongly written
  "destroying the nexus ends the match" as decided; the user was only asking how
  destruction could work. Do not reintroduce this.)*
- **Towers are captured (reversible, flip, tick score); only the NEXUS is destroyed.**
  Reversible towers are the main brake on snowball. The nexus is the largest single
  point source, not a separate way to win — a team can win without ever touching it.
- **NEXUS = PURE TERMINATOR, awards no points, ends the match at any score.**
  Two endings only: target score (the large majority) or nexus destroyed (rare).
  No round limit needed — towers tick, so the target is always approached.
- **The comeback is a DESIGN GOAL, not a leak.** *(Corrected 2026-08-17 — I had
  written the invariant "no team may destroy the nexus while behind on points". That
  is the exact opposite of the intent. The user wants a behind team that lands an ace
  and capitalises to win. Do not reintroduce that invariant.)* Criteria 24-26 now test
  the inverse: nexus endings must be non-zero, some must come from behind, and the
  nexus must be destructible in ~one ace window.
- **The calibration that makes it work:** nexus durability ≈ *most but not all* of one
  ace window for a full unopposed team. Too durable → comeback never happens, nexus is
  decoration. Too fragile → every ace ends the match, points race is irrelevant.
- Meshes with defender scaling for free: after an ace nothing is adjacent, so the nexus
  takes full-rate damage. The window when it is attackable and the window when the
  enemy is dead are the same window — neither rule was written to produce that.
- **Nexus = the middle three hexes of each front line.** Reaching it means standing on
  the enemy's own front rank. This closed board Open Question 7 — the front line now
  matters in every round instead of only the first.
- **Defenders scale structure damage down, never to zero** (user chose this over my
  "immune while defended"). Their version is better: full immunity lets one parked
  champion veto a siege forever and the match stalls. Scaling guarantees progress, so
  defence buys rounds rather than denying outright — and after an ace, nothing is
  adjacent, so structures fall at full rate. That is the ace-capitalisation answer.
- **Minion waves deferred to Vertical Slice.** MVP must not depend on them; they plug
  in later as the tempo layer (windows when a tower is takeable). Nothing else
  currently supplies rhythm — recorded as Map open question 8.

**Geometric finding — the board already had lanes.** Exactly **two** straight
front-to-front lines exist on a radius-4 hexagon: `(0,-4)` going `(0,+1)`, and
`(4,-4)` going `(-1,+1)`. They cross at the centre tower and **carry all five towers**
— unplanned; the towers were placed for symmetry and landed on the two natural axes.
A square map affords three lanes, a hexagon exactly two, which suits five champions
better anyway: **two per lane plus a jungler**, so every lane is a pair. Lanes are
minion routes only — champions are never obliged to use them, so they carry none of
the single-file cost that movement corridors do.

**Next:** re-run `applicability` with the real tower positions as contested points
(acceptance criterion 11, blocking before abilities), then Movement & Targeting, then
unpark the champion/ability schema.

---

**Earlier: champion/ability schema GDD PARKED; process changed.**

User pushback, 2026-08-16, and it was correct on every point:

1. **Wrong order.** The stat model was designed before the board. Board size sets
   champion density → density sets pattern applicability → applicability prices ability
   power. Every number in the schema silently encoded a radius-4 board nobody chose.
   Verified by sweeping radii: a tier-4 5-hex pattern is 41.8% applicable at radius 3
   and 23.9% at radius 6, a spread wider than the ±0.08 conformance band built on it.
   Only the qualitative findings survive (melee never most applicable; tier-3 reach
   matters and area does not; tier-4 hex count monotonic).
2. **Too many stats, some premature.** Six was not derived. `SPD` presupposes Movement
   & Targeting, `RES` presupposes Status Effects — neither designed. Both are deletion
   candidates, not tuning candidates.
3. **Kit shapes too narrow.** *Decision: initiative total is a currency traded against
   stats* — `[1,2,2,4]` (sum 9) buys a stat bonus, sum 11 pays for it. Replaces the
   "exactly three shapes" rule, which is now obsolete.
4. **Prototypes ran on unstated assumptions** (a damage model nobody had agreed).

**Process change in force — see "Collaboration Process" below.**

**Board decisions already given:** jungle · towers/defended points · symmetric and
chess-like. Lane corridors *not* selected.

Preceded by a real measurement rather than an assumption. `tools/Augury.Tools` (the
Balance Simulation Harness ADR-0001 says exists from day one) sampled 100,000
actor-positions per placement model to close ladder Open Question 2:

- **Melee is not the baseline.** A range-1 ability reaches an enemy in 50% of contested
  board states, not the 100% ladder F4 assumed. Tier-1 abilities must be ranged.
- **Rotatable pattern size is irrelevant; reach is the dial.** 2-hex line and 3-hex
  wedge measure identically (68.7%) — six facings make area nearly meaningless.
- **Fixed pattern size matters linearly**, ≈6pp per hex → 5 hexes is the tier-4 target.

`M` revised `[1.0, 1.3, 2.2, 4.4]` → `[1.0, 1.3, 2.0, 4.0]`; effective value now flat
within ±2%. The curve barely moved — the shape was right, the top tiers ~10% hot.

**Key schema decisions:** six permille stats (VIT/POW/ARM/RCH/SPD/RES), split into
continuous (drift invisibly) and threshold (floored, snap visibly); the **cross rule**
(an ability never molds the stat it scales from, so a kit is a rotation not a button);
and an **initiative budget** of 10 with ≤2 per tier, which admits exactly three kit
shapes — Ladder `1-2-3-4`, Anvil `1-1-4-4`, Vice `2-2-3-3`. The Vice cannot answer at
initiative 1 at all, which is a deliberate exploitable weakness.

**Owed to ADR-0007:** `AbilityDef` needs `MoldUp`/`MoldDown` pairs plus `ScalesFrom` to
enforce the cross rule. Additive change, no implementation exists yet, but it must land
before content authoring.

---

**Earlier: test infrastructure scaffolded AND VERIFIED — 32 tests passing.**

I installed the .NET 8 SDK in this container (extracted from
packages.microsoft.com's jammy repo; `dot.net` and the Azure CDN are blocked by
the egress proxy, `packages.microsoft.com` is not). So this scaffold is
**compiled and run**, not written blind.

```
Augury.sln
src/Augury.Sim/          Arith.cs, HexCoord.cs — specified verbatim by ADR-0002/0005
tests/unit/Augury.Sim.Tests/
    Architecture/ManifestRuleTests.cs   the manifest's 🤖 rules, executable
    Foundation/ArithTests.cs            ADR-0002 validation criteria
    Foundation/HexCoordTests.cs         ADR-0005 validation criteria
tests/integration/       gdUnit4, empty until Augury.Game exists
tests/smoke/critical-paths.md
tests/evidence/
.github/workflows/tests.yml   sim-tests · manifest-guard · integration (if: false)
```

**Three real defects the verification caught** — all would have shipped as
"looks right" documentation:
1. `ReadOnlySpan<HexCoord>` cannot be backed by a collection expression (CS9203)
2. Missing XML doc comments are build **errors** here — `TreatWarningsAsErrors`
   plus `GenerateDocumentationFile` enforces `coding-standards.md` mechanically
3. The prototype-isolation CI guard false-positived on a doc comment *citing*
   `prototypes/initiative-ladder/REPORT.md`; narrowed to real references

**Two repo-level fixes:**
- `.gitignore` excluded `*.csproj` and `*.sln` (Unity-oriented, since Unity
  regenerates them). Ours are hand-authored source — `Augury.Sim.csproj` is
  where ADR-0001's boundary is declared. Un-ignored, with a note.
- `coding-standards.md`'s Godot CI command assumed GDScript. Now names
  `dotnet test Augury.sln` for this project. (The long-standing follow-up.)

**Not verified:** anything needing Godot. No engine in this container, so the
gdUnit4 job is `if: false` and the integration directory is empty by design.

**Next:** `/design-system` #2 (Champion Data & Stat Model + Ability Definition
Schema) · `/architecture-decision` ADR-0008 (AI search) · `/design-review` on
the ladder GDD in a **fresh session**.

## Collaboration Process

`.claude/rules/design-docs.md` already required section-by-section authoring with
approval between sections. It was not followed for the ladder GDD or the schema GDD —
both were written in one pass. The cause was over-correcting on earlier feedback about
asking the same question twice: the fix for *asking twice* was taken as *not asking*.

**The four rules that apply from here:**

1. **Decisions listed before drafting.** Before writing any GDD section, state the
   decisions it will make, split into **yours** (vision, feel, identity) and **mine**
   (derivable from decisions already made, or purely technical). Only the vision list
   gets asked, once, in one batch.
2. **Assumptions are marked in the document, never buried.** Any value not derived from
   a stated decision carries a visible marker so it can be scanned for and challenged.
3. **Prototypes state their model before they run.** The ladder prototype used a damage
   model nobody had agreed to. The model gets confirmed *before* CPU is spent, so the
   numbers describe the intended game.
4. **Dependency order is enforced, not assumed.** If a GDD needs a quantity another
   undesigned GDD owns, that is a blocker — write the dependency first or mark the
   number provisional. This is what parking the schema GDD is for.

Challenging a written document is always cheap. Markdown costs nothing to revise;
what gets expensive is code and content built on it, and none exists yet.

## Project

**AUGURY (working title)** — turn-based tactics / MOBA hybrid. One player commands
five drafted champions on a hex map; abilities carry an **initiative** value and
both sides trade down a descending response ladder.

- Phase: **Concept → Systems Design**
- Engine: Godot 4.6, C# (.NET 8+), PC
- Review mode: `lean` (director gates fire only at `/gate-check`)

## Progress

- [x] `/start` — onboarding, review mode set to lean
- [x] `/brainstorm` — `design/gdd/game-concept.md` written
- [x] `/setup-engine` — Godot 4.6 + C# pinned; `CLAUDE.md`, `technical-preferences.md`, `VERSION.md` updated
- [x] `/map-systems` — `design/gdd/systems-index.md` written (33 systems, 21 MVP)
- [x] `/prototype initiative-ladder` — PROCEED; 3 rounds, 2 harness bugs found and fixed
- [x] `/design-system initiative-ladder` — complete; Open Question 2 now closed
- [x] `/create-architecture` — master blueprint + ADR-0001..0007 accepted
- [x] `/test-setup` — 32 tests passing, CI in `.github/workflows/tests.yml`
- [x] Applicability measurement — `tools/Augury.Tools`, ladder F3/F4 revised
- [x] `/design-system` champion + ability schema
- [ ] `/design-review` on both GDDs — **next, fresh session**
- [ ] ADR-0007 amendment for the mold pair; ADR-0008 (AI search); ADR-0009 (replay)
- [ ] Remaining 11 design-owned MVP GDDs

## Key Decisions

- **Initiative ladder**: an ability at initiative N may be answered by any ability
  at initiative ≤ N, from any champion; the ladder descends until someone passes.
- **Round order**: ladder exchange → death check → status resolution. A champion
  taken to ≤0 HP by statuses survives one more round, debuffed, before dying.
- **Molding**: ability use applies permanent in-match stat changes. Champions
  arrive from the draft unfinished.
- **Three phases**: draft → opening phase (one ability per champion, starts on
  cooldown) → tactical combat. ~25 turns, 10–15 minutes.
- **Turn structure**: alternating, not simultaneous — the ladder supplies the
  collision feel that simultaneous commit would have provided.
- **Action Economy merged into the Initiative Ladder GDD** — they define each other.
- **Physics is presentation-only.** Determinism is a pillar requirement and a
  precondition for post-launch async PvP.
- **Tests split**: xUnit for engine-independent simulation, gdUnit4 for engine
  integration. If a sim test needs Godot to run, the architecture has drifted.
- Items and blitz clock deferred to Vertical Slice — validate one economy first.

## Files Written This Session

| File | Purpose |
|------|---------|
| `design/gdd/game-concept.md` | Full game concept: pitch, pillars, loop, MVP, scope, risks |
| `design/gdd/systems-index.md` | 33 systems with dependencies, priorities, design order, risks |
| `CLAUDE.md` | Technology stack → Godot 4.6 / C# |
| `.claude/docs/technical-preferences.md` | Engine, input, naming, budgets, testing, routing |
| `docs/engine-reference/godot/VERSION.md` | Corrected LLM cutoff (May 2026); 4.6 risk HIGH → MEDIUM |
| `production/review-mode.txt` | `lean` |

## Open Questions

- ~~Action economy~~ **CLOSED**: one action per champion per **half**. Measured —
  per-round left a team with zero available champions entering the half it opens in
  61.8% of rounds, and ended 62% of halves by exhaustion rather than choice.
- Movement's relationship to the ladder — action with initiative, separate phase, or free?
  *(Assumed initiative-1 costing the champion's action; still unverified.)*
- **Applicability geometry** — F4 assumes tier-4 fixed patterns are legal in ~30% of
  board states. That is an assumption about hex geometry, not a decision.
- Equal-initiative exchanges — what prevents ping-pong at initiative 1?
- Negative HP and the dying round — what debuffs apply, can healing rescue?
- Map composition — lanes and minions unresolved (recommendation: cut minions,
  keep lane-shaped corridors plus a jungle).
- Points economy — what awards points, at what rate, to what total.
- Snowball risk — scaling respawns + points race + 15-minute match, with no runway.

## Highest Risks

1. **AI Opponent** — mandatory for ship, hardest technical problem in the project.
2. ~~Initiative Ladder~~ — **de-risked.** Prototyped over 3 rounds; rules hold.
3. **Determinism** — expensive to retrofit, gates PvP permanently.
4. **Molding legibility** — Pillars 1 and 5 collide; drift may read as noise.

## Follow-Ups Noted

- `.claude/docs/coding-standards.md` hardcodes the Godot CI command as
  `godot --headless --script tests/gdunit4_runner.gd`, which assumes GDScript.
  With C# it needs `dotnet test` for the sim plus a gdUnit4 pass. Fix in `/test-setup`.
