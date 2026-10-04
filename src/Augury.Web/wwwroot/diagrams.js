// AUGURY — small fixed diagrams of what an ability does: its combat shape and its three
// opening instructions. Drawn from the ability's data (the `kit` the server sends), in the
// team's real orientation: team A plays up the screen, team B is its mirror image.
'use strict';

const DIRS = [[1, 0], [1, -1], [0, -1], [-1, 0], [-1, 1], [0, 1]];   // canonical frame, as Hex.Directions
const MINI = 10;
const START_ROW = ['Top', 'Jungle', 'Mid', 'Bottom', 'Support'];
const DIR_NAMES = ['right', 'back-right', 'back-left', 'left', 'forward-left', 'forward-right'];

const mirrorHex = ([q, r]) => [q + r, -r];
const frameFor = team => (team === 'B' ? mirrorHex : h => h);
const hexDist = ([q1, r1], [q2, r2]) => (Math.abs(q1 - q2) + Math.abs(r1 - r2) + Math.abs(q1 + r1 - q2 - r2)) / 2;
// The board comes from the server's view (V), so diagrams follow the shipped layout.
const boardSet = () => new Set((V?.hexes || []).map(x => `${x.q},${x.r}`));
const towerSet = () => new Set(V?.board?.towersBlock ? (V.towers || []).map(t => `${t.q},${t.r}`) : []);
const onBoard = h => boardSet().has(`${h[0]},${h[1]}`);
const add = (a, b) => [a[0] + b[0], a[1] + b[1]];
const same = (a, b) => a[0] === b[0] && a[1] === b[1];

function mpx([q, r]) { return [MINI * Math.sqrt(3) * (q + r / 2), -MINI * 1.5 * r]; }
function mhex(h, scale = 0.93) {
  const [cx, cy] = mpx(h);
  const pts = [];
  for (let k = 0; k < 6; k++) {
    const a = Math.PI / 180 * (60 * k - 30);
    pts.push(`${(cx + MINI * scale * Math.cos(a)).toFixed(1)},${(cy + MINI * scale * Math.sin(a)).toFixed(1)}`);
  }
  return pts.join(' ');
}
function around(radius, centre = [0, 0]) {
  const out = [];
  for (let q = -radius; q <= radius; q++) for (let r = -radius; r <= radius; r++) {
    const h = add(centre, [q, r]);
    if (hexDist(h, centre) <= radius) out.push(h);
  }
  return out;
}

// One small SVG. Everything is given in board-like coordinates and drawn as is.
function miniSvg({ bg, cells = [], tokens = [], arrows = [], beacons = [], marks = [], kind = 'combat' }) {
  const pts = [...bg, ...cells.map(c => c.h), ...tokens.map(t => t.h)].map(mpx);
  const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]);
  const pad = MINI * 1.2;
  const x0 = Math.min(...xs) - pad, y0 = Math.min(...ys) - pad;
  const w = Math.max(...xs) - Math.min(...xs) + 2 * pad, hgt = Math.max(...ys) - Math.min(...ys) + 2 * pad;
  const parts = [`<svg class="mini ${kind}" viewBox="${x0.toFixed(1)} ${y0.toFixed(1)} ${w.toFixed(1)} ${hgt.toFixed(1)}">`,
    '<defs><marker id="mm-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="4" markerHeight="4" orient="auto"><path d="M0 0 L10 5 L0 10 z" fill="#fff"/></marker>',
    '<marker id="mm-arrow-bad" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="4" markerHeight="4" orient="auto"><path d="M0 0 L10 5 L0 10 z" fill="#ff5a5a"/></marker></defs>'];
  for (const h of bg) parts.push(`<polygon points="${mhex(h)}" class="mm-bg"/>`);
  for (const c of cells) parts.push(`<polygon points="${mhex(c.h)}" class="mm-${c.cls}"/>`);
  for (const b of beacons) {
    const [x, y] = mpx(b.h);
    const bx = x - MINI * 0.45, by = y - MINI * 0.45;
    parts.push(`<polygon points="${bx},${by - 5} ${bx + 4.5},${by} ${bx},${by + 5} ${bx - 4.5},${by}" class="beacon ${b.team}"/><text x="${bx}" y="${by + 0.5}" class="mm-sig">${b.sigil}</text>`);
  }
  for (const t of tokens) {
    const [x, y] = mpx(t.h);
    const s = MINI * 1.1;
    parts.push(`<g class="mm-tok ${t.team} ${t.cls || ''}"><circle cx="${x}" cy="${y}" r="${MINI * 0.68}"/>`
      + (t.role ? roleIcon(t.role, '#fff').replace('<svg', `<svg x="${x - s / 2}" y="${y - s / 2}" width="${s}" height="${s}"`) : '') + '</g>');
  }
  for (const a of arrows) {
    const [x1, y1] = mpx(a.from), [x2, y2] = mpx(a.to);
    const d = Math.hypot(x2 - x1, y2 - y1) || 1, sh = MINI * 0.55;
    parts.push(`<line x1="${x1 + (x2 - x1) * sh / d}" y1="${y1 + (y2 - y1) * sh / d}" x2="${x2 - (x2 - x1) * sh / d}" y2="${y2 - (y2 - y1) * sh / d}" class="mm-arrow ${a.cls || ''}" marker-end="url(#mm-arrow${a.cls === 'bad' ? '-bad' : ''})"/>`);
    if (a.label) {
      // Numbered beside the arrow, not on it, so it never covers a token.
      const mx = (x1 + x2) / 2 - (y2 - y1) / d * 6, my = (y1 + y2) / 2 + (x2 - x1) / d * 6;
      parts.push(`<circle cx="${mx}" cy="${my}" r="4.2" class="mm-num-bg"/><text x="${mx}" y="${my + 0.4}" class="mm-num">${a.label}</text>`);
    }
  }
  for (const m of marks) {
    const [x, y] = mpx(m.h);
    parts.push(`<text x="${x}" y="${y + 0.5}" class="mm-mark ${m.cls || ''}">${m.text}</text>`);
  }
  parts.push('</svg>');
  return parts.join('');
}

