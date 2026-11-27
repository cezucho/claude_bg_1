// ═════════════ CAREER MODE (2026-11-27) ═════════════
// You manage one team through four leagues. Every team has five named players with favourite champions and
// champions the team refuses to play; a team forced onto one plays it "uncomfortably" (−1 attack, −10% HP).
// A season: 7 league matches (round robin of 8), playoffs for the top 4 (semi-final, final), top 2 promoted,
// bottom 2 relegated. Matches you do not play are decided by ratings (Elo), with drafts drawn from each team's
// preferences so the scouting statistics fill up. Saved in localStorage (3 slots), with export / import.
// Loaded after index.html's main script: it uses its globals (G, ROSTER, ROLES, newMatch, render, h, esc…).

const CAREER_V = 1, CSLOT = n => `augury-career-v${CAREER_V}-s${n}`, CLAST = 'augury-career-last';
const TIERS = [
  { name: 'Masters League', short: 'Masters', cup: 'Masters Cup', base: 1420, ai: { plan: 2, beam: 6, samples: 2, noise: 0, fit: 2.0, power: 1.10 }, scout: 1.0, col: '#e7b75a' },
  { name: 'Pro League', short: 'Pro', cup: 'Pro Cup', base: 1270, ai: { plan: 2, beam: 4, samples: 2, noise: 0.4, fit: 1.6, power: 1.03 }, scout: 0.7, col: '#c49bff' },
  { name: 'Challenger League', short: 'Challenger', cup: 'Challenger Cup', base: 1120, ai: { plan: 1, beam: 5, samples: 2, noise: 1.0, fit: 1.2, power: 0.97 }, scout: 0.4, col: '#9fd8ff' },
  { name: 'Rookie League', short: 'Rookie', cup: 'Rookie Cup', base: 970, ai: { plan: 1, beam: 3, samples: 1, noise: 1.5, fit: 0.6, power: 0.88 }, scout: 0, col: '#d09a5a' },
];
const MY_AI = { plan: 2, beam: 5, samples: 2, noise: 0, fit: 1.5, power: 1 };   // "let the AI play my match"
const UNCOMFY = { atk: 1, hp: 0.9 };
// Difficulty by league: AI search and mistakes, draft quality, and team power (champion HP: better players).
// Measured 150 AI matches each: search depth and mistakes alone barely matter (Rookie AI vs Masters AI 48%),
// HP ×0.92 → 38%, ×1.08 → 61% against an even AI. See game/README.md "Career mode".
let C = null;   // the career in play

// ── seeded dice for the career (its state is saved, so a career replays the same way) ──
function crng() { C.rs = (C.rs + 0x6D2B79F5) | 0; let t = C.rs; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }
const cpick = a => a[Math.floor(crng() * a.length)];
const cshuffle = a => { const o = [...a]; for (let i = o.length - 1; i > 0; i--) { const j = Math.floor(crng() * (i + 1)); [o[i], o[j]] = [o[j], o[i]]; } return o; };
const cweighted = (items, w) => { const tot = items.reduce((s, x) => s + w(x), 0); let r = crng() * tot; for (const x of items) { r -= w(x); if (r <= 0) return x; } return items[items.length - 1]; };

// ── the world ──
const ADJ = ['Ashen', 'Iron', 'Crimson', 'Silver', 'Golden', 'Shadow', 'Storm', 'Frost', 'Ember', 'Verdant', 'Obsidian', 'Azure', 'Thunder', 'Jade', 'Scarlet', 'Midnight', 'Copper', 'Ivory', 'Wild', 'Hollow', 'Radiant', 'Feral', 'Gilded', 'Rune', 'Silent', 'Brass', 'Cobalt', 'Dusk'];
const NOUN = ['Wolves', 'Ravens', 'Owls', 'Serpents', 'Lions', 'Foxes', 'Stags', 'Drakes', 'Hawks', 'Bears', 'Vipers', 'Titans', 'Wardens', 'Comets', 'Specters', 'Knights', 'Golems', 'Phoenixes', 'Lynxes', 'Krakens', 'Sentinels', 'Hounds', 'Oracles', 'Wyverns', 'Mantis', 'Badgers', 'Herons', 'Jackals'];
const SYL1 = ['Ka', 'Ze', 'Ny', 'Ro', 'Ta', 'Vi', 'Lo', 'Sa', 'Mi', 'Dra', 'Ko', 'Fe', 'Ju', 'Ri', 'Xa', 'Bo', 'Ce', 'Ha', 'Ul', 'Ae', 'Py', 'Qu', 'Sly', 'Tor', 'We'];
const SYL2 = ['ren', 'x', 'ra', 'lo', 'vin', 'sk', 'to', 'mi', 'den', 'ka', 'zu', 'no', 'th', 'ris', 'gar', 'phy', 'lia', 'ko', 'van', 'rek', 'yo', 'shi', 'ndo', 'mo', 'rix'];
const handle = used => { for (let i = 0; i < 50; i++) { const n = cpick(SYL1) + cpick(SYL2) + (crng() < .15 ? cpick(['', '7', 'X', 'o']) : ''); if (!used.has(n)) { used.add(n); return n; } } return 'Player' + used.size; };
const roleChamps = role => ROSTER.filter(d => d.role === role).map(d => d.id);
const defOf = id => ROSTER.find(d => d.id === id);
const teamOf = id => C.teams[id];
const freshRec = () => ({ g: 0, w: 0, d: 0, l: 0, picks: {}, bans: {}, champW: {} });

function makeTeam(id, name, short, tier, rating, used, human) {
  const players = ROLES.map(role => ({ name: handle(used), role, pool: {} })), dislikes = [];
  if (!human) {
    for (const p of players) { const ids = cshuffle(roleChamps(p.role)), n = crng() < .35 ? 3 : 2; ids.slice(0, n).forEach((x, i) => { p.pool[x] = [5, 3, 1.5][i]; }); }
    const cand = cshuffle(ROSTER.map(d => d.id).filter(x => !players.some(p => p.pool[x])));
    const nd = crng() < .4 ? 4 : 3; for (const x of cand) { if (dislikes.length >= nd) break; const role = defOf(x).role;
      if (dislikes.filter(y => defOf(y).role === role).length < 2) dislikes.push(x); } }
  return { id, name, short, hue: Math.floor(crng() * 360), tier, rating, players, dislikes, rec: freshRec(), human: !!human, hist: [] };
}
function newCareer(slot, manager, teamName, tag) {
  C = { v: CAREER_V, slot, manager: manager || 'Manager', created: Date.now(), rs: (Date.now() & 0x7fffffff) || 7, teams: [], season: null, seasonNo: 1,
        stats: { g: 0, w: 0, l: 0, d: 0, kf: 0, ka: 0, champs: {}, players: {}, vs: {}, history: [] }, trophies: [], seasons: [], fame: [], news: [] };
  const used = new Set(), adj = cshuffle(ADJ), noun = cshuffle(NOUN), tags = new Set([(tag || 'AUG').toUpperCase().slice(0, 4)]);
  const tagFor = (a, n) => { for (const t of [a[0] + n.slice(0, 2), a.slice(0, 2) + n[0], a[0] + n[0] + n[n.length - 1], a.slice(0, 3), n.slice(0, 3)].map(x => x.toUpperCase())) if (!tags.has(t)) { tags.add(t); return t; } return 'T' + tags.size; };
  C.teams.push(makeTeam(0, teamName || 'Augury Rookies', (tag || 'AUG').toUpperCase().slice(0, 4), 3, TIERS[3].base - 40, used, true));
  let k = 0;
  for (let tier = 0; tier < 4; tier++) for (let i = 0; i < (tier === 3 ? 7 : 8); i++) {
    const a = adj[k % adj.length], n = noun[k % noun.length]; k++;   // every team its own adjective and its own noun
    const r = TIERS[tier].base + Math.round((crng() - .5) * 120);
    C.teams.push(makeTeam(C.teams.length, a + ' ' + n, tagFor(a, n), tier, r, used, false)); }
  seedHistory();
  startSeason();
  news(`Welcome, ${C.manager}. ${teamOf(0).name} start in the ${TIERS[3].name}.`);
  saveCareer();
}
// Two past seasons of drafts per team, so scouting has numbers from day one (results and ratings untouched).
function seedHistory() {
  for (let tier = 0; tier < 4; tier++) { const ids = C.teams.filter(t => t.tier === tier && !t.human).map(t => t.id);
    for (let rep = 0; rep < 2; rep++) for (const fx of fixtures(ids)) for (const [a, b] of fx) { const dr = virtualDraft(a, b); recordDraft(a, b, dr, crng() < winP(a, b, dr) ? a : b, true); } }
}

