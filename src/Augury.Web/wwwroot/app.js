// AUGURY — broadcast-style browser client. Presentation only: every rule lives in the
// server's simulation. The server sends the state, the legal commands, and an exact
// preview of each; this page draws them and sends back the index the player chose.
'use strict';

const ROLES = ['Top', 'Jungle', 'Mid', 'Bottom', 'Support'];
const SVGNS = 'http://www.w3.org/2000/svg';
const HEX = 30;                       // hex radius in map units
const SQ3 = Math.sqrt(3);

let V = null;                         // latest view from the server
let sel = null;                       // {type:'ability', champ, ability} | {type:'basic', champ}
let paused = false;
let aiTimer = null;
let seenEvents = -1;
let busy = false;

// ───────────────────────────── server ─────────────────────────────

async function api(method, path) {
  if (busy) return;
  busy = true;
  try {
    const res = await fetch(path, { method });
    const view = await res.json();
    if (view.error) flashBanner(view.error);
    V = view;
    sel = null;
    render();
  } catch (e) {
    flashBanner('Lost contact with the server — is it still running?');
  } finally {
    busy = false;
  }
  scheduleAi();
}

function play(index) { clearTimeout(aiTimer); api('POST', `/api/play/${index}`); }

function scheduleAi() {
  clearTimeout(aiTimer);
  if (!V || V.humanTurn || V.phase === 'MatchOver' || paused) return;
  const delay = V.phase === 'Draft' ? 350 : +document.getElementById('speed').value;
  aiTimer = setTimeout(() => api('POST', '/api/step'), delay);
}

// ───────────────────────────── geometry ─────────────────────────────

// Pointy-top axial layout; team A (forward = +r) plays up the screen.
function px(q, r) { return [HEX * SQ3 * (q + r / 2), -HEX * 1.5 * r]; }
function hexPath(q, r, scale = 1) {
  const [cx, cy] = px(q, r);
  const pts = [];
  for (let k = 0; k < 6; k++) {
    const a = Math.PI / 180 * (60 * k - 30);
    pts.push(`${(cx + HEX * scale * Math.cos(a)).toFixed(1)},${(cy + HEX * scale * Math.sin(a)).toFixed(1)}`);
  }
  return pts.join(' ');
}
const key = (q, r) => `${q},${r}`;

function el(tag, attrs = {}, parent = null) {
  const e = document.createElementNS(SVGNS, tag);
  for (const [k, v] of Object.entries(attrs)) e.setAttribute(k, v);
  if (parent) parent.appendChild(e);
  return e;
}
function h(html) { const t = document.createElement('template'); t.innerHTML = html.trim(); return t.content.firstElementChild; }
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

// ───────────────────────────── icons & portraits ─────────────────────────────

function roleIcon(role, color = '#e8c46a') {
  const s = `stroke="${color}" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"`;
  const body = {
    Top: `<path ${s} d="M5 19 L17 7 L19 5 M16 4 L20 4 L20 8 M7 13 L11 17 M4 20 L6 18"/>`,
    Jungle: `<path ${s} d="M6 20 C6 12 9 7 14 4 M11 20 C11 13 13 9 18 6 M16 20 C16 15 17 12 20 10"/>`,
    Mid: `<path ${s} d="M12 3 L20 12 L12 21 L4 12 Z M12 8 L16 12 L12 16 L8 12 Z"/>`,
    Bottom: `<circle ${s} cx="12" cy="12" r="7"/><path ${s} d="M12 2 V7 M12 17 V22 M2 12 H7 M17 12 H22"/>`,
    Support: `<path ${s} d="M12 3 L19 6 V12 C19 16 16 19 12 21 C8 19 5 16 5 12 V6 Z M12 9 V15 M9 12 H15"/>`,
  }[role] || '';
  return `<svg viewBox="0 0 24 24">${body}</svg>`;
}

function hash(s) { let x = 7; for (const c of s) x = (x * 31 + c.charCodeAt(0)) >>> 0; return x; }

// A generated emblem stands in for champion art: team-coloured field, an id-seeded
// geometric sigil, the champion's glyph.
function portrait(id, glyph, team) {
  const x = hash(id);
  const hue = x % 360;
  const sides = 3 + (x >> 4) % 4;
  const rot = (x >> 8) % 60;
  const base = team === 'A' ? ['#1d5f9c', '#0a1f38'] : team === 'B' ? ['#9c2433', '#360a12'] : ['#4a5470', '#141a2c'];
  const pts = [];
  for (let k = 0; k < sides; k++) {
    const a = (Math.PI * 2 * k) / sides + rot * Math.PI / 180;
    pts.push(`${(35 + 25 * Math.cos(a)).toFixed(1)},${(35 + 25 * Math.sin(a)).toFixed(1)}`);
  }
  const gid = `g${id}${team}`;
  return `<svg viewBox="0 0 70 70">
    <defs><radialGradient id="${gid}" cx="50%" cy="35%" r="75%"><stop offset="0" stop-color="${base[0]}"/><stop offset="1" stop-color="${base[1]}"/></radialGradient></defs>
    <rect width="70" height="70" fill="url(#${gid})"/>
    <polygon points="${pts.join(' ')}" fill="hsla(${hue},70%,60%,.22)" stroke="hsla(${hue},80%,70%,.7)" stroke-width="2"/>
    <circle cx="35" cy="35" r="13" fill="rgba(0,0,0,.35)"/>
    <text x="35" y="36" text-anchor="middle" dominant-baseline="central" font-size="24" font-weight="900" fill="#fff">${esc(glyph)}</text>
  </svg>`;
}

// ───────────────────────────── legal-command indexes ─────────────────────────────

function legalFor(pred) { return V.legal.filter(pred); }
function abilityCommands(champ, ability) {
  return legalFor(c => (c.kind === 'Ability' && c.champ2 < 0 || c.kind === 'OpeningPlay') && c.champ === champ && c.ability === ability);
}
function basicCommands(champ) { return legalFor(c => (c.kind === 'BasicMove' || c.kind === 'BasicAttack') && c.champ === champ); }
function champById(slot) { return V.champions[slot]; }
function champAt(q, r) { return V.champions.find(c => c.drafted && c.presence === 'board' && c.q === q && c.r === r); }

// ───────────────────────────── render ─────────────────────────────

function render() {
  if (!V) return;
  hideTip();
  renderTop();
  renderPanel('A');
  renderPanel('B');
  renderMap();
  renderActions();
  renderFeed();
  renderOverlay();
  clearPreview();
  document.getElementById('undo').disabled = !V.canUndo;
  document.querySelectorAll('#toolbar [data-mode]').forEach(b => b.classList.toggle('on', b.dataset.mode === V.mode));
}