// ───────────────────────────── combat shape ─────────────────────────────

function combatDiagram(kit, team, role) {
  const T = frameFor(team);
  const enemy = team === 'A' ? 'B' : 'A';
  const fx = kit.fx.map(f => f.kind);
  const damages = fx.includes('Damage');
  const hostile = ['Damage', 'Poison', 'Burn', 'Root', 'Mark', 'Exhaust', 'Wound', 'Displace'];
  const hitCls = kit.target !== 'Ally' && fx.some(k => hostile.includes(k)) ? 'hit' : 'buff';
  if (kit.tier === 0) return { svg: '', caption: '' };
  const displace = kit.fx.find(f => f.kind === 'Displace');
  const caster = { h: [0, 0], role, team, cls: 'caster' };

  if (kit.tier >= 3) {
    const cells = kit.pattern.map(T);
    const radius = Math.max(2, ...cells.map(c => hexDist(c, [0, 0])));
    const push = displace ? ` Then ${displace.amount > 0 ? `pushes ${displace.amount}` : `pulls ${-displace.amount}`} ${displace.amount > 0 ? 'away from' : 'toward'} the caster.` : '';
    const n = cells.length;
    return {
      svg: miniSvg({ bg: around(radius), cells: cells.map(h => ({ h, cls: hitCls })), tokens: [caster] }),
      caption: (kit.tier === 3
        ? `<b>Rotatable pattern</b> — shown in one of its 6 facings; you pick the facing.`
        : `<b>Fixed pattern</b> — always exactly this, aimed toward the enemy.`)
        + ` <b>${n === 1 ? 'Single hex' : `Area: ${n} hexes`}</b> — every enemy in a ${hitCls === 'hit' ? 'red' : 'green'} hex is affected.${push}`,
    };
  }

  // Free aim: every hex in range is a possible target, one is chosen.
  const range = kit.range;
  if (kit.target === 'Self') {
    return {
      svg: miniSvg({ bg: around(1), cells: [{ h: [0, 0], cls: 'buff' }], tokens: [caster] }),
      caption: `<b>Self</b> — affects only the caster. No aim.`,
    };
  }
  if (kit.target === 'TeleportHex') {
    return {
      svg: miniSvg({ bg: around(2), cells: around(1).filter(h => !same(h, [0, 0])).map(h => ({ h, cls: 'buff' })), tokens: [], marks: [{ h: [0, 0], text: '♜' }] }),
      caption: `<b>Teleport</b> — to any empty hex beside a friendly tower or beacon (green), from anywhere on the board or from spawn.`,
    };
  }
  if (kit.target === 'Structure') {
    const at = T([0, range]);
    return {
      svg: miniSvg({ bg: around(Math.max(range + 1, 2)), cells: [...around(range).filter(h => !same(h, [0, 0])).map(h => ({ h, cls: 'reach' })), { h: at, cls: 'hit' }], tokens: [caster], marks: [{ h: at, text: '♜' }] }),
      caption: `<b>A tower or an open nexus</b> up to ${range} away (gold). Example in red.`,
    };
  }
  const bg = around(Math.max(range + 1, 2));
  const reach = around(range).filter(h => !same(h, [0, 0]) || kit.target === 'Ally').map(h => ({ h, cls: 'reach' }));
  const ahead = T([0, range]);
  if (kit.target === 'EmptyHex' && fx.includes('Wall')) {
    return {
      svg: miniSvg({ bg, cells: [...reach, { h: ahead, cls: 'wall' }], tokens: [caster] }),
      caption: `<b>Free aim · empty hex</b> up to ${range} away (gold) becomes a <b>wall</b>: nothing may enter it until it falls. Example shown.`,
    };
  }
  if (kit.target === 'EmptyHex') {
    return {
      svg: miniSvg({ bg, cells: [...reach, { h: ahead, cls: 'buff' }], tokens: [caster], arrows: [{ from: [0, 0], to: ahead }] }),
      caption: `<b>Free aim · single hex</b> — ${fx.includes('Dash') ? 'dash' : 'move'} to any empty hex up to ${range} away (gold). Example shown.`,
    };
  }
  if (kit.target === 'Ally') {
    const ally = T([-1, 1]);
    const arrows = [];
    let what = 'Example target in green.';
    if (fx.includes('Swap')) { arrows.push({ from: [0, 0], to: ally }, { from: ally, to: [0, 0] }); what = 'The caster and the ally <b>swap places</b>.'; }
    const pull = kit.fx.find(f => f.kind === 'PullAlly');
    let far = ally;
    if (pull) { far = T([-1, 1 + pull.amount]); arrows.push({ from: far, to: ally }); what = `The ally is <b>pulled up to ${pull.amount}</b> toward the caster.`; }
    return {
      svg: miniSvg({ bg: around(Math.max(range + 1, 2, hexDist(far, [0, 0]))), cells: [...reach, { h: far, cls: 'buff' }], tokens: [caster, { h: far, role: null, team, cls: 'ghost' }], arrows }),
      caption: `<b>Free aim · one ally</b>${fx.includes('Swap') || pull ? '' : ' (or itself)'} within ${range} (gold). ${what}`,
    };
  }
  const arrows = [];
  if (displace) {
    const step = T([0, displace.amount > 0 ? 1 : -1]);
    let to = ahead;
    for (let k = 0; k < Math.abs(displace.amount); k++) to = add(to, step);
    arrows.push({ from: ahead, to });
  }
  return {
    svg: miniSvg({ bg: around(Math.max(range + 1 + (displace?.amount > 0 ? displace.amount : 0), 2)), cells: [...reach, { h: ahead, cls: hitCls }], tokens: [caster, { h: ahead, role: null, team: enemy, cls: 'ghost' }], arrows }),
    caption: `<b>Free aim · single target</b> — one enemy or structure up to ${range} away (gold). Example target in red.`
      + (displace ? ` ${displace.amount > 0 ? 'Pushed' : 'Pulled'} ${Math.abs(displace.amount)}.` : ''),
  };
}