// ── seasons ──
function fixtures(ids) {   // round robin (circle method); with an odd count one team rests each round
  const t = [...ids]; if (t.length % 2) t.push(-1); const n = t.length, rounds = [];
  for (let r = 0; r < n - 1; r++) { const pairs = []; for (let i = 0; i < n / 2; i++) { const a = t[i], b = t[n - 1 - i]; if (a >= 0 && b >= 0) pairs.push(r % 2 ? [b, a] : [a, b]); } rounds.push(pairs); t.splice(1, 0, t.pop()); }
  return rounds;
}
function startSeason() {
  const me = teamOf(0), ids = cshuffle(C.teams.filter(t => t.tier === me.tier).map(t => t.id));
  C.season = { n: C.seasonNo, tier: me.tier, teams: ids, fx: fixtures(ids), md: 0, res: [], phase: 'league', po: null, start: Object.fromEntries(C.teams.map(t => [t.id, t.rating])) };
}
const myPair = () => { const s = C.season; if (s.phase === 'league') return s.fx[s.md].find(p => p.includes(0)) || null;
  if (s.phase === 'playoffs' && s.po) { const st = s.po.stage === 'semi' ? s.po.semis.find(p => p.includes(0)) : s.po.stage === 'final' ? s.po.final : null; return st && st.includes(0) ? st : null; } return null; };
const stageName = () => { const s = C.season; return s.phase === 'league' ? `Matchday ${s.md + 1} of ${s.fx.length}` : s.po && s.po.stage === 'semi' ? 'Playoff semi-final' : s.po && s.po.stage === 'final' ? 'Playoff final' : 'Season over'; };
function table(s = C.season) {
  const row = {}; for (const id of s.teams) row[id] = { id, p: 0, w: 0, d: 0, l: 0, pts: 0, kf: 0, ka: 0, form: [] };
  for (const md of s.res) for (const r of md) for (const [x, y, kx, ky] of [[r.a, r.b, r.ka, r.kb], [r.b, r.a, r.kb, r.ka]]) { const o = row[x]; o.p++; o.kf += kx; o.ka += ky;
    const res = r.w === x ? 'W' : r.w == null ? 'D' : 'L'; o[res.toLowerCase()]++; o.pts += res === 'W' ? 3 : res === 'D' ? 1 : 0; o.form.push(res); }
  return Object.values(row).sort((a, b) => b.pts - a.pts || b.w - a.w || (b.kf - b.ka) - (a.kf - a.ka) || teamOf(b.id).rating - teamOf(a.id).rating);
}

// ── deciding matches nobody plays ──
const expect = (ra, rb) => 1 / (1 + Math.pow(10, (rb - ra) / 400));
function winP(a, b, dr) { let p = expect(teamOf(a).rating, teamOf(b).rating);
  if (dr) { p -= .06 * dr.uncomfy[a]; p += .06 * dr.uncomfy[b]; } return Math.min(.95, Math.max(.05, p)); }
function prefsOf(tid, oppId) {   // what a team wants in a draft: its players' favourites, its dislikes, whom to ban
  const t = teamOf(tid), pool = {}; for (const p of t.players) Object.assign(pool, p.pool);
  if (t.human) for (const [id, z] of Object.entries(C.stats.champs)) if (!z.sim) pool[id] = Math.min(5, z.g);   // you, in a simulated match: what you actually play
  const banTargets = {};
  if (oppId != null) { const o = teamOf(oppId), scout = TIERS[t.tier].scout;
    if (o.human) { const ch = Object.entries(C.stats.champs).filter(([, z]) => z.g >= 3).sort((a, b) => b[1].g - a[1].g).slice(0, 4);
      const tot = Math.max(1, C.stats.g); for (const [id, z] of ch) banTargets[id] = scout * 8 * z.g / tot; }
    else for (const p of o.players) for (const [id, w] of Object.entries(p.pool)) banTargets[id] = (banTargets[id] || 0) + scout * w * .9; }
  return { pool, dislikes: new Set(t.dislikes), banTargets };
}
function virtualDraft(a, b) {
  const taken = new Set(), bans = { [a]: [], [b]: [] }, picks = { [a]: {}, [b]: {} }, pr = { [a]: prefsOf(a, b), [b]: prefsOf(b, a) }, uncomfy = { [a]: 0, [b]: 0 };
  const perRole = r => [...taken].filter(x => bans[a].includes(x) || bans[b].includes(x)).filter(x => defOf(x).role === r).length;
  for (let i = 0; i < 2; i++) for (const t of [a, b]) { const opts = ROSTER.filter(d => !taken.has(d.id) && perRole(d.role) < 2);
    const d = cweighted(opts, d => .4 + (pr[t].banTargets[d.id] || 0) * 2 - (pr[t].pool[d.id] ? .35 : 0)); bans[t].push(d.id); taken.add(d.id); }
  const order = [a, b, b, a, a, b, b, a, a, b];
  for (const t of order) { const free = ROLES.filter(r => !picks[t][r]); const role = cpick(free);
    let opts = ROSTER.filter(d => d.role === role && !taken.has(d.id));
    const liked = opts.filter(d => pr[t].pool[d.id]), ok = opts.filter(d => !pr[t].dislikes.has(d.id));
    const d = liked.length && crng() < .85 ? cweighted(liked, d => pr[t].pool[d.id]) : ok.length ? cpick(ok) : cpick(opts);
    if (pr[t].dislikes.has(d.id)) uncomfy[t]++; picks[t][role] = d.id; taken.add(d.id); }
  return { bans, picks, uncomfy };
}
function recordDraft(a, b, dr, w, quiet) {
  for (const [t, o] of [[a, b], [b, a]]) { const r = teamOf(t).rec; r.g++; if (w === t) r.w++; else if (w == null) r.d++; else r.l++;
    for (const id of Object.values(dr.picks[t])) { r.picks[id] = (r.picks[id] || 0) + 1; if (w === t) r.champW[id] = (r.champW[id] || 0) + 1; }
    for (const id of dr.bans[t]) r.bans[id] = (r.bans[id] || 0) + 1; }
}
function elo(a, b, w, k = 24) { const ta = teamOf(a), tb = teamOf(b), e = expect(ta.rating, tb.rating), s = w === a ? 1 : w == null ? .5 : 0, d = Math.round(k * (s - e)); ta.rating += d; tb.rating -= d; return d; }
function simMatch(a, b) {
  const dr = virtualDraft(a, b), w = crng() < winP(a, b, dr) ? a : b; recordDraft(a, b, dr, w);
  const kw = 8 + Math.floor(crng() * 10), kl = Math.floor(crng() * kw * .8);
  elo(a, b, w); return { a, b, w, ka: w === a ? kw : kl, kb: w === b ? kw : kl, sim: true };
}