function renderTop() {
  for (const t of ['A', 'B']) {
    const i = t === 'A' ? 0 : 1;
    const nx = V.nexus[i];
    const hp = Math.max(0, nx.hp);
    const towers = V.towers.filter(w => w.owner === t).length;
    const incoming = V.towers.filter(w => w.owner !== t && w.owner !== 'None').length * V.race.towerSiege;
    const who = V.humans.includes(t) ? (V.humans.length === 2 ? `Player ${t === 'A' ? 1 : 2}` : 'You') : 'AI';
    const pct = Math.min(100, 100 * hp / nx.max);
    document.getElementById(`top-${t}`).innerHTML = `
      <div class="team-name"><b>Team ${t}</b>${who} · ${t === 'A' ? 'bottom' : 'top'}</div>
      <div class="big-score" title="Nexus HP">${hp}</div>
      <div class="stat-col">
        <div class="score-sub">nexus HP ${nx.open ? '<span class="open-badge">OPEN</span>' : '· closed to direct attack'}</div>
        <div class="score-bar nexus-hp"><i style="width:${pct}%;background:var(--${t})"></i></div>
        <div class="nexus-line plain">Siege at round close: <b class="${incoming ? 'bad-txt' : ''}">−${incoming}</b></div>
        <div class="nexus-line">Towers <span class="towers-line">${V.towers.map(w => `<i class="tw-pip" style="background:${w.owner === t ? `var(--${t})` : '#2a3350'}"></i>`).join('')}</span> ${towers} held</div>
      </div>`;
  }

  const phaseName = { Draft: 'Draft', SpellPick: 'Summoner spells', Opening: 'Opening', Basic: 'Basics', Ladder: 'Ladder', LastWord: 'Last Word', MatchOver: 'Match over' }[V.phase];
  const round = V.round > 0 ? `Round ${V.round} / ${V.race.roundCap} · Half ${V.half}` : 'Pre-game';
  let turn = '';
  if (V.phase !== 'MatchOver') {
    const human = V.humanTurn;
    turn = `<div class="turn-line"><b style="background:var(--${V.active})">▶ ${V.active}</b> ${human ? (V.humans.length === 2 ? 'to act' : 'your turn') : 'AI is thinking…'}</div>`;
  } else {
    turn = `<div class="turn-line">${V.winner === 'None' ? 'Draw' : `<b style="background:var(--${V.winner})">${V.winner} wins</b>`} by ${V.endReason}</div>`;
  }
  let extra = '';
  if (V.phase === 'Ladder' || V.phase === 'LastWord') {
    const pips = [1, 2, 3, 4].map(n => `<div class="ceil-pip ${n <= V.ceiling ? 'lit' : ''}" style="${n <= V.ceiling ? `background:var(--t${n})` : ''}">${n}</div>`).join('');
    extra = `<div class="ceiling"><span>ceiling</span>${pips}</div>`;
  } else if (V.phase === 'Basic') {
    extra = `<div class="ceiling"><span>basics</span><span class="A-c">A ${V.basics.a}/${V.basics.per}</span><span class="B-c">B ${V.basics.b}/${V.basics.per}</span></div>`;
  }
  document.getElementById('top-centre').innerHTML = `<div class="round-line">${round}</div><div class="phase-line">${phaseName}</div>${turn}${extra}`;
}

function renderPanel(team) {
  const panel = document.getElementById(`panel-${team}`);
  panel.innerHTML = '';
  const first = team === 'A' ? 0 : 5;
  for (let r = 0; r < 5; r++) {
    const c = V.champions[first + r];
    panel.appendChild(card(c));
  }
}

function card(c) {
  if (!c.drafted) {
    return h(`<div class="card undrafted"><div class="portrait">${portrait('?', '?', c.team)}<div class="role-badge">${roleIcon(c.role)}</div></div>
      <div class="info"><div class="name-line"><b>—</b><span class="role-txt">${c.role}</span></div><div class="statline">not drafted yet</div></div></div>`);
  }
  const basics = V.humanTurn && V.phase === 'Basic' ? basicCommands(c.slot) : [];
  const cls = ['card'];
  if (c.presence === 'dead') cls.push('dead');
  if (c.acted && (V.phase === 'Ladder' || V.phase === 'LastWord')) cls.push('acted');
  if (basics.length) cls.push('selectable');
  if (sel && sel.champ === c.slot) cls.push('selected');
  if (isLastActor(c.slot)) cls.push('last-actor');

  const hpPct = c.presence === 'dead' ? 0 : Math.max(0, 100 * c.hp / c.maxHp);
  const shPct = Math.min(100 - hpPct, 100 * c.shield / c.maxHp);
  const chips = [];
  if (c.presence === 'dead') chips.push(`<span class="chip">dead · ${respawnText(c)}</span>`);
  if (c.presence === 'spawn') chips.push(`<span class="chip">in spawn</span>`);
  if (c.dying) chips.push(`<span class="chip dying">dying · half power</span>`);
  if (c.poison) chips.push(`<span class="chip poison">poison ${c.poison.amount}×${c.poison.rounds}</span>`);
  for (const st of c.status || []) chips.push(`<span class="chip st-${st.kind}" title="${esc(st.text)}">${esc(st.label)}</span>`);
  if (c.shield > 0) chips.push(`<span class="chip shield">shield ${c.shield}</span>`);
  if (c.acted && (V.phase === 'Ladder' || V.phase === 'LastWord')) chips.push(`<span class="chip acted">acted</span>`);
  if (c.basicUsed && V.phase === 'Basic') chips.push(`<span class="chip acted">basic used</span>`);
  if (c.openingDone && V.phase === 'Opening') chips.push(`<span class="chip acted">opened</span>`);

  const pow = (c.stats.powNow / 1000).toFixed(2);
  const node = h(`<div class="${cls.join(' ')}" data-slot="${c.slot}">
    <div class="portrait">${portrait(c.id, c.glyph, c.team)}<div class="role-badge">${roleIcon(c.role)}</div>
      ${c.presence === 'dead' ? `<div class="dead-mark">☠</div>` : ''}</div>
    <div class="info">
      <div class="name-line"><b>${esc(c.name)}</b><span class="role-txt">${c.role}</span></div>
      <div class="hpbar"><div class="hp" style="width:${hpPct}%"></div><div class="sh" style="left:${hpPct}%;width:${shPct}%"></div>
        <div class="txt">${c.presence === 'dead' ? 'DEAD' : `${c.hp} / ${c.maxHp}`}</div></div>
      <div class="statline">POW <b>${pow}</b> · ARM <b>${c.stats.arm}</b> · RCH <b>${c.stats.rch}</b> · SPD <b>${c.stats.spd}</b></div>
      <div class="chips">${chips.join('')}</div>
      <div class="abilities"></div>
    </div>
  </div>`);

  const abs = node.querySelector('.abilities');
  c.abilities.forEach((a, i) => abs.appendChild(abilityIcon(c, a, i)));

  node.addEventListener('mouseenter', () => { hotChamp(c.slot, true); });
  node.addEventListener('mouseleave', () => { hotChamp(c.slot, false); });
  node.querySelector('.portrait').addEventListener('mouseenter', e => showTip(e, champTip(c)));
  node.querySelector('.portrait').addEventListener('mousemove', moveTip);
  node.querySelector('.portrait').addEventListener('mouseleave', hideTip);
  if (basics.length) node.addEventListener('click', e => { if (!e.target.closest('.ab')) selectBasic(c.slot); });
  return node;
}

function respawnText(c) { return c.respawnIn === 0 ? 'respawns at this round close' : `respawns in ${c.respawnIn + 1} round closes`; }

function abilityIcon(c, a, i) {
  const humanOwns = V.humanTurn && V.active === c.team;
  const cls = ['ab', `t${a.init}`, a.state];
  if (sel && sel.type === 'ability' && sel.champ === c.slot && sel.ability === i) cls.push('selected');
  if (V.last && ((V.last.champ === c.slot && V.last.ability === i) || (V.last.champ2 === c.slot && V.last.ability2 === i))) cls.push('last-used');
  const sig = a.printedSigil
    ? `<span class="sig">${a.printedSigil}</span>`
    : a.slotSigil ? `<span class="sig slot ${a.activeSigils.includes(a.slotSigil) ? 'active' : ''}">${a.slotSigil}</span>` : '';
  if (a.spell) cls.push('spell');
  if (a.hidden) cls.push('hidden-spell');
  const shown = V.phase === 'Opening' && a.openingName !== a.name ? a.openingName : a.name;
  const node = h(`<div class="${cls.join(' ')}">
    <span class="key">${a.key}</span>${sig}<span class="init">${a.hidden ? '?' : a.init}</span>
    <span class="nm">${esc(shown)}</span>
    ${a.cd > 0 ? `<div class="cdo">${a.cd}</div>` : ''}
  </div>`);
  node.addEventListener('mouseenter', e => { showTip(e, abilityTip(c, a), 'with-dia'); if (!sel) showAbilityHover(c, a, i); });
  node.addEventListener('mousemove', moveTip);
  node.addEventListener('mouseleave', () => { hideTip(); if (!sel) { clearHighlights(); clearPreview(); } });
  node.addEventListener('click', () => {
    if (!humanOwns || a.state !== 'usable') return;
    const cmds = abilityCommands(c.slot, i);
    if (V.phase === 'Opening') { if (cmds.length) play(cmds[0].i); return; }
    if (cmds.length === 1 && cmds[0].click.length === 0) { play(cmds[0].i); return; }
    selectAbility(c.slot, i);
  });
  return node;
}

// ───────────────────────────── map ─────────────────────────────