// ───────────────────────────── opening ─────────────────────────────

// The three instructions, played from the starting line. In a match they run from wherever
// the team stands when the ability is played, so this is the shape, not a promise.
function openingDiagram(kit, team, role) {
  const T = frameFor(team);
  const pos = {};
  const startA = V?.board?.startA || START_ROW.map((r, i) => [i, -4]);
  START_ROW.forEach((r, i) => { pos[r] = startA[i]; });
  const start = { ...pos };
  const towers = towerSet();
  const arrows = [], beacons = [], marks = [], cells = [];
  let blocked = false;
  kit.steps.forEach((st, i) => {
    if (blocked) return;
    if (st.kind === 'beacon') {
      beacons.push({ h: T(pos[st.role]), sigil: st.sigil, team });
      marks.push({ h: T(add(pos[st.role], [0.9, 0.35])), text: `${i + 1}`, cls: 'step' });
      return;
    }
    if (st.kind === 'cast') {
      // A cast never blocks: with nothing in reach it fizzles, but the ability still goes on cooldown.
      cells.push({ h: T(pos[st.role]), cls: 'cast' });
      marks.push({ h: T(add(pos[st.role], [0.9, 0.35])), text: `${i + 1}`, cls: 'step cast' });
      return;
    }
    const to = add(pos[st.role], DIRS[st.dir]);
    const occupied = Object.values(pos).some(p => same(p, to)) || towers.has(`${to[0]},${to[1]}`);
    if (!onBoard(to) || occupied) {
      arrows.push({ from: T(pos[st.role]), to: T(to), label: `${i + 1}`, cls: 'bad' });
      marks.push({ h: T(to), text: '✕', cls: 'bad' });
      blocked = true;
      return;
    }
    arrows.push({ from: T(pos[st.role]), to: T(to), label: `${i + 1}` });
    pos[st.role] = to;
  });

  // Our half of the board (rows up to the centre), with any solid towers on it.
  const bg = (V?.hexes || []).filter(x => x.r <= 0).map(x => T([x.q, x.r]));
  for (const t of (V?.towers || [])) if (towers.has(`${t.q},${t.r}`) && t.r <= 0) marks.push({ h: T([t.q, t.r]), text: '♜', cls: 'tower' });
  const tokens = [];
  for (const r of START_ROW) {
    if (!same(pos[r], start[r])) tokens.push({ h: T(start[r]), role: r, team, cls: 'ghost' });
    tokens.push({ h: T(pos[r]), role: r, team, cls: r === role ? 'caster' : '' });
  }
  const dir = team === 'B' ? '↓' : '↑';
  const text = s => s.kind === 'move' ? `${s.role.toLowerCase()} ${DIR_NAMES[s.dir]}`
    : s.kind === 'cast' ? `<b class="cast-txt">${s.role.toLowerCase()} casts ${s.slot}</b>` : `beacon ${s.sigil} under ${s.role.toLowerCase()}`;
  const casts = kit.steps.some(s => s.kind === 'cast');
  return {
    svg: miniSvg({ bg, cells, tokens, arrows, beacons, marks, kind: 'opening' }),
    caption: blocked
      ? `<b class="bad-txt">Not playable from the starting line</b> — step ${arrows.length} is blocked. Playable once the team has moved.`
      : `<b>Opening</b> from the starting line (forward ${dir}): ${kit.steps.map((s, i) => `${i + 1}. ${text(s)}`).join(' · ')}.`
        + (casts ? ' <span class="tt-dim">A cast fires that role\'s ability at the nearest target; with nothing in reach it fizzles, and either way the ability starts round 1 on cooldown. Opening damage can\'t kill.</span>' : ''),
  };
}