// ── advancing the season ──
function storeResult(r) {
  const s = C.season;
  if (s.phase === 'league') { (s.res[s.md] || (s.res[s.md] = [])).push(r); }
  else if (s.phase === 'playoffs') (s.po.res || (s.po.res = [])).push({ ...r, stage: s.po.stage });
}
function finishRound() {   // everyone else's matches of this matchday / playoff stage, then move on
  const s = C.season;
  if (s.phase === 'league') { const done = new Set((s.res[s.md] || []).flatMap(r => [r.a, r.b]));
    for (const [a, b] of s.fx[s.md]) if (!done.has(a)) storeResult(simMatch(a, b));
    s.md++; if (s.md >= s.fx.length) startPlayoffs(); return; }
  if (s.phase === 'playoffs') { const po = s.po;
    if (po.stage === 'semi') { const ws = po.semis.map(p => { const r = (po.res || []).find(x => x.stage === 'semi' && p.includes(x.a) && p.includes(x.b)) || (storeResult(simMatch(...p)), po.res[po.res.length - 1]); return r.w; });
      po.final = ws; po.stage = 'final'; if (!po.final.includes(0)) finishRound(); return; }
    if (po.stage === 'final') { if (!(po.res || []).some(x => x.stage === 'final')) storeResult(simMatch(...po.final)); po.stage = 'done'; endSeasonPhase(); } }
}
function startPlayoffs() {
  const s = C.season, t = table(s).map(r => r.id); s.phase = 'playoffs'; s.po = { stage: 'semi', semis: [[t[0], t[3]], [t[1], t[2]]], final: null, res: [] };
  news(`League over: ${teamOf(t[0]).name} top the ${TIERS[s.tier].name}. ${t.indexOf(0) < 4 ? `You are in the playoffs (${t.indexOf(0) + 1}${['st', 'nd', 'rd', 'th'][Math.min(3, t.indexOf(0))]}).` : 'You missed the playoffs.'}`);
  if (t.indexOf(0) >= 4) { finishRound(); finishRound(); }
}
function endSeasonPhase() {
  const s = C.season; s.phase = 'done'; const fin = s.po.res.find(r => r.stage === 'final'), cupW = fin.w, lt = table(s), leagueW = lt[0].id, pos = lt.findIndex(r => r.id === 0) + 1;
  if (leagueW === 0) trophy(`${TIERS[s.tier].name} champions`, 'league');
  if (cupW === 0) trophy(`${TIERS[s.tier].cup} winners`, 'cup');
  s.summary = { pos, leagueW, cupW, tier: s.tier, playoff: s.po.res.filter(r => r.a === 0 || r.b === 0).map(r => r.w === 0 ? 'W' : 'L').join('') };
  news(`${TIERS[s.tier].cup}: ${teamOf(cupW).name} win the final.`);
}
const trophy = (name, kind) => { C.trophies.push({ season: C.seasonNo, name, kind }); news(`🏆 ${teamOf(0).name}: ${name}!`); };
// The whole world's season ends: the other leagues are decided, then promotion and relegation (2 up, 2 down).
function nextSeason() {
  const s = C.season, fame = { season: C.seasonNo, tiers: [] }, final = {};
  for (let tier = 0; tier < 4; tier++) { let order, cupW;
    if (tier === s.tier) { order = table(s).map(r => r.id); cupW = s.summary.cupW; }
    else { const ids = C.teams.filter(t => t.tier === tier).map(t => t.id), ss = { teams: ids, res: [] };
      for (const md of fixtures(ids)) ss.res.push(md.map(([a, b]) => simMatch(a, b)));
      order = table(ss).map(r => r.id); const s1 = simMatch(order[0], order[3]).w, s2 = simMatch(order[1], order[2]).w; cupW = simMatch(s1, s2).w; }
    final[tier] = order; fame.tiers.push({ tier, league: teamOf(order[0]).name, cup: teamOf(cupW).name }); }
  C.fame.push(fame);
  const me = teamOf(0), before = me.tier;
  for (let tier = 0; tier < 4; tier++) { const o = final[tier];
    if (tier > 0) for (const id of o.slice(0, 2)) teamOf(id).tier = tier - 1;
    if (tier < 3) for (const id of o.slice(-2)) teamOf(id).tier = tier + 1; }
  C.seasons.push({ season: C.seasonNo, tier: before, pos: s.summary.pos, playoff: s.summary.playoff, after: me.tier, rating: me.rating });
  if (me.tier < before) { trophy(`Promoted to the ${TIERS[me.tier].name}`, 'promo'); }
  else if (me.tier > before) news(`Relegated to the ${TIERS[me.tier].name}.`);
  for (const t of C.teams) { t.rating = Math.round(t.rating * .85 + TIERS[t.tier].base * .15); if (!t.human && crng() < .3) driftPool(t); }   // ratings settle toward the new league; some teams change a favourite
  C.seasonNo++; startSeason(); news(`Season ${C.seasonNo} begins in the ${TIERS[me.tier].name}.`); saveCareer();
}
function driftPool(t) { const p = cpick(t.players), ids = roleChamps(p.role).filter(x => !p.pool[x] && !t.dislikes.includes(x)); if (!ids.length) return;
  const old = Object.keys(p.pool).sort((a, b) => p.pool[a] - p.pool[b])[0], nu = cpick(ids); const w = p.pool[old]; delete p.pool[old]; p.pool[nu] = w;
  news(`${t.name}: ${p.name} swaps ${defOf(old).name} for ${defOf(nu).name} in their pool.`); }
const news = txt => { C.news.unshift({ s: C.seasonNo, t: txt }); C.news = C.news.slice(0, 60); };

// ── playing your match ──
function aiFor(tid) { const t = teamOf(tid), a = { ...TIERS[t.tier].ai }, off = (t.rating - TIERS[t.tier].base) / 60;   // within a league, stronger teams make fewer mistakes
  a.noise = Math.max(0, a.noise - off * .4); a.power = +(a.power + Math.max(-.03, Math.min(.03, off * .01))).toFixed(3); return a; }