let layers = {};

function renderMap() {
  const svg = document.getElementById('map');
  svg.innerHTML = '';
  const all = [...V.hexes, ...V.spawns];
  const xs = all.map(p => px(p.q, p.r)[0]), ys = all.map(p => px(p.q, p.r)[1]);
  const pad = HEX * 1.3;
  svg.setAttribute('viewBox', `${Math.min(...xs) - pad} ${Math.min(...ys) - pad} ${Math.max(...xs) - Math.min(...xs) + 2 * pad} ${Math.max(...ys) - Math.min(...ys) + 2 * pad}`);

  const defs = el('defs', {}, svg);
  const m = el('marker', { id: 'arrowhead', viewBox: '0 0 10 10', refX: 8, refY: 5, markerWidth: 5, markerHeight: 5, orient: 'auto-start-reverse' }, defs);
  el('path', { d: 'M0 0 L10 5 L0 10 z', fill: '#fff' }, m);

  layers = {};
  for (const name of ['tiles', 'lastUnder', 'hl', 'structures', 'tokens', 'last', 'preview', 'markers']) layers[name] = el('g', { class: name }, svg);
  const lm = el('marker', { id: 'arrowhead-last', viewBox: '0 0 10 10', refX: 8, refY: 5, markerWidth: 5, markerHeight: 5, orient: 'auto-start-reverse' }, defs);
  el('path', { d: 'M0 0 L10 5 L0 10 z', fill: 'var(--last)' }, lm);

  const nexusOf = {};
  for (const n of V.nexus) for (const [q, r] of n.hexes) nexusOf[key(q, r)] = n.team;
  for (const t of V.hexes) {
    const nx = nexusOf[key(t.q, t.r)];
    const poly = el('polygon', { points: hexPath(t.q, t.r), class: `hex ${nx ? 'nexus' + nx : t.zone}` }, layers.tiles);
    poly.addEventListener('mouseenter', e => hexHover(e, t.q, t.r));
    poly.addEventListener('mousemove', moveTip);
    poly.addEventListener('mouseleave', hideTip);
    const [cx, cy] = px(t.q, t.r);
    el('text', { x: cx, y: cy + HEX * 0.72, class: 'coord' }, layers.tiles).textContent = `${t.q},${t.r}`;
  }
  for (const s of V.spawns) {
    el('polygon', { points: hexPath(s.q, s.r, 0.9), class: 'hex spawn' }, layers.tiles);
  }
  for (const n of V.nexus) {
    const mid = n.hexes[1];
    const [cx, cy] = px(mid[0], mid[1]);
    const t = el('text', { x: cx, y: cy + (n.team === 'A' ? HEX * 1.25 : -HEX * 1.05), class: 'nexus-lbl', fill: `var(--${n.team})` }, layers.tiles);
    t.textContent = `NEXUS ${n.team} · ${Math.max(0, n.hp)}${n.hp <= 0 ? ' · DESTROYED' : n.open ? ' · OPEN' : ''}`;
  }

  for (const b of V.beacons) {
    const [cx, cy] = px(b.q, b.r);
    el('circle', { cx, cy, r: HEX * SQ3 * 1.08, class: `beacon-aura ${b.team}` }, layers.structures);
    const g = el('g', {}, layers.structures);
    const bx = cx - HEX * 0.52, by = cy - HEX * 0.5;
    el('polygon', { points: `${bx},${by - 8} ${bx + 7},${by} ${bx},${by + 8} ${bx - 7},${by}`, class: `beacon ${b.team}` }, g);
    el('text', { x: bx, y: by, class: 'beacon-txt' }, g).textContent = b.sigil;
    g.addEventListener('mouseenter', e => showTip(e, `<h4 class="${b.team}-c">Beacon ${b.sigil} (team ${b.team})</h4><div class="tt-row">Lights slot sigil ${b.sigil} for friendly champions within 1 hex.</div><div class="tt-dim">Durability ${b.durability}: broken by enemy basic attacks.</div>`));
    g.addEventListener('mousemove', moveTip);
    g.addEventListener('mouseleave', hideTip);
  }

  for (const w of V.walls || []) {
    const g = el('g', { class: 'wall' }, layers.structures);
    el('polygon', { points: hexPath(w.q, w.r, 0.82), class: 'wall-block' }, g);
    const [cx, cy] = px(w.q, w.r);
    el('text', { x: cx, y: cy + 4, class: 'wall-txt' }, g).textContent = `▦ ${w.rounds}`;
    g.addEventListener('mouseenter', e => showTip(e, `<h4>Wall</h4><div class="tt-row">Impassable: nothing may move, dash or be pushed into it.</div><div class="tt-dim">Falls after ${w.rounds} more round close${w.rounds === 1 ? '' : 's'}.</div>`));
    g.addEventListener('mousemove', moveTip);
    g.addEventListener('mouseleave', hideTip);
  }

  for (const t of V.towers) {
    const [cx, cy] = px(t.q, t.r);
    const g = el('g', {}, layers.structures);
    const w = 15, top = cy - 20;
    el('polygon', { points: `${cx - w},${cy + 12} ${cx + w},${cy + 12} ${cx + w - 4},${top + 8} ${cx + w},${top} ${cx + 5},${top} ${cx + 5},${top + 5} ${cx - 5},${top + 5} ${cx - 5},${top} ${cx - w},${top} ${cx - w + 4},${top + 8}`, class: `tower-body ${t.owner}` }, g);
    el('text', { x: cx, y: cy + 3, class: 'tower-hp' }, g).textContent = t.hp;
    g.addEventListener('mouseenter', e => showTip(e, towerTip(t)));
    g.addEventListener('mousemove', moveTip);
    g.addEventListener('mouseleave', hideTip);
  }

  for (const c of V.champions) {
    if (!c.drafted || c.presence === 'dead' || c.presence === 'draft') continue;
    drawToken(c);
  }

  if (V.phase === 'Opening' && V.humanTurn) {
    const fb = legalFor(x => x.kind === 'OpeningFallback');
    if (fb.length) drawMarkers(fb, 'fallback');
  }
  drawLast();
  if (sel) redrawSelection();
}

function drawToken(c) {
  const [cx, cy] = px(c.q, c.r);
  const cls = ['token', c.team];
  if (c.acted && (V.phase === 'Ladder' || V.phase === 'LastWord')) cls.push('acted');
  if (c.dying) cls.push('dying');
  if (c.presence === 'spawn') cls.push('spawn');
  const basics = V.humanTurn && V.phase === 'Basic' ? basicCommands(c.slot) : [];
  if (basics.length) cls.push('selectable');
  if (sel && sel.champ === c.slot) cls.push('selected');
  if (isLastActor(c.slot)) cls.push('last-actor');
  const g = el('g', { class: cls.join(' '), 'data-slot': c.slot }, layers.tokens);
  slideIn(g, c);
  el('circle', { cx, cy, r: HEX * 0.62, class: 'body' }, g);
  // The role, not the champion: every top looks the same, only the colour says whose.
  g.appendChild(roleGlyph(c.role, cx, cy, HEX * 0.78, '#fff'));
  const bw = HEX * 1.1, bx = cx - bw / 2, by = cy + HEX * 0.52;
  el('rect', { x: bx, y: by, width: bw, height: 5, class: 'tok-hp-bg', rx: 1 }, g);
  const hpw = bw * Math.max(0, c.hp) / c.maxHp;
  el('rect', { x: bx, y: by, width: hpw, height: 5, class: `tok-hp ${c.team}`, rx: 1 }, g);
  if (c.shield > 0) el('rect', { x: bx + hpw, y: by, width: Math.min(bw - hpw, bw * c.shield / c.maxHp), height: 5, class: 'tok-sh' }, g);
  // Statuses as small lettered pips above the token, so the board itself shows who is rooted or burning.
  const pips = [...(c.poison ? [{ kind: 'poison', label: 'poison' }] : []), ...(c.status || [])];
  pips.forEach((st, k) => {
    const sx = cx - (pips.length - 1) * 6 + k * 12, sy = cy - HEX * 0.72;
    el('circle', { cx: sx, cy: sy, r: 5.5, class: `st-pip st-${st.kind}` }, g);
    el('text', { x: sx, y: sy + 0.5, class: 'st-pip-txt' }, g).textContent = STATUS_LETTER[st.kind] || '?';
  });
  g.addEventListener('mouseenter', e => { showTip(e, champTip(c)); hotChamp(c.slot, true); });
  g.addEventListener('mousemove', moveTip);
  g.addEventListener('mouseleave', () => { hideTip(); hotChamp(c.slot, false); });
  if (basics.length) g.addEventListener('click', () => selectBasic(c.slot));
}

