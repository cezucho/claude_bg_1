# Browser client — walkthrough evidence

> **Date**: 2026-09-28 · **Story type**: UI (advisory gate) · **Build**: `src/Augury.Web`

Driven in headless Chromium (Playwright) against a live server; no console or page errors
in any run.

| Check | Result |
|---|---|
| Draft overlay: roster by role, pick buttons only on the human's turn, mode switch | Pass |
| Opening: hovering a playable ability previews its three instructions as arrows and ghosts; click plays it; fallback markers appear when nothing fits | Pass |
| Basics: clicking a champion shows green move and gold attack markers; hover previews | Pass |
| Ladder: hovering an ability shows reach and targets; selecting shows markers; tier-3 shows one arrow per legal facing; hover previews exact damage | Pass |
| Chains, Pass and Decline listed with previews | Pass |
| Play as B (right panel, top of the map) | Pass |
| Watch mode to match end; end card; nexus destruction and score endings seen | Pass |
| Layout at 1920×1080, 1600×900, 1366×768 | Pass (at 768 px tall the right panel scrolls for its fifth card) |

Screenshots: `browser-client-ladder-1920.png` (a tier-3 ability selected: two facing
arrows, one hovered, its cells and the exact damage previewed), `browser-client-basics-1600.png` (playing team B in the basics phase).