function careerPlay(mode) {
  const pair = myPair(); if (!pair) return; const opp = pair[0] === 0 ? pair[1] : pair[0];
  NEXT_CAREER = { opp, stage: stageName(), names: { A: teamOf(0).name, B: teamOf(opp).name }, players: { A: teamOf(0).players.map(p => p.name), B: teamOf(opp).players.map(p => p.name) }, ai: { B: aiFor(opp), A: mode === 'watch' ? MY_AI : null }, power: { A: 1, B: aiFor(opp).power }, prefs: { B: prefsOf(opp, 0), A: null }, rating0: teamOf(0).rating };
  C.pending = { opp, stage: stageName() }; saveCareer();
  UI.career = false; newMatch(mode, Math.floor(crng() * 2147483647));
}
function careerDraftBias(d, team, kind) {
  const pr = G.career && G.career.prefs && G.career.prefs[team]; if (!pr) return 0;
  if (kind === 'pick') return pr.dislikes.has(d.id) ? -60 : (pr.pool[d.id] || 0) * 1.2;
  return (pr.banTargets[d.id] || 0) - (pr.pool[d.id] ? 4 : 0) - (pr.dislikes.has(d.id) ? 3 : 0);
}
function careerComfort(c) {
  const pr = G.career && G.career.prefs && G.career.prefs[c.team]; if (!pr || !pr.dislikes.has(c.def.id)) return;
  c.atk = Math.max(1, c.atk - UNCOMFY.atk); c.maxHp = Math.round(c.maxHp * UNCOMFY.hp); c.hp = c.maxHp; c.uncomfy = true;
}
function careerAfterMatch() {   // index.html's finish() calls this once a career match is over
  if (!C) loadCareer(); const ctx = G.career, opp = ctx.opp, w = G.winner === 'A' ? 0 : G.winner === 'B' ? opp : null;
  const ka = G.champs.filter(c => c.team === 'A').reduce((s, c) => s + c.kills, 0), kb = G.champs.filter(c => c.team === 'B').reduce((s, c) => s + c.kills, 0);
  const picks = t => Object.fromEntries(G.picks[t].map(d => [d.role, d.id])), bans = t => G.banList[t].map(d => d.id);
  const dr = { picks: { 0: picks('A'), [opp]: picks('B') }, bans: { 0: bans('A'), [opp]: bans('B') } };
  const killsBy = Object.fromEntries(G.champs.filter(c => c.team === 'A').map(c => [c.def.id, c.kills]));
  const d = recordMine({ opp, w, ka, kb, dr, killsBy, rounds: G.round, watched: G.mode === 'watch' });
  ctx.note = `${ctx.stage} · rating ${teamOf(0).rating - d} → ${teamOf(0).rating} (${d >= 0 ? '+' : ''}${d})`;
}
function careerSimMine() {   // decide your match by ratings, without playing it
  const pair = myPair(); if (!pair) return; const opp = pair[0] === 0 ? pair[1] : pair[0], dr = virtualDraft(0, opp), w = crng() < winP(0, opp, dr) ? 0 : opp;
  const kw = 8 + Math.floor(crng() * 10), kl = Math.floor(crng() * kw * .8);
  recordMine({ opp, w, ka: w === 0 ? kw : kl, kb: w === 0 ? kl : kw, dr, killsBy: null, rounds: 0, simulated: true }); render();
}
function recordMine({ opp, w, ka, kb, dr, killsBy, rounds, simulated, watched }) {
  const S = C.stats, stage = stageName(), res = w === 0 ? 'W' : w == null ? 'D' : 'L';
  recordDraft(0, opp, dr, w);
  const d = elo(0, opp, w, simulated ? 24 : 32);
  S.g++; S[res === 'W' ? 'w' : res === 'L' ? 'l' : 'd']++; S.kf += ka; S.ka += kb;
  const v = S.vs[opp] || (S.vs[opp] = { g: 0, w: 0 }); v.g++; if (res === 'W') v.w++;
  for (const [role, id] of Object.entries(dr.picks[0])) { const z = S.champs[id] || (S.champs[id] = { g: 0, w: 0, k: 0 }); z.g++; if (res === 'W') z.w++; z.k += killsBy ? killsBy[id] || 0 : 0;
    const pl = teamOf(0).players[ROLES.indexOf(role)], y = S.players[pl.name] || (S.players[pl.name] = { role, g: 0, w: 0, k: 0, champs: {} }); y.g++; if (res === 'W') y.w++; y.k += killsBy ? killsBy[id] || 0 : 0; y.champs[id] = (y.champs[id] || 0) + 1; }
  S.history.unshift({ s: C.seasonNo, tier: C.season.tier, stage, opp, res, ka, kb, rounds, mine: Object.values(dr.picks[0]), theirs: Object.values(dr.picks[opp]), bans: dr.bans[0], tbans: dr.bans[opp], d, sim: !!simulated, watched: !!watched });
  S.history = S.history.slice(0, 150);
  news(`${stage}: ${teamOf(0).short} ${ka}–${kb} ${teamOf(opp).short} (${res === 'W' ? 'win' : res === 'L' ? 'loss' : 'draw'}${simulated ? ', simulated' : ''})`);
  storeResult({ a: 0, b: opp, w, ka, kb, sim: !!simulated }); C.pending = null; finishRound(); saveCareer(); return d;
}

// ── saving ──
function saveCareer() { if (!C) return; try { localStorage.setItem(CSLOT(C.slot), JSON.stringify(C)); localStorage.setItem(CLAST, String(C.slot)); } catch (e) { console.warn('career save failed', e); } }
function readSlot(n) { try { const s = localStorage.getItem(CSLOT(n)); const c = s && JSON.parse(s); return c && c.v === CAREER_V ? c : null; } catch (e) { return null; } }
function loadCareer(n) { if (n == null) { try { n = +localStorage.getItem(CLAST) || null; } catch (e) { n = null; } } const c = n && readSlot(n); C = c || null; return C; }
function exportCareer() { const blob = new Blob([JSON.stringify(C, null, 1)], { type: 'application/json' }), a = document.createElement('a');
  a.href = URL.createObjectURL(blob); a.download = `augury-career-${teamOf(0).short}-s${C.seasonNo}.json`; a.click(); setTimeout(() => URL.revokeObjectURL(a.href), 2000); }
function importCareer(file, slot) { const r = new FileReader(); r.onload = () => { try { const c = JSON.parse(r.result); if (c.v !== CAREER_V || !c.teams || !c.season) throw new Error('not a career save'); c.slot = slot; C = c; saveCareer(); UI.ctab = 'home'; render(); } catch (e) { alert('That file is not an Augury career save.'); } }; r.readAsText(file); }

// ═════════════ the career screens ═════════════
function careerOpen() { if (!C) loadCareer(); UI.career = true; UI.ctab = C ? (UI.ctab && UI.ctab !== 'saves' ? UI.ctab : 'home') : 'saves'; render(); }
const crest = (t, size = 34) => `<svg class="ccrest" width="${size}" height="${size}" viewBox="0 0 40 40"><polygon points="20,2 37,11 37,29 20,38 3,29 3,11" fill="hsl(${t.hue} 55% 32%)" stroke="hsl(${t.hue} 70% 62%)" stroke-width="2.5"></polygon><text x="20" y="25" text-anchor="middle" style="font:800 ${t.short.length > 3 ? 9 : 11}px var(--display, sans-serif);fill:#fff">${esc(t.short)}</text></svg>`;
const tname = (t, me) => `<span class="ctn ${t.id === 0 ? 'me' : ''}">${crest(t, 22)}<b>${esc(t.name)}</b></span>`;
const pic = (id, cls = '') => `<img class="cpic ${cls}" src="art/${id}.png" alt="" title="${esc(defOf(id) ? defOf(id).name : id)}">`;
const cpct = (a, b) => b ? Math.round(100 * a / b) + '%' : '–';
const tierTag = tier => `<span class="ctier" style="color:${TIERS[tier].col};border-color:${TIERS[tier].col}">${TIERS[tier].short}</span>`;
const cformHtml = f => f.slice(-5).map(x => `<i class="cf ${x}">${x}</i>`).join('');
const ladder = () => [...C.teams].sort((a, b) => b.rating - a.rating);