const STATUS_LETTER = { root: 'R', burn: 'B', mark: 'M', exhaust: 'X', unstop: 'U', wound: 'W', poison: 'P' };

function roleGlyph(role, cx, cy, size, color) {
  const g = document.createElementNS(SVGNS, 'g');
  g.setAttribute('class', 'role-glyph');
  g.innerHTML = roleIcon(role, color).replace('<svg', `<svg x="${cx - size / 2}" y="${cy - size / 2}" width="${size}" height="${size}"`);
  return g;
}

// ───────────────────────────── last action ─────────────────────────────
// The most recent action stays marked in one colour (magenta, --last) until the next:
// who acted, the hexes it covered, a line to what it hit, where anyone moved from,
// and every change in numbers. Moved champions slide from their old hex once.

let animatedAction = -1;

function isLastActor(slot) {
  return !!V.last && V.phase !== 'Draft' && V.phase !== 'SpellPick' && V.last.kind !== 'SpellPick' && (V.last.champ === slot || V.last.champ2 === slot);
}

function lastMove(slot) {
  if (!V.last || V.last.kind === 'Draft' || V.last.kind === 'SpellPick') return null;
  return V.last?.diff?.champs.find(ch => ch.slot === slot && ch.from && ch.to);
}

function slideIn(g, c) {
  const mv = V.last && V.last.id !== animatedAction ? lastMove(c.slot) : null;
  if (!mv) return;
  const [x1, y1] = px(mv.from[0], mv.from[1]), [x2, y2] = px(mv.to[0], mv.to[1]);
  g.style.transform = `translate(${x1 - x2}px, ${y1 - y2}px)`;
  requestAnimationFrame(() => requestAnimationFrame(() => {
    g.style.transition = 'transform .55s ease-out';
    g.style.transform = 'translate(0px, 0px)';
  }));
}

function drawLast() {
  const L = V.last;
  if (!L || L.kind === 'Draft' || L.kind === 'SpellPick' || V.phase === 'Draft' || V.phase === 'SpellPick') return;
  const under = layers.lastUnder, top = layers.last;
  for (const [q, r] of L.cells) el('polygon', { points: hexPath(q, r, 0.92), class: 'last-cell' }, under);

  // Where the actor stood when it acted, and a line to each single target.
  for (const [at, champ] of [[L.actorAt, L.champ], [L.actor2At, L.champ2]]) {
    if (!at || champ < 0) continue;
    const [ax, ay] = px(at[0], at[1]);
    el('circle', { cx: ax, cy: ay, r: HEX * 0.8, class: 'last-ring' }, under);
    if (champ === L.champ) {
      for (const [q, r] of L.focus) {
        const [tx, ty] = px(q, r);
        const d = Math.hypot(tx - ax, ty - ay) || 1, sh = HEX * 0.7;
        el('line', { x1: ax + (tx - ax) * sh / d, y1: ay + (ty - ay) * sh / d, x2: tx - (tx - ax) * sh / d, y2: ty - (ty - ay) * sh / d, class: 'last-beam' }, top);
      }
    }
  }

  const stack = {};
  const num = (q, r, text, cls) => {
    const k = key(q, r);
    const n = stack[k] = (stack[k] || 0) + 1;
    const [cx, cy] = px(q, r);
    el('text', { x: cx, y: cy - HEX * 0.78 - (n - 1) * 14, class: `pv-num last-num ${cls}` }, top).textContent = text;
  };
  for (const ch of L.diff.champs) {
    const c = champById(ch.slot);
    if (ch.from && ch.to && (ch.from[0] !== ch.to[0] || ch.from[1] !== ch.to[1])) {
      const [x1, y1] = px(ch.from[0], ch.from[1]), [x2, y2] = px(ch.to[0], ch.to[1]);
      const d = Math.hypot(x2 - x1, y2 - y1), sh = HEX * 0.65;
      el('circle', { cx: x1, cy: y1, r: HEX * 0.55, class: 'last-ghost' }, under);
      el('line', { x1: x1 + (x2 - x1) * sh / d, y1: y1 + (y2 - y1) * sh / d, x2: x2 - (x2 - x1) * sh / d, y2: y2 - (y2 - y1) * sh / d, class: 'last-arrow' }, top);
    }
    const at = ch.to || (c.presence === 'board' ? [c.q, c.r] : ch.from);
    if (!at) continue;
    if (ch.dHp < 0) num(at[0], at[1], `${ch.dHp}`, 'bad');
    if (ch.dHp > 0) num(at[0], at[1], `+${ch.dHp}`, 'good');
    if (ch.dShield > 0) num(at[0], at[1], `+${ch.dShield} shield`, 'shield');
    if (ch.dies) num(at[0], at[1], 'KILL', 'bad');
    if (ch.dying) num(at[0], at[1], 'DYING', 'bad');
    if (ch.poisoned) num(at[0], at[1], 'poison', 'info');
    for (const st of ch.status || []) num(at[0], at[1], st, 'info');
  }
  for (const [q, r] of L.diff.walls || []) num(q, r, 'WALL', 'info');
  for (const t of L.diff.towers) {
    const tw = V.towers[t.i];
    num(tw.q, tw.r, t.captured ? `CAPTURED → ${t.owner}` : `${t.dHp}`, t.captured ? 'info' : 'bad');
  }
  L.diff.nexus.forEach((d, i) => { if (d < 0) { const [q, r] = V.nexus[i].hexes[1]; num(q, r, `NEXUS ${d}`, 'bad'); } });
  // A round closed: every held tower fired at the enemy nexus.
  for (const sl of L.siege) {
    const [x1, y1] = px(sl.from[0], sl.from[1]), [x2, y2] = px(sl.to[0], sl.to[1]);
    const d = Math.hypot(x2 - x1, y2 - y1), sh = HEX * 0.6;
    el('line', { x1: x1 + (x2 - x1) * sh / d, y1: y1 + (y2 - y1) * sh / d, x2: x2 - (x2 - x1) * sh / d, y2: y2 - (y2 - y1) * sh / d, class: `last-beam siege ${sl.team}` }, under);
  }
  animatedAction = L.id;
}

function setLastBanner() {
  const L = V.last;
  const b = document.getElementById('banner');
  if (!L || V.phase === 'Draft') { setBanner(lastActionText()); return; }
  const actor = L.champ >= 0 ? champById(L.champ) : null;
  b.innerHTML = `<span class="last-dot"></span> <b class="${L.team}-c">${L.team}</b>${actor ? ` · ${esc(actor.name)}` : ''} — ${esc(L.label.replace(/^[AB]:\w+ /, ''))}`;
}

function hotChamp(slot, on) {
  document.querySelectorAll(`.card[data-slot="${slot}"]`).forEach(n => n.classList.toggle('hot', on));
  document.querySelectorAll(`.token[data-slot="${slot}"]`).forEach(n => n.classList.toggle('hot', on));
}

function hexHover(e, q, r) {
  const c = champAt(q, r);
  if (c) return;
  const nx = V.nexus.find(n => n.hexes.some(([a, b]) => a === q && b === r));
  const zone = V.hexes.find(x => x.q === q && x.r === r)?.zone;
  const lines = [`<h4>Hex (${q},${r})</h4><div class="tt-sub">${nx ? `Nexus of team ${nx.team}` : zone}</div>`];
  if (nx) lines.push(`<div class="tt-row">${nx.hp}/${nx.max} HP. ${nx.open ? 'OPEN — can be damaged.' : 'Closed until team ' + nx.team + ' loses a home tower.'}</div>`);
  showTip(e, lines.join(''));
}

// ───────────────────────────── highlights, markers, previews ─────────────────────────────