// ───────────────────────────── composed views ─────────────────────────────

function abilityDiagrams(kit, team, role) {
  const c = combatDiagram(kit, team, role), o = openingDiagram(kit, team, role);
  return `<div class="dia-pair">
    ${c.svg ? `<div class="dia"><div class="dia-title">In combat</div>${c.svg}<div class="dia-cap">${c.caption}</div></div>` : ''}
    <div class="dia"><div class="dia-title">In the opening</div>${o.svg}<div class="dia-cap">${o.caption}</div></div>
  </div>`;
}

// The draft's hover card: every ability, with both diagrams, so champions can be compared
// at a glance ("this one hits to the left, that one to the right").
function champDetail(d, team) {
  const all = d.signature ? [...d.abilities, { ...d.signature, init: 0, effects: 'Summoner spell — chosen after the draft, hidden until the opening.', targeting: 'any of the eight', cooldown: '—' }] : d.abilities;
  const rows = all.map(a => {
    const c = combatDiagram(a.kit, team, d.role), o = openingDiagram(a.kit, team, d.role);
    return `<div class="det-row">
      <div class="det-txt"><div>${a.init ? `<i class="det-init" style="background:var(--t${a.init})">${a.init}</i>` : '<i class="det-init spell">✦</i>'} <b>${a.key} · ${esc(a.name)}</b>${a.casts ? ' <span class="cast-tag">opening attack</span>' : ''}</div>
        <div>${esc(a.effects)}</div><div class="tt-dim">${esc(a.targeting)} · cd ${a.cooldown}${a.printedSigil ? ` · sigil ${a.printedSigil}` : ''}${a.slotSigil ? ` · slot ${a.slotSigil}` : ''}</div></div>
      <div class="dia">${c.svg || '<div class="tt-dim">Your summoner spell.</div>'}<div class="dia-cap">${c.caption}</div></div>
      <div class="dia">${o.svg}<div class="dia-cap">${o.caption}</div></div>
    </div>`;
  }).join('');
  return `<div class="det-head"><div class="det-port">${portrait(d.id, d.glyph, team)}</div><div>
      <h4>${esc(d.name)} <span class="tt-dim">· ${d.role}</span></h4>
      <div class="tt-dim">HP ${d.stats.hp} · POW ${(d.stats.pow / 1000).toFixed(2)} · ARM ${d.stats.arm} · RCH ${d.stats.rch} · SPD ${d.stats.spd}</div>
      ${d.line ? `<div class="det-line">${esc(d.line)}</div>` : ''}
      <div style="color:var(--t3)">${esc(d.passive.name)} — ${esc(d.passive.text)}</div></div></div>
    <div class="det-cols"><span></span><span>In combat</span><span>In the opening (team ${team}, from the starting line)</span></div>
    ${rows}`;
}