function careerView() {
  const box = h(`<div class="box career"></div>`);
  if (!C || UI.ctab === 'saves') box.innerHTML = savesHtml();
  else { const me = teamOf(0), s = C.season, rank = ladder().findIndex(t => t.id === 0) + 1;
    const tabs = [['home', 'Overview'], ['league', 'League'], ['ladder', 'Ladder'], ['scout', 'Scouting'], ['stats', 'Statistics'], ['trophies', 'Trophies'], ['saves', 'Saves']];
    box.innerHTML = `<div class="chead">${crest(me, 52)}<div class="chn"><h2>${esc(me.name)}</h2><span>Manager ${esc(C.manager)} · Season ${C.seasonNo} · ${tierTag(s.tier)} ${esc(TIERS[s.tier].name)} · ${esc(stageName())}</span></div>
      <div class="cstat"><b>${me.rating}</b><span>rating</span></div><div class="cstat"><b>#${rank}</b><span>ladder</span></div><div class="cstat"><b>${C.stats.w}–${C.stats.l}</b><span>career</span></div>
      <button data-act="menu" class="cmenu">Main menu</button></div>
      <div class="ctabs">${tabs.map(([k, n]) => `<button data-act="tab" data-v="${k}" class="${UI.ctab === k ? 'on' : ''}">${n}</button>`).join('')}</div>
      <div class="cbody">${({ home: homeHtml, league: leagueHtml, ladder: ladderHtml, scout: scoutHtml, stats: statsHtml, trophies: trophiesHtml }[UI.ctab] || homeHtml)()}</div>`; }
  box.addEventListener('click', ev => { const b = ev.target.closest('[data-act]'); if (!b) return; const a = b.dataset.act, v = b.dataset.v;
    if (a === 'tab') { UI.ctab = v; render(); }
    else if (a === 'menu') { UI.career = false; UI.title = true; render(); }
    else if (a === 'play') careerPlay('play');
    else if (a === 'watch') careerPlay('watch');
    else if (a === 'sim') { if (confirm('Decide this match by ratings, without playing it?')) careerSimMine(); }
    else if (a === 'scout') { UI.scoutId = +v; UI.ctab = 'scout'; render(); }
    else if (a === 'next') { nextSeason(); render(); }
    else if (a === 'new') { const n = +v, f = box.querySelector(`#cnew${n}`), get = k => f.querySelector(`[name=${k}]`).value.trim();
      if (readSlot(n) && !confirm('Overwrite the career in this slot?')) return; newCareer(n, get('mgr'), get('team') || 'Augury Rookies', get('tag') || 'AUG'); UI.ctab = 'home'; render(); }
    else if (a === 'load') { loadCareer(+v); UI.ctab = 'home'; render(); }
    else if (a === 'del') { if (confirm('Delete this career for good?')) { try { localStorage.removeItem(CSLOT(+v)); } catch (e) {} if (C && C.slot === +v) C = null; render(); } }
    else if (a === 'export') exportCareer();
    else if (a === 'import') { const inp = box.querySelector(`#cimp${v}`); inp.onchange = () => inp.files[0] && importCareer(inp.files[0], +v); inp.click(); } });
  return box;
}
function homeHtml() {
  const s = C.season, pair = myPair(), t = table();
  let next = '';
  if (s.phase === 'done') { const sm = s.summary;
    next = `<div class="cnext done"><h3>Season ${C.seasonNo} is over</h3><p>You finished <b>${sm.pos}${['st', 'nd', 'rd'][sm.pos - 1] || 'th'}</b> in the ${esc(TIERS[s.tier].name)}${sm.playoff ? `; playoffs: ${sm.playoff.split('').map(x => x === 'W' ? 'won' : 'lost').join(', then ')}` : ', outside the playoffs'}.
      League winners: ${tname(teamOf(sm.leagueW))}. ${esc(TIERS[s.tier].cup)}: ${tname(teamOf(sm.cupW))}.</p>
      <p>${s.tier > 0 && sm.pos <= 2 ? `🎉 Top two: you go up to the <b>${esc(TIERS[s.tier - 1].name)}</b>.` : s.tier < 3 && sm.pos >= 7 ? `Bottom two: you go down to the <b>${esc(TIERS[s.tier + 1].name)}</b>.` : `You stay in the ${esc(TIERS[s.tier].name)}.`}</p>
      <button class="primary big" data-act="next">Start season ${C.seasonNo + 1}</button></div>`; }
  else if (pair) { const opp = teamOf(pair[0] === 0 ? pair[1] : pair[0]), r = opp.rec, top = Object.entries(r.picks).sort((a, b) => b[1] - a[1]).slice(0, 5).map(([id]) => pic(id)).join('');
    const p = Math.round(100 * expect(teamOf(0).rating, opp.rating));
    next = `<div class="cnext"><div class="cnl">${esc(stageName())}${C.pending ? ' · <span class="cwarn">started, not finished: it will be replayed</span>' : ''}</div>
      <div class="cvs">${crest(teamOf(0), 56)}<span class="vs">VS</span>${crest(opp, 56)}<div><h3>${esc(opp.name)}</h3><span>${tierTag(opp.tier)} rating <b>${opp.rating}</b> · season ${tableRow(opp.id)} · you win about ${p}% on ratings</span>
      <div class="cfav">Most picked: ${top}</div><div class="cfav">Never picks: ${neverPicks(opp).map(id => pic(id, 'no')).join('') || '–'}</div></div></div>
      <div class="crow"><button class="primary big" data-act="play">Play the match</button><button data-act="scout" data-v="${opp.id}">Scout ${esc(opp.short)}</button><button data-act="watch">Let the AI play it</button><button data-act="sim">Simulate the result</button></div></div>`; }
  else next = `<div class="cnext"><p>No match for you in this round.</p></div>`;
  return `<div class="cgrid"><div>${next}<h3>${esc(TIERS[s.tier].name)}</h3>${tableHtml(t, true)}</div>
    <div><h3>Your team</h3><div class="croster">${teamOf(0).players.map(p => { const y = C.stats.players[p.name] || { g: 0, w: 0, champs: {} }, fav = Object.entries(y.champs).sort((a, b) => b[1] - a[1]).slice(0, 3).map(([id]) => pic(id)).join('');
      return `<div class="cpl"><span class="cro">${p.role}</span><b>${esc(p.name)}</b><span>${y.g ? `${y.g} games · ${cpct(y.w, y.g)} won` : 'no games yet'}</span><span class="cfv">${fav}</span></div>`; }).join('')}</div>
    <h3>News</h3><div class="cnews">${C.news.slice(0, 14).map(n => `<div><i>S${n.s}</i> ${esc(n.t).replace(/🏆/g, '🏆')}</div>`).join('')}</div></div></div>`;
}
const tableRow = id => { const r = table().find(x => x.id === id); return r ? `${r.w}–${r.l}${r.d ? '–' + r.d : ''}` : '–'; };
function tableHtml(t, short) {
  return `<table class="ctab"><tr><th>#</th><th>Team</th><th>P</th><th>W</th><th>L</th><th>Pts</th>${short ? '' : '<th>Kills</th>'}<th>Form</th><th>Rating</th></tr>${t.map((r, i) => { const tm = teamOf(r.id);
    return `<tr class="${r.id === 0 ? 'me' : ''} ${i < 2 && C.season.tier > 0 ? 'up' : ''} ${i >= 6 && C.season.tier < 3 ? 'down' : ''} ${i === 3 ? 'cut' : ''}"><td>${i + 1}</td><td><a data-act="scout" data-v="${r.id}">${tname(tm)}</a></td><td>${r.p}</td><td>${r.w}</td><td>${r.l}</td><td><b>${r.pts}</b></td>${short ? '' : `<td>${r.kf}–${r.ka}</td>`}<td>${cformHtml(r.form)}</td><td>${tm.rating}</td></tr>`; }).join('')}</table>
    <p class="cnote">Top 4 go to the playoffs. ${C.season.tier > 0 ? 'Top 2 are promoted. ' : ''}${C.season.tier < 3 ? 'Bottom 2 are relegated.' : ''}</p>`;
}
function leagueHtml() {
  const s = C.season, res = (md, a, b) => (s.res[md] || []).find(r => r.a === a && r.b === b || r.a === b && r.b === a);
  const fx = s.fx.map((md, i) => `<div class="cmd ${i === s.md && s.phase === 'league' ? 'now' : ''}"><b>Matchday ${i + 1}</b>${md.map(([a, b]) => { const r = res(i, a, b);
    return `<div class="cfx ${a === 0 || b === 0 ? 'mine' : ''}">${tname(teamOf(a))}<span class="sc">${r ? `${r.a === a ? r.ka : r.kb}–${r.a === a ? r.kb : r.ka}${r.sim && (a === 0 || b === 0) ? '*' : ''}` : 'vs'}</span>${tname(teamOf(b))}${r ? `<i>${r.w == null ? 'draw' : esc(teamOf(r.w).short) + ' win'}</i>` : ''}</div>`; }).join('')}</div>`).join('');
  const po = s.po ? `<h3>Playoffs</h3><div class="cpo">${s.po.semis.map(p => `<div class="cfx ${p.includes(0) ? 'mine' : ''}">${tname(teamOf(p[0]))}<span class="sc">semi</span>${tname(teamOf(p[1]))}${(() => { const r = s.po.res.find(x => x.stage === 'semi' && p.includes(x.a) && p.includes(x.b)); return r ? `<i>${esc(teamOf(r.w).short)} win</i>` : ''; })()}</div>`).join('')}
    ${s.po.final ? `<div class="cfx final ${s.po.final.includes(0) ? 'mine' : ''}">${tname(teamOf(s.po.final[0]))}<span class="sc">FINAL</span>${tname(teamOf(s.po.final[1]))}${(() => { const r = s.po.res.find(x => x.stage === 'final'); return r ? `<i>🏆 ${esc(teamOf(r.w).name)}</i>` : ''; })()}</div>` : ''}</div>` : '';
  return `<div class="cgrid"><div><h3>${esc(TIERS[s.tier].name)} · season ${C.seasonNo}</h3>${tableHtml(table(), false)}${po}</div><div><h3>Fixtures</h3><div class="cfxs">${fx}</div><p class="cnote">* decided by ratings (simulated).</p></div></div>`;
}
function ladderHtml() {
  const L = ladder();
  return `<h3>The ladder: every team, by rating</h3><p class="cnote">Ratings move after every match (Elo): beating a stronger team gains more. Promotion and relegation follow the league tables; the ladder shows who is really strongest.</p>
    <table class="ctab"><tr><th>#</th><th>Team</th><th>League</th><th>Rating</th><th>Season start</th><th>Record (all time)</th></tr>${L.map((t, i) => { const d = t.rating - (C.season.start[t.id] ?? t.rating);
      return `<tr class="${t.id === 0 ? 'me' : ''}"><td>${i + 1}</td><td><a data-act="scout" data-v="${t.id}">${tname(t)}</a></td><td>${tierTag(t.tier)}</td><td><b>${t.rating}</b> <i class="${d >= 0 ? 'up' : 'dn'}">${d >= 0 ? '+' : ''}${d}</i></td><td>${C.season.start[t.id] ?? ''}</td><td>${t.rec.w}–${t.rec.l}</td></tr>`; }).join('')}</table>`;
}
const neverPicks = t => t.rec.g >= 3 ? t.dislikes.filter(id => !t.rec.picks[id]) : [];
function scoutHtml() {
  const pair = myPair(), def = pair ? (pair[0] === 0 ? pair[1] : pair[0]) : C.season.teams.find(x => x !== 0);
  const id = UI.scoutId != null && teamOf(UI.scoutId) ? UI.scoutId : def, t = teamOf(id), r = t.rec;
  const opts = [...C.season.teams.filter(x => x !== 0), ...ladder().filter(x => !C.season.teams.includes(x.id)).map(x => x.id)].map(x => `<button data-act="scout" data-v="${x}" class="${x === id ? 'on' : ''}">${crest(teamOf(x), 18)} ${esc(teamOf(x).short)}</button>`).join('');
  if (t.human) return `<div class="cscoutbar">${opts}</div><p>This is your own team: see Statistics.</p>`;
  const fams = {}; for (const [cid, n] of Object.entries(r.picks)) { const f = FAMILY[cid]; if (f) fams[f] = (fams[f] || 0) + n; }
  const style = Object.entries(fams).sort((a, b) => b[1] - a[1]).slice(0, 2).map(([f]) => f);
  const roles = t.players.map(p => { const champs = roleChamps(p.role).map(cid => ({ cid, n: r.picks[cid] || 0, wins: r.champW[cid] || 0 })).sort((a, b) => b.n - a.n);
    const tot = champs.reduce((s, x) => s + x.n, 0) || 1;
    return `<tr><td><b>${esc(p.name)}</b><br><i>${p.role}</i></td><td class="cpool">${champs.map(x => `<span class="cpc ${t.dislikes.includes(x.cid) && !x.n ? 'no' : ''}">${pic(x.cid)}<b>${x.n ? Math.round(100 * x.n / tot) + '%' : t.dislikes.includes(x.cid) && r.g >= 3 ? 'never' : '0%'}</b>${x.n ? `<i>${cpct(x.wins, x.n)} won</i>` : ''}</span>`).join('')}</td></tr>`; }).join('');
  const bans = Object.entries(r.bans).sort((a, b) => b[1] - a[1]).slice(0, 5).map(([cid, n]) => `<span class="cpc">${pic(cid)}<b>${cpct(n, r.g)}</b></span>`).join('');
  const vs = C.stats.vs[id];
  return `<div class="cscoutbar">${opts}</div>
    <div class="cscout"><div class="cvs">${crest(t, 60)}<div><h3>${esc(t.name)}</h3><span>${tierTag(t.tier)} rating <b>${t.rating}</b> · ladder #${ladder().findIndex(x => x.id === id) + 1} · ${r.w}–${r.l} in ${r.g} games on record · plays ${style.length ? style.join(' and ') + ' shapes most' : '–'}${vs ? ` · against you: ${vs.w}–${vs.g - vs.w}` : ''}</span></div></div>
    <h3>Champion pools (share of that player's games)</h3><table class="ctab cpools">${roles}</table>
    <div class="cgrid"><div><h3>They ban most</h3><div class="cpool">${bans || '–'}</div></div><div><h3>Draft advice</h3><ul class="cadv">${advice(t).map(x => `<li>${x}</li>`).join('')}</ul></div></div>
    <p class="cnote">A team that has to play a champion it never picks plays it uncomfortably: −1 attack and −10% HP (😣 on its card).</p></div>`;
}
function advice(t) {
  const out = [], r = t.rec;
  for (const p of t.players) { const champs = roleChamps(p.role), liked = champs.filter(cid => (r.picks[cid] || 0) / Math.max(1, r.g) >= .2), hated = neverPicks(t).filter(cid => defOf(cid).role === p.role);
    const other = champs.filter(cid => !liked.includes(cid) && !hated.includes(cid));
    if (liked.length && liked.length <= 2 && hated.length && other.length <= 1) out.push(`Ban ${liked.map(cid => '<b>' + defOf(cid).name + '</b>').join(' and ')}${other.length ? ` and pick <b>${defOf(other[0]).name}</b> yourself` : ''}: ${esc(p.name)} (${p.role}) is left with ${hated.map(cid => defOf(cid).name).join(' or ')}, which they never play.`);
    else if (liked.length === 1) out.push(`${esc(p.name)} (${p.role}) plays <b>${defOf(liked[0]).name}</b> in ${cpct(r.picks[liked[0]], r.g)} of games: a ban there hurts them.`); }
  const myTop = Object.entries(C.stats.champs).sort((a, b) => b[1].g - a[1].g)[0];
  if (myTop && (r.bans[myTop[0]] || 0) / Math.max(1, r.g) > .2) out.push(`They often ban <b>${defOf(myTop[0]).name}</b>, your most-played champion: have a second plan.`);
  if (TIERS[t.tier].scout > 0) out.push(`Teams in the ${TIERS[t.tier].name} scout you: expect bans on your most-played champions.`);
  return out.length ? out : ['No clear weakness on record: draft for your own patterns.'];
}
function statsHtml() {
  const S = C.stats, me = teamOf(0);
  const champs = Object.entries(S.champs).sort((a, b) => b[1].g - a[1].g).map(([id, z]) => `<tr><td>${pic(id)} ${esc(defOf(id).name)}</td><td>${z.g}</td><td>${cpct(z.w, z.g)}</td><td>${z.k}</td></tr>`).join('');
  const players = me.players.map(p => { const y = S.players[p.name] || { g: 0, w: 0, k: 0, champs: {} }; const fav = Object.entries(y.champs).sort((a, b) => b[1] - a[1]).slice(0, 3).map(([id]) => pic(id)).join('');
    return `<tr><td><b>${esc(p.name)}</b></td><td>${p.role}</td><td>${y.g}</td><td>${cpct(y.w, y.g)}</td><td>${y.k}</td><td>${fav}</td></tr>`; }).join('');
  const vs = Object.entries(S.vs).sort((a, b) => b[1].g - a[1].g).map(([id, v]) => `<tr><td>${tname(teamOf(id))}</td><td>${v.w}–${v.g - v.w}</td></tr>`).join('');
  const hist = S.history.slice(0, 25).map(x => `<tr class="${x.res}"><td>S${x.s} ${esc(x.stage)}</td><td>${tname(teamOf(x.opp))}</td><td><b>${x.res}</b> ${x.ka}–${x.kb}${x.sim ? '*' : x.watched ? ' (AI)' : ''}</td><td>${x.mine.map(id => pic(id)).join('')}</td><td>${x.theirs.map(id => pic(id)).join('')}</td><td>${x.d >= 0 ? '+' : ''}${x.d}</td></tr>`).join('');
  return `<div class="cgrid3"><div class="cstat big"><b>${S.g}</b><span>matches</span></div><div class="cstat big"><b>${cpct(S.w, S.g)}</b><span>won (${S.w}–${S.l}${S.d ? '–' + S.d : ''})</span></div><div class="cstat big"><b>${S.kf}–${S.ka}</b><span>kills for–against</span></div><div class="cstat big"><b>${C.trophies.filter(x => x.kind !== 'promo').length}</b><span>trophies</span></div></div>
    <div class="cgrid"><div><h3>Your players</h3><table class="ctab"><tr><th>Player</th><th>Role</th><th>Games</th><th>Won</th><th>Kills</th><th>Most played</th></tr>${players}</table>
      <h3>Champions</h3><table class="ctab"><tr><th>Champion</th><th>Games</th><th>Won</th><th>Kills</th></tr>${champs || '<tr><td colspan=4>No matches yet.</td></tr>'}</table></div>
      <div><h3>Head to head</h3><table class="ctab"><tr><th>Team</th><th>Record</th></tr>${vs || '<tr><td colspan=2>–</td></tr>'}</table></div></div>
    <h3>Recent matches</h3><table class="ctab"><tr><th>When</th><th>Against</th><th>Result</th><th>You picked</th><th>They picked</th><th>Rating</th></tr>${hist || '<tr><td colspan=6>No matches yet.</td></tr>'}</table><p class="cnote">* simulated · (AI) played by the AI for you.</p>`;
}
function trophiesHtml() {
  const tr = C.trophies.map(x => `<div class="ctro ${x.kind}"><span>${x.kind === 'cup' ? '🏆' : x.kind === 'league' ? '🥇' : '⬆️'}</span><b>${esc(x.name)}</b><i>season ${x.season}</i></div>`).join('');
  const seasons = C.seasons.map(x => `<tr><td>Season ${x.season}</td><td>${tierTag(x.tier)}</td><td>${x.pos}${['st', 'nd', 'rd'][x.pos - 1] || 'th'}</td><td>${x.playoff ? x.playoff.split('').join(' ') : '–'}</td><td>${x.after < x.tier ? '⬆ promoted' : x.after > x.tier ? '⬇ relegated' : '–'}</td><td>${x.rating}</td></tr>`).join('');
  const fame = C.fame.map(f => `<tr><td>Season ${f.season}</td>${f.tiers.map(x => `<td>${esc(x.league)}<br><i>cup: ${esc(x.cup)}</i></td>`).join('')}</tr>`).join('');
  return `<h3>Your trophy cabinet</h3><div class="ctros">${tr || '<p>Nothing yet. Win a league or a cup.</p>'}</div>
    <h3>Your seasons</h3><table class="ctab"><tr><th>Season</th><th>League</th><th>Finished</th><th>Playoffs</th><th></th><th>Rating</th></tr>${seasons || '<tr><td colspan=6>Your first season is under way.</td></tr>'}</table>
    <h3>Hall of fame</h3><table class="ctab"><tr><th></th>${TIERS.map(t => `<th>${t.name}</th>`).join('')}</tr>${fame || '<tr><td colspan=5>–</td></tr>'}</table>`;
}
function savesHtml() {
  const slots = [1, 2, 3].map(n => { const c = readSlot(n);
    if (c) { const me = c.teams[0]; return `<div class="cslot ${C && C.slot === n ? 'on' : ''}"><div class="cvs">${crest(me, 44)}<div><h3>${esc(me.name)}</h3><span>Manager ${esc(c.manager)} · season ${c.seasonNo} · ${TIERS[me.tier].name} · ${c.stats.w}–${c.stats.l} · rating ${me.rating}</span></div></div>
      <div class="crow"><button class="primary" data-act="load" data-v="${n}">${C && C.slot === n ? 'Continue' : 'Load'}</button><button data-act="export" ${C && C.slot === n ? '' : 'disabled'}>Export to a file</button><button data-act="del" data-v="${n}">Delete</button></div></div>`; }
    return `<div class="cslot empty"><h3>Slot ${n}: empty</h3><form id="cnew${n}" onsubmit="return false" class="cform"><label>Manager <input name="mgr" placeholder="your name" maxlength="20"></label><label>Team <input name="team" placeholder="Augury Rookies" maxlength="24"></label><label>Tag <input name="tag" placeholder="AUG" maxlength="4" style="width:5em"></label></form>
      <div class="crow"><button class="primary" data-act="new" data-v="${n}">Start a new career</button><button data-act="import" data-v="${n}">Import a save file</button><input type="file" id="cimp${n}" accept=".json,application/json" hidden></div></div>`; }).join('');
  return `<div class="chead"><h2>Career · saves</h2><button data-act="menu" class="cmenu">Main menu</button>${C ? '<button data-act="tab" data-v="home">Back</button>' : ''}</div>
    <p>A career is saved in this browser after every match, in one of three slots. Export it to a file to keep it safe or move it to another computer; import puts a file back in a slot.</p><div class="cslots">${slots}</div>`;
}