function clearHighlights() { if (layers.hl) layers.hl.innerHTML = ''; if (layers.markers) layers.markers.innerHTML = ''; }
function hl(cells, cls) { for (const [q, r] of cells) el('polygon', { points: hexPath(q, r, 0.92), class: cls }, layers.hl); }

// Hovering an ability with nothing selected: reach, what it could hit, and — if it is
// playable now — every hex it can actually be played on.
function showAbilityHover(c, a, i) {
  clearHighlights();
  clearPreview();
  if (c.presence !== 'board') return;
  const mine = V.humanTurn && V.active === c.team;
  hl(a.reach, mine || V.humans.includes(c.team) ? 'hl-reach' : 'hl-enemy-reach');
  hl(a.targets, 'hl-target');
  if (V.phase === 'Opening') {
    const cmds = abilityCommands(c.slot, i);
    if (cmds.length) showPreview(cmds[0]);
  } else if (mine && a.state === 'usable') {
    const cmds = abilityCommands(c.slot, i);
    for (const cmd of cmds) hl(cmd.cells, cmd.kind === 'Ability' && isFriendly(c, a) ? 'hl-cells friendly' : 'hl-cells');
    if (cmds.length === 1) showPreview(cmds[0]);
    setBanner(`${c.name} · ${a.name}: click the ability, then a gold marker on the map.`);
  }
}

function isFriendly(c, a) { return !!a.friendly; }

function selectAbility(champ, ability) {
  sel = { type: 'ability', champ, ability };
  render();
}
function selectBasic(champ) {
  if (sel && sel.type === 'basic' && sel.champ === champ) { sel = null; render(); return; }
  sel = { type: 'basic', champ };
  render();
}

function redrawSelection() {
  clearHighlights();
  const c = champById(sel.champ);
  if (sel.type === 'ability') {
    const a = c.abilities[sel.ability];
    hl(a.reach, 'hl-reach');
    const cmds = abilityCommands(sel.champ, sel.ability);
    for (const cmd of cmds) hl(cmd.cells, isFriendly(c, a) ? 'hl-cells friendly' : 'hl-cells');
    drawMarkers(cmds, 'ability');
    setBanner(`${c.name} · ${a.name} — hover a gold marker to preview, click to play. Esc cancels.`);
  } else {
    const cmds = basicCommands(sel.champ);
    drawMarkers(cmds.filter(x => x.kind === 'BasicMove'), 'move');
    drawMarkers(cmds.filter(x => x.kind === 'BasicAttack'), 'attack');
    setBanner(`${c.name} — green: move there · gold: basic attack. Esc cancels.`);
  }
}

function drawMarkers(cmds, kind) {
  const used = new Set();
  for (const cmd of cmds) {
    for (const [q, r] of cmd.click) {
      const k = key(q, r);
      if (used.has(k)) continue;
      used.add(k);
      const [cx, cy] = px(q, r);
      const g = el('g', { class: `marker ${kind}` }, layers.markers);
      el('circle', { cx, cy, r: HEX * 0.8 }, g);
      if (cmd.facing >= 0) {
        const c = champById(cmd.champ);
        const [ox, oy] = px(c.q, c.r);
        const ang = Math.atan2(cy - oy, cx - ox);
        const tip = [cx + Math.cos(ang) * 12, cy + Math.sin(ang) * 12];
        const l = [cx + Math.cos(ang + 2.5) * 10, cy + Math.sin(ang + 2.5) * 10];
        const rr = [cx + Math.cos(ang - 2.5) * 10, cy + Math.sin(ang - 2.5) * 10];
        el('path', { d: `M${tip} L${l} L${rr} Z` }, g);
      }
      g.addEventListener('mouseenter', () => { showPreview(cmd); focusCells(cmd); });
      g.addEventListener('mouseleave', () => { clearPreview(); unfocusCells(); });
      g.addEventListener('click', ev => { ev.stopPropagation(); play(cmd.i); });
    }
  }
}

let focusGroup = null;
function focusCells(cmd) {
  unfocusCells();
  focusGroup = el('g', {}, layers.hl);
  for (const [q, r] of cmd.cells) el('polygon', { points: hexPath(q, r, 0.92), class: 'hl-cells', style: 'fill:rgba(255,255,255,.25);stroke:#fff' }, focusGroup);
}
function unfocusCells() { if (focusGroup) focusGroup.remove(); focusGroup = null; }

function clearPreview() {
  if (layers.preview) layers.preview.innerHTML = '';
  document.getElementById('map').classList.remove('previewing');
  const box = document.getElementById('preview');
  box.innerHTML = '<div class="muted">Hover an action — a marker, an ability, a chain, Pass — to see exactly what it does. The engine is deterministic, so the preview is the outcome.</div>';
}

function showPreview(cmd) {
  clearPreview();
  document.getElementById('map').classList.add('previewing');
  const p = cmd.preview;
  const box = document.getElementById('preview');
  if (!p) return;
  const lines = [`<div class="pv-head">${esc(cmd.label)}</div>`];
  for (const t of p.events) lines.push(`<div class="pv-line">${esc(t)}</div>`);
  if (p.more) lines.push(`<div class="pv-line muted">… and ${p.more} more</div>`);
  if (p.endsMatch) lines.push(`<div class="pv-line" style="color:var(--gold)">This ends the match.</div>`);
  box.innerHTML = lines.join('');

  const g = layers.preview;
  const stack = {};
  const num = (q, r, text, cls) => {
    const k = key(q, r);
    const n = stack[k] = (stack[k] || 0) + 1;
    const [cx, cy] = px(q, r);
    el('text', { x: cx, y: cy - HEX * 0.75 - (n - 1) * 14, class: `pv-num ${cls}` }, g).textContent = text;
  };
  for (const ch of p.champs) {
    const c = champById(ch.slot);
    if (ch.from && ch.to) {
      const [x1, y1] = px(ch.from[0], ch.from[1]);
      const [x2, y2] = px(ch.to[0], ch.to[1]);
      if (x1 !== x2 || y1 !== y2) {
        const d = Math.hypot(x2 - x1, y2 - y1), sh = HEX * 0.62;
        el('line', { x1: x1 + (x2 - x1) * sh / d, y1: y1 + (y2 - y1) * sh / d, x2: x2 - (x2 - x1) * sh / d, y2: y2 - (y2 - y1) * sh / d, class: 'pv-arrow' }, g);
        el('circle', { cx: x2, cy: y2, r: HEX * 0.62, class: 'pv-ghost', stroke: `var(--${c.team})` }, g);
        g.appendChild(roleGlyph(c.role, x2, y2, HEX * 0.7, 'rgba(255,255,255,.6)'));
      }
    }
    const at = ch.to || [c.q, c.r];
    if (ch.dHp < 0) num(at[0], at[1], `${ch.dHp}`, 'bad');
    if (ch.dHp > 0) num(at[0], at[1], `+${ch.dHp}`, 'good');
    if (ch.dShield > 0) num(at[0], at[1], `+${ch.dShield} shield`, 'shield');
    if (ch.dShield < 0 && ch.dHp === 0) num(at[0], at[1], `${ch.dShield} shield`, 'shield');
    if (ch.dies) num(c.q, c.r, 'KILL', 'bad');
    if (ch.dying) num(at[0], at[1], 'DYING', 'bad');
    if (ch.poisoned) num(at[0], at[1], 'poison', 'info');
    for (const st of ch.status || []) num(at[0], at[1], st, 'info');
  }
  for (const [q, r] of p.walls || []) {
    el('polygon', { points: hexPath(q, r, 0.82), class: 'wall-block', style: 'opacity:.6' }, g);
    num(q, r, 'WALL', 'info');
  }
  for (const t of p.towers) {
    const tw = V.towers[t.i];
    num(tw.q, tw.r, t.captured ? `CAPTURED → ${t.owner}` : `${t.dHp}`, t.captured ? 'info' : 'bad');
  }
  p.nexus.forEach((d, i) => {
    if (d < 0) { const [q, r] = V.nexus[i].hexes[1]; num(q, r, `NEXUS ${d}`, 'bad'); }
  });
  for (const b of p.beacons) {
    const [cx, cy] = px(b.q, b.r);
    const bx = cx - HEX * 0.52, by = cy - HEX * 0.5;
    el('polygon', { points: `${bx},${by - 8} ${bx + 7},${by} ${bx},${by + 8} ${bx - 7},${by}`, class: `beacon ${b.team}`, style: 'opacity:.7' }, g);
    el('text', { x: bx, y: by, class: 'beacon-txt' }, g).textContent = b.sigil;
  }
}

// ───────────────────────────── tooltips ─────────────────────────────

const tip = () => document.getElementById('tooltip');
function showTip(e, html, cls = '') { const t = tip(); t.className = cls; t.innerHTML = html; t.style.display = 'block'; moveTip(e); }
function moveTip(e) {
  const t = tip();
  const w = t.offsetWidth, hh = t.offsetHeight;
  let x = e.clientX + 16, y = e.clientY + 14;
  if (x + w > innerWidth - 8) x = e.clientX - w - 16;
  if (y + hh > innerHeight - 8) y = innerHeight - hh - 8;
  x = Math.max(8, x); y = Math.max(8, y);
  t.style.left = `${x}px`; t.style.top = `${y}px`;
}
function hideTip() { tip().style.display = 'none'; }

const STATE_TEXT = {
  usable: '▶ Playable now', chain: '⛓ Playable only in a chain', ceiling: 'Above the ceiling', acted: 'Already acted this half',
  cooldown: 'On cooldown', notarget: 'Nothing in range', unavailable: 'Unavailable', idle: '', blocked: 'Opening blocked', done: 'Opening done',
};

function abilityTip(c, a) {
  if (a.hidden) {
    return `<h4 class="${c.team}-c">${a.key} · summoner spell</h4>
      <div class="tt-sub">${esc(c.name)} · ${esc(a.reason)}</div>
      <div class="tt-row">The opening in this slot is ${esc(c.name)}'s own: <b>${esc(a.openingName)}</b>.</div>
      ${abilityDiagrams(a.kit, c.team, c.role)}
      ${a.state !== 'idle' && a.state !== 'unavailable' ? `<div class="tt-state">${stateLine(a)}</div>` : ''}`;
  }
  const tier = ['', 'free aim', 'free aim', 'rotatable pattern', 'fixed pattern'][a.init];
  const opening = a.openingName !== a.name ? `<div class="tt-row">Opening in this slot: <b>${esc(a.openingName)}</b> — ${esc(c.name)}'s own; a spell never brings an opening.</div>` : '';
  const amount = a.amount ? ` <span class="tt-dim">(${a.amount} before armour at current POW)</span>` : '';
  const sig = [a.printedSigil && `printed sigil ${a.printedSigil}`, a.slotSigil && `slot sigil ${a.slotSigil}${a.activeSigils.includes(a.slotSigil) ? ' — lit by a beacon' : ' — needs a friendly beacon within 1 hex'}`].filter(Boolean).join(' · ');
  return `<h4 class="${c.team}-c">${a.key} · ${esc(a.name)}</h4>
    <div class="tt-sub">${a.spell ? 'Summoner spell · ' : ''}${esc(c.name)} · initiative ${a.init} (${tier}) · cooldown ${a.cooldown}</div>
    <div class="tt-row">${esc(a.targeting)}</div>
    <div class="tt-row"><b>${esc(a.effects)}</b>${amount}</div>
    ${sig ? `<div class="tt-row">${sig}</div>` : ''}
    <div class="tt-row tt-dim">${a.spell ? esc(a.mold) : `Molds: ${esc(a.mold)}`}</div>
    ${opening}
    ${abilityDiagrams(a.kit, c.team, c.role)}
    ${a.init === 3 && c.presence === 'board' ? '<div class="tt-row tt-dim">On the map: faint = every hex some facing could reach · gold = enemies it could hit now.</div>' : ''}
    ${a.state !== 'idle' || a.reason ? `<div class="tt-state">${stateLine(a)}</div>` : ''}`;
}

function stateLine(a) {
  const label = STATE_TEXT[a.state] ?? a.state;
  if (!a.reason || a.reason.toLowerCase() === label.toLowerCase()) return esc(label);
  return label ? `${label} — ${esc(a.reason)}` : esc(a.reason);
}

function champTip(c) {
  const drift = c.drift.length ? c.drift.map(d => `${d.stat} ${d.permille > 0 ? '+' : ''}${d.permille}`).join(', ') : 'none yet';
  const where = c.presence === 'board' ? `at (${c.q},${c.r})` : c.presence === 'spawn' ? 'in spawn' : c.presence === 'dead' ? `dead — ${respawnText(c)}` : '';
  return `<h4 class="${c.team}-c">${esc(c.name)}</h4>
    <div class="tt-sub">Team ${c.team} · ${c.role} · ${where}</div>
    <div class="tt-row">HP ${c.hp}/${c.maxHp}${c.shield ? ` · shield ${c.shield}` : ''} · POW ${(c.stats.powNow / 1000).toFixed(2)}${c.dying ? ' (halved: dying)' : ''} · ARM ${c.stats.arm} · RCH ${c.stats.rch} · SPD ${c.stats.spd}</div>
    <div class="tt-row" style="color:var(--t3)">Passive — ${esc(c.passive.name)}: ${esc(c.passive.text)}</div>
    ${(c.status || []).map(st => `<div class="tt-row st-line st-${st.kind}-c"><b>${esc(st.label)}</b> — ${esc(st.text)}</div>`).join('')}
    <div class="tt-row tt-dim">Molding so far (permille): ${drift}</div>
    ${c.abilities.map(a => `<div class="tt-row"><b>${a.key}</b> ${esc(a.name)}${a.hidden ? '' : ` [${a.init}] ${esc(a.effects)}`}${a.cd ? ` · cd ${a.cd}` : ''}</div>`).join('')}`;
}

function towerTip(t) {
  const who = t.i === 0 ? 'Centre tower' : `Team ${t.home}'s home tower`;
  return `<h4 class="${t.owner}-c">${who}</h4><div class="tt-sub">(${t.q},${t.r}) · held by ${t.owner === 'None' ? 'nobody' : 'team ' + t.owner}</div>
    <div class="tt-row">${t.hp}/${t.max} HP. At 0 it is captured by the attacker and resets.</div>
    <div class="tt-row tt-dim">Scores 1 point per round for its holder. Shoots enemies within 1 hex at round close. Damage to it drops with each defender nearby.</div>`;
}

// ───────────────────────────── bottom bar ─────────────────────────────

function setBanner(text) { document.getElementById('banner').textContent = text || ''; }
let bannerTimer = null;
function flashBanner(text) { setBanner(text); clearTimeout(bannerTimer); bannerTimer = setTimeout(() => setBanner(''), 3500); }