// ── styles (kept here so the career stays one file) ──
document.head.appendChild(Object.assign(document.createElement('style'), { textContent: `
.overlay.careerov { align-items: flex-start; }
.box.career { width: min(1240px, 100%); gap: 10px; }
.career h3 { font-family: var(--display); font-weight: 400; margin: 10px 0 6px; font-size: 16px; letter-spacing: .03em; color: var(--gold); }
.chead { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; } .chead h2 { margin: 0; } .chn { display: flex; flex-direction: column; gap: 2px; } .chn span { color: var(--dim); font-size: 13px; }
.cmenu { margin-left: auto; } .cstat { display: flex; flex-direction: column; align-items: center; padding: 4px 12px; border: 1px solid var(--line); border-radius: 10px; background: #0b1020aa; min-width: 70px; }
.cstat b { font-family: var(--display); font-size: 20px; color: #fff; font-weight: 400; } .cstat span { font-size: 11px; color: var(--dim); } .cstat.big b { font-size: 28px; }
.ctabs { display: flex; gap: 4px; flex-wrap: wrap; border-bottom: 1px solid var(--line); padding-bottom: 6px; } .ctabs button.on { border-color: var(--gold); color: var(--gold); background: #2c2614; }
.cgrid { display: grid; grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr); gap: 16px; } .cgrid3 { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; }
@media (max-width: 900px) { .cgrid, .cgrid3 { grid-template-columns: 1fr; } }
.ctab { width: 100%; border-collapse: collapse; font-size: 13px; } .ctab th { text-align: left; font-size: 11px; color: var(--dim); font-weight: 700; padding: 3px 6px; border-bottom: 1px solid var(--line); }
.ctab td { padding: 3px 6px; border-bottom: 1px solid #ffffff0d; vertical-align: middle; } .ctab tr.me td { background: #e7b75a1f; } .ctab tr.up td:first-child { box-shadow: inset 3px 0 #7bd88f; } .ctab tr.down td:first-child { box-shadow: inset 3px 0 #ff6b6b; } .ctab tr.cut td { border-bottom: 1px dashed var(--gold); }
.ctab tr.W td:nth-child(3) b { color: var(--good); } .ctab tr.L td:nth-child(3) b { color: var(--bad); } .ctab a { cursor: pointer; } .ctab i.up { color: var(--good); font-style: normal; } .ctab i.dn { color: var(--bad); font-style: normal; }
.ctn { display: inline-flex; align-items: center; gap: 6px; } .ctn.me b { color: var(--gold); } .ccrest { flex: none; vertical-align: middle; }
.ctier { font-size: 10px; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; border: 1px solid; border-radius: 6px; padding: 0 5px; }
.cf { display: inline-block; width: 15px; text-align: center; font-style: normal; font-size: 10px; font-weight: 800; border-radius: 3px; margin-right: 1px; } .cf.W { background: #2f6b3a; } .cf.L { background: #7a2a2a; } .cf.D { background: #555; }
.cnext { border: 1px solid var(--gold); border-radius: 12px; padding: 12px; background: linear-gradient(180deg, #2a2210aa, #0b1020aa); display: flex; flex-direction: column; gap: 8px; }
.cnext h3 { margin: 0; color: #fff; font-size: 22px; } .cnl { font-size: 12px; letter-spacing: .1em; text-transform: uppercase; color: var(--gold); font-weight: 800; } .cwarn { color: var(--fire); text-transform: none; letter-spacing: 0; }
.cvs { display: flex; align-items: center; gap: 12px; } .cvs span { color: var(--dim); font-size: 13px; } .cvs .vs { font-family: var(--display); font-size: 22px; color: var(--gold); }
.cfav { display: flex; align-items: center; gap: 3px; font-size: 12px; color: var(--dim); margin-top: 4px; }
.cpic { width: 26px; height: 26px; object-fit: cover; object-position: top; border-radius: 6px; border: 1px solid var(--line); background: #0b1020; vertical-align: middle; } .cpic.no { filter: grayscale(1) brightness(.7); border-color: var(--bad); }
.crow { display: flex; gap: 6px; flex-wrap: wrap; align-items: center; }
.cnews { display: flex; flex-direction: column; gap: 4px; font-size: 13px; max-height: 430px; overflow: auto; } .cnews i { color: var(--dim); font-style: normal; font-size: 11px; margin-right: 4px; }
.cfxs { display: flex; flex-direction: column; gap: 8px; max-height: 520px; overflow: auto; } .cmd { display: flex; flex-direction: column; gap: 2px; font-size: 12px; } .cmd.now b { color: var(--gold); }
.cfx { display: grid; grid-template-columns: 1fr auto 1fr auto; gap: 6px; align-items: center; padding: 2px 4px; border-radius: 6px; } .cfx.mine { background: #e7b75a1a; } .cfx .sc { font-family: var(--mono, monospace); color: #fff; } .cfx i { color: var(--dim); font-size: 11px; font-style: normal; }
.cfx.final { border: 1px solid var(--gold); } .cpo { display: flex; flex-direction: column; gap: 4px; }
.croster { display: flex; flex-direction: column; gap: 3px; } .cpl { display: grid; grid-template-columns: 60px 1fr auto auto; gap: 8px; align-items: center; font-size: 13px; padding: 2px 4px; border-bottom: 1px solid #ffffff0d; } .cpl .cro { color: var(--dim); font-size: 11px; text-transform: uppercase; letter-spacing: .06em; } .cpl span { color: var(--dim); font-size: 12px; } .cfv { display: flex; gap: 2px; }
.cnote { color: var(--dim); font-size: 12px; margin: 4px 0; }
.cscoutbar { display: flex; flex-wrap: wrap; gap: 3px; } .cscoutbar button { font-size: 11px; padding: 2px 6px; display: inline-flex; align-items: center; gap: 3px; } .cscoutbar button.on { border-color: var(--gold); }
.cpool { display: flex; flex-wrap: wrap; gap: 6px; } .cpc { display: inline-flex; flex-direction: column; align-items: center; font-size: 11px; min-width: 44px; } .cpc .cpic { width: 38px; height: 38px; } .cpc i { color: var(--dim); font-size: 10px; font-style: normal; }
.cpc.no .cpic { filter: grayscale(1) brightness(.6); border-color: var(--bad); } .cpc.no b { color: var(--bad); }
.cadv { margin: 0; padding-left: 18px; font-size: 13px; display: flex; flex-direction: column; gap: 4px; }
.ctros { display: flex; flex-wrap: wrap; gap: 8px; } .ctro { display: flex; flex-direction: column; align-items: center; padding: 10px 14px; border: 1px solid var(--line); border-radius: 12px; background: #0b1020aa; min-width: 140px; }
.ctro span { font-size: 30px; } .ctro i { color: var(--dim); font-size: 11px; font-style: normal; } .ctro.cup, .ctro.league { border-color: var(--gold); }
.cslots { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; } @media (max-width: 900px) { .cslots { grid-template-columns: 1fr; } }
.cslot { border: 1px solid var(--line); border-radius: 12px; padding: 10px; display: flex; flex-direction: column; gap: 8px; background: #0b1020aa; } .cslot.on { border-color: var(--gold); }
.cform { display: flex; flex-direction: column; gap: 6px; font-size: 13px; } .cform label { display: flex; gap: 6px; align-items: center; justify-content: space-between; } .cform input { flex: 1; background: #0b1020; color: #fff; border: 1px solid var(--line); border-radius: 6px; padding: 4px 6px; font: inherit; }
` }));