function renderActions() {
  const prompt = document.getElementById('prompt');
  const buttons = document.getElementById('buttons');
  const extra = document.getElementById('extra');
  buttons.innerHTML = '';
  extra.innerHTML = '';
  const T = `<b class="${V.active}">${V.active}</b>`;
  if (!sel) setLastBanner();

  if (V.phase === 'MatchOver') {
    prompt.innerHTML = `Match over — ${V.winner === 'None' ? 'a draw' : `<b class="${V.winner}">${V.winner}</b> wins`} by ${V.endReason}. Start a new match from the toolbar.`;
    return;
  }
  if (!V.humanTurn) {
    prompt.innerHTML = `${T} (AI) is choosing${paused ? ' — paused' : '…'}`;
    if (paused) buttons.appendChild(btn('Next AI move', () => api('POST', '/api/step'), true));
    return;
  }

  const fallback = legalFor(c => c.kind === 'OpeningFallback');
  const texts = {
    Draft: `${T}: draft a champion.`,
    SpellPick: `${T}: choose a summoner spell for each champion. The other team can't see them until the opening.`,
    Opening: fallback.length
      ? `${T}: no opening ability fits the board. <b>${esc(champById(fallback[0].champ).name)}</b> falls back — click a green marker to step one hex.`
      : `${T} — Opening: hover an ability (glowing icons) to preview its three instructions; click it to play. Every champion plays one. A cast that finds no target fizzles but still costs the cooldown.`,
    Basic: `${T} — Basics ${(V.active === 'A' ? V.basics.a : V.basics.b) + 1} of ${V.basics.per}: click one of your champions, then a green hex to move or a gold target to basic-attack.`,
    Ladder: `${T} — Ladder, ceiling <b>${V.ceiling}</b>: click a glowing ability, then a target on the map. Or pass: the opponent gets one Last Word.`,
    LastWord: `${T} — LAST WORD: one unanswerable ability at initiative ≤ <b>${V.ceiling}</b>, or decline. Then the half ends.`,
  };
  prompt.innerHTML = texts[V.phase] || '';

  const pass = legalFor(c => c.kind === 'Pass')[0];
  const decline = legalFor(c => c.kind === 'Decline')[0];
  if (pass) buttons.appendChild(btn('Pass', () => play(pass.i), false, pass));
  if (decline) buttons.appendChild(btn('Decline', () => play(decline.i), false, decline));
  if (sel) buttons.appendChild(btn('Cancel (Esc)', () => { sel = null; render(); }));

  const chains = legalFor(c => c.champ2 >= 0);
  if (chains.length) {
    extra.appendChild(h(`<div class="box-title">⛓ Chains — two abilities, one step, no answer between (${chains.length})</div>`));
    for (const c of chains) {
      const row = h(`<div class="chain-row">${esc(c.label)}</div>`);
      row.addEventListener('mouseenter', () => { clearHighlights(); hl(c.cells, 'hl-cells'); showPreview(c); });
      row.addEventListener('mouseleave', () => { clearHighlights(); clearPreview(); if (sel) redrawSelection(); });
      row.addEventListener('click', () => play(c.i));
      extra.appendChild(row);
    }
  }
}

function btn(label, fn, primary = false, cmd = null) {
  const b = document.createElement('button');
  b.textContent = label;
  if (primary) b.className = 'primary';
  b.addEventListener('click', fn);
  if (cmd) {
    b.addEventListener('mouseenter', () => showPreview(cmd));
    b.addEventListener('mouseleave', clearPreview);
  }
  return b;
}

function lastActionText() {
  for (let k = V.events.length - 1; k >= 0; k--) {
    const e = V.events[k];
    if (['AbilityResolved', 'Basic', 'Opening', 'Pass', 'Decline', 'Draft', 'MatchOver'].includes(e.kind)) return e.text.trim();
  }
  return '';
}

function renderFeed() {
  const feed = document.getElementById('feed');
  const atBottom = feed.scrollHeight - feed.scrollTop - feed.clientHeight < 30;
  feed.innerHTML = '';
  for (const e of V.events) {
    const major = e.kind === 'Phase' && e.text.startsWith('━━');
    const d = h(`<div class="ev ${e.kind} ${major ? 'major' : ''} ${e.n > seenEvents && seenEvents >= 0 ? 'new' : ''}"></div>`);
    d.textContent = e.text;
    feed.appendChild(d);
  }
  if (V.events.length) seenEvents = V.events[V.events.length - 1].n;
  if (atBottom || true) feed.scrollTop = feed.scrollHeight;
}

// ───────────────────────────── overlays: draft, end, help ─────────────────────────────

function renderOverlay() {
  const ov = document.getElementById('overlay');
  if (helpOpen) return;
  if (V.phase === 'Draft') { ov.classList.remove('hidden'); ov.innerHTML = ''; ov.appendChild(draftView()); return; }
  if (V.phase === 'SpellPick') { ov.classList.remove('hidden'); ov.innerHTML = ''; ov.appendChild(spellView()); return; }
  if (V.phase === 'MatchOver' && !endDismissed) {
    ov.classList.remove('hidden');
    const w = V.winner;
    ov.innerHTML = `<div class="endcard"><div class="big ${w}-c">${w === 'None' ? 'DRAW' : `TEAM ${w} WINS`}</div>
      <p>${V.endReason === 'RoundCap' ? 'round cap reached' : `nexus ${w === 'A' ? 'B' : 'A'} destroyed ${V.endReason === 'Siege' ? 'by siege' : 'by a direct attack'}`} after ${V.round} rounds · nexus A ${Math.max(0, V.nexus[0].hp)} – B ${Math.max(0, V.nexus[1].hp)}</p>
      <button id="end-close">Look at the board</button> <button id="end-new" class="primary">New match</button></div>`;
    document.getElementById('end-close').onclick = () => { endDismissed = true; ov.classList.add('hidden'); };
    document.getElementById('end-new').onclick = () => { endDismissed = false; api('POST', `/api/new?mode=${V.mode}`); };
    return;
  }
  ov.classList.add('hidden');
}
let endDismissed = false;
let helpOpen = false;

function draftView() {
  const picks = legalFor(c => c.kind === 'Draft');
  const byIndex = {};
  for (const p of picks) byIndex[p.ability] = p;
  const who = V.humanTurn ? `Team ${V.active} — your pick` : `Team ${V.active} (AI) is picking…`;
  const modes = [['vsai-A', 'vs AI · play A (bottom)'], ['vsai-B', 'vs AI · play B (top)'], ['hotseat', 'Hotseat'], ['watch', 'Watch AI vs AI']];
  const root = h(`<div class="draft"><h2>Draft</h2>
    <div class="sub mode-row">${modes.map(([m, l]) => `<button data-newmode="${m}" class="${V.mode === m ? 'on' : ''}">${l}</button>`).join(' ')} <button data-help>Rules</button></div>
    <div class="sub">${who}. Snake order A · B B · A A · B B · A A · B. One champion per role; a picked champion is gone for both teams.</div><div class="draft-cols"></div></div>`);
  root.querySelectorAll('[data-newmode]').forEach(b => b.addEventListener('click', () => api('POST', `/api/new?mode=${b.dataset.newmode}`)));
  root.querySelector('[data-help]').addEventListener('click', showHelp);
  const cols = root.querySelector('.draft-cols');
  for (const role of ROLES) {
    const a = V.champions.find(c => c.team === 'A' && c.role === role);
    const b = V.champions.find(c => c.team === 'B' && c.role === role);
    const col = h(`<div class="draft-col"><h3>${roleIcon(role)} ${role}</h3>
      <div class="picks"><span class="A-c">A: ${a.drafted ? esc(a.name) : '—'}</span><span class="B-c">B: ${b.drafted ? esc(b.name) : '—'}</span></div></div>`);
    for (const d of V.roster.filter(x => x.role === role)) {
      const pick = V.humanTurn ? byIndex[d.index] : null;
      const taken = V.champions.find(c => c.drafted && c.id === d.id);
      const card = h(`<div class="dcard ${pick ? 'pickable' : ''} ${taken ? 'taken' : ''}">
        <div class="top">${portrait(d.id, d.glyph, taken ? taken.team : pick ? V.active : 'N')}<div><b>${esc(d.name)}</b>${taken ? ` <span class="${taken.team}-c taken-lbl">picked by ${taken.team}</span>` : ''}<div class="tt-dim">HP ${d.stats.hp} · POW ${(d.stats.pow / 1000).toFixed(2)} · ARM ${d.stats.arm} · RCH ${d.stats.rch} · SPD ${d.stats.spd}</div></div></div>
        ${d.line ? `<div class="dline">${esc(d.line)}</div>` : ''}
        ${d.abilities.map(x => `<div class="dab"><i style="background:var(--t${x.init})">${x.init}</i><span><b>${esc(x.name)}</b> · ${esc(x.effects)} <span class="tt-dim">· cd ${x.cooldown}${x.printedSigil ? ` · sigil ${x.printedSigil}` : ''}${x.slotSigil ? ` · slot ${x.slotSigil}` : ''}</span>${x.casts ? ' <span class="cast-tag">opening attack</span>' : ''}</span></div>`).join('')}
        ${d.signature ? `<div class="dab"><i class="spell">✦</i><span><b>Summoner spell</b> <span class="tt-dim">· chosen after the draft · opening: ${esc(d.signature.name)}</span>${d.signature.casts ? ' <span class="cast-tag">opening attack</span>' : ''}</span></div>` : ''}
        <div class="passive">${esc(d.passive.name)} — ${esc(d.passive.text)}</div>
        ${pick ? `<button class="pickbtn primary">Pick for ${V.active}</button>` : ''}
      </div>`);
      if (pick) card.addEventListener('click', () => play(pick.i));
      card.addEventListener('mouseenter', e => showTip(e, champDetail(d, draftTeam()), 'detail'));
      card.addEventListener('mousemove', moveTip);
      card.addEventListener('mouseleave', hideTip);
      col.appendChild(card);
    }
    cols.appendChild(col);
  }
  return root;
}

// Summoner spells: each team gives every champion one spell from the shared pool, hidden
// from the other team until the opening. No spell twice in a team.
function spellView() {
  const picks = V.humanTurn ? legalFor(c => c.kind === 'SpellPick') : [];
  const who = V.humanTurn ? `Team ${V.active} — choose` : `Team ${V.active} (AI) is choosing — hidden from you`;
  const root = h(`<div class="draft spells"><h2>Summoner spells</h2>
    <div class="sub">${who}. One spell per champion, no spell twice in a team. Both teams' choices are revealed when the opening begins. Spells have long cooldowns and never mold.</div>
    <div class="spell-team"></div><div class="spell-grid"></div></div>`);
  const team = V.humanTurn ? V.active : (V.humans[0] || 'A');
  const row = root.querySelector('.spell-team');
  const next = picks.length ? picks[0].champ : -1;
  for (const c of V.champions.filter(x => x.team === team)) {
    const sp = c.abilities[3];
    row.appendChild(h(`<div class="spell-champ ${c.slot === next ? 'next' : ''}"><div class="portrait">${portrait(c.id, c.glyph, c.team)}</div>
      <div><b>${esc(c.name)}</b><div class="tt-dim">${c.role}</div><div class="${sp.hidden ? 'tt-dim' : 'spell-name'}">${sp.hidden ? (c.slot === next ? '← choosing' : '—') : esc(sp.name)}</div></div></div>`));
  }
  const grid = root.querySelector('.spell-grid');
  const byIndex = {};
  for (const p of picks) byIndex[p.spell] = p;
  for (const sp of V.spells) {
    const pick = byIndex[sp.index];
    const taken = V.humanTurn && !pick;
    const card = h(`<div class="dcard spell-card ${pick ? 'pickable' : ''} ${taken ? 'taken' : ''}">
      <div><i class="det-init" style="background:var(--t${sp.init})">${sp.init}</i> <b>${esc(sp.name)}</b> <span class="tt-dim">· cooldown ${sp.cooldown}</span>${taken ? ' <span class="taken-lbl">taken by your team</span>' : ''}</div>
      <div>${esc(sp.effects)}</div><div class="tt-dim">${esc(sp.targeting)}</div>
      ${pick ? `<button class="pickbtn primary">Give to ${esc(champById(pick.champ).name)}</button>` : ''}</div>`);
    if (pick) card.addEventListener('click', () => play(pick.i));
    card.addEventListener('mouseenter', e => showTip(e, combatDiagram(sp.kit, team, 'Mid').svg + `<div class="dia-cap">${combatDiagram(sp.kit, team, 'Mid').caption}</div>`, 'with-dia'));
    card.addEventListener('mousemove', moveTip);
    card.addEventListener('mouseleave', hideTip);
    grid.appendChild(card);
  }
  return root;
}

// Whose orientation the draft diagrams use: the human's, or the side picking in hotseat.
function draftTeam() { return V.humans.length === 1 ? V.humans[0] : V.active; }

function showHelp() {
  helpOpen = true;
  const ov = document.getElementById('overlay');
  ov.classList.remove('hidden');
  ov.innerHTML = `<div class="help"><button class="close" id="help-close">Close</button><h2>How AUGURY plays</h2>
    <h3>Winning</h3>Destroy the enemy <b>nexus</b> (${V.race.nexusHp} HP). It is damaged three ways: at every round close <b>each tower you hold fires at it for ${V.race.towerSiege}</b>; <b>each enemy death costs it ${V.race.killSiege}</b>; and once it is <b>open</b> — after that team has lost one of its two home towers — you can attack it directly.
    <h3>A round</h3>Two halves. Each half: <b>basics</b> first — each team moves or basic-attacks with two different champions — then the <b>ladder</b>.
    <h3>The ladder</h3>Teams alternate playing one ability at initiative ≤ the <b>ceiling</b> (the coloured pips at the top). The ceiling drops to what you played. Each champion acts once per half. <b>Pass</b> and your opponent gets one unanswerable <b>Last Word</b>. If a team has nothing legal, the half ends with no Last Word.
    <h3>Initiative tiers</h3><span style="color:var(--t1)">1</span>–<span style="color:var(--t2)">2</span>: aim freely at a target in range. <span style="color:var(--t3)">3</span>: a pattern you rotate — click the arrow for the facing you want. <span style="color:var(--t4)">4</span>: a big fixed pattern pointing toward the enemy.
    <h3>Chains</h3>Two abilities that share an active sigil (the small I/II/III tag) resolve as one step with no answer between — and the second may exceed the ceiling. Dashed tags are slot sigils: lit (white) when a friendly beacon is within 1 hex.
    <h3>Round close</h3>Champions at 0 HP die → poison and tower shots (dropping to 0 <i>here</i> means <b>Dying</b>: one more round at half power) → siege: held towers fire at the enemy nexus → cooldowns tick and the dead respawn.
    <h3>Reading the screen</h3>Hover anything. Ability icons show initiative (big number), key, sigil and cooldown. Hovering an ability shows its reach (faint) and what it could hit (gold); on your turn, the hexes it can be played on. Hover a marker, a chain or Pass to preview the exact result. <b>Undo</b> takes back your last decision. <span style="color:var(--last)"><b>Magenta</b> always marks the last action</span> — the champion who acted (and its ability icon), the hexes it covered, a line to what it hit, where anyone moved from, and the numbers that changed.
    <h3>Champions and spells</h3>Each champion has three abilities (Q W E) and a fourth slot (R) for a <b>summoner spell</b>, chosen after the draft from a shared pool of eight — hidden from the opponent until the opening, never twice in a team. The R slot's opening belongs to the champion, not the spell.
    <h3>Opening</h3>Each champion plays one of its four openings, whose three instructions move your team into formation. Moves are strict: an opening that would walk off the board or into a champion, tower or wall can't be played. One opening per champion <b>casts</b>: "mid casts E" fires whatever ability your mid has in E, aimed at the nearest target. With nothing in reach it fizzles — and either way that ability starts round 1 on cooldown. Opening damage can't kill. If no opening fits, one champion steps one hex.
    <h3>Statuses</h3><b>Root</b> can't move or be moved · <b>Burn</b> takes damage each time it acts · <b>Mark</b> the next hit deals more · <b>Exhaust</b> deals half damage · <b>Unstoppable</b> immune to root, push and pull · <b>Wound</b> healing halved · <b>Wall</b> an impassable hex. Towers are solid: nothing may enter them. Hover a status pip on the board or a chip on a card for the details.</div>`;
  document.getElementById('help-close').onclick = () => { helpOpen = false; renderOverlay(); };
}

// ───────────────────────────── wiring ─────────────────────────────

document.querySelectorAll('#toolbar [data-mode]').forEach(b => b.addEventListener('click', () => {
  endDismissed = false;
  api('POST', `/api/new?mode=${b.dataset.mode}`);
}));
document.getElementById('undo').addEventListener('click', () => { clearTimeout(aiTimer); api('POST', '/api/undo'); });
document.getElementById('help-btn').addEventListener('click', showHelp);
document.getElementById('pause').addEventListener('click', e => {
  paused = !paused;
  e.target.textContent = paused ? 'Resume' : 'Pause';
  e.target.classList.toggle('on', paused);
  renderActions();
  scheduleAi();
});
document.getElementById('speed').addEventListener('change', scheduleAi);
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    if (helpOpen) { helpOpen = false; renderOverlay(); return; }
    if (sel) { sel = null; render(); }
  }
});
document.getElementById('map').addEventListener('click', e => {
  if (sel && !e.target.closest('.marker') && !e.target.closest('.token.selectable')) { sel = null; render(); }
});

api('GET', '/api/state');
