# PROTOTYPE - NOT FOR PRODUCTION
# Question: Is the descending initiative ladder a real decision, is a round
#           bounded, and is the search space tractable for an AI?
# Date: 2026-08-14
#
# Throwaway. Python because this container has no .NET; the questions are about
# rules, not about C#. Never import this from src/.

import random
from copy import deepcopy

MAX_INIT = 4
BOARD_RADIUS = 4
POINTS_TO_WIN = 20
ROUND_CAP = 60
KILL_POINTS = 2

OBJECTIVES = [(0, 0), (0, -2), (0, 2)]
HOME = {0: (-3, 0), 1: (3, 0)}


def hex_dist(a, b):
    aq, ar = a
    bq, br = b
    return (abs(aq - bq) + abs(aq + ar - bq - br) + abs(ar - br)) // 2


def in_board(p):
    return hex_dist(p, (0, 0)) <= BOARD_RADIUS


NEIGHBORS = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)]


# ---------------------------------------------------------------- abilities
# kind: dmg | poison | shield | heal | buff | move
# mold: (stat, delta) applied permanently to the user on every use

class Ability:
    __slots__ = ("name", "init", "cd", "kind", "power", "rng", "area", "mold")

    def __init__(self, name, init, cd, kind, power, rng, area=0, mold=None):
        self.name = name
        self.init = init
        self.cd = cd
        self.kind = kind
        self.power = power
        self.rng = rng
        self.area = area
        self.mold = mold


# Every champion also gets Reposition (init 1, no cooldown).
# ASSUMPTION: movement sits inside the ladder as a low-initiative ability.
# This is one of the open questions in the systems index; flagged in REPORT.md.
REPOSITION = Ability("Reposition", 1, 0, "move", 2, 0, mold=None)

TEMPLATES = {
    # committer: strong, high initiative, wide answer windows
    "Vanguard": [
        Ability("Cleave", 4, 2, "dmg", 7, 1, area=1, mold=("atk", 1)),
        Ability("Charge", 3, 2, "dmg", 5, 2, mold=("atk", 1)),
        Ability("Brace", 2, 2, "shield", 6, 0, mold=("dfn", 1)),
        Ability("Jab", 1, 0, "dmg", 3, 1, mold=("atk", 0)),
    ],
    # counterpuncher: lives at the bottom of the ladder
    "Duelist": [
        Ability("Riposte", 1, 0, "dmg", 5, 1, mold=("atk", 1)),
        Ability("Feint", 1, 1, "dmg", 4, 1, mold=("dfn", 1)),
        Ability("Lunge", 2, 1, "dmg", 6, 2, mold=("atk", 1)),
        Ability("Execute", 3, 3, "dmg", 9, 1, mold=("atk", 2)),
    ],
    "Warden": [
        Ability("Bulwark", 2, 2, "shield", 8, 2, mold=("dfn", 2)),
        Ability("Taunt", 3, 2, "dmg", 3, 2, area=1, mold=("dfn", 1)),
        Ability("Guard", 1, 1, "shield", 4, 1, mold=("dfn", 1)),
        Ability("Slam", 4, 3, "dmg", 8, 1, mold=("atk", 1)),
    ],
    "Venom": [
        Ability("Toxin", 2, 1, "poison", 3, 2, mold=("atk", 1)),
        Ability("Cloud", 4, 3, "poison", 2, 2, area=1, mold=("atk", 1)),
        Ability("Nick", 1, 0, "dmg", 3, 2, mold=("atk", 0)),
        Ability("Wither", 3, 2, "poison", 4, 1, mold=("dfn", 1)),
    ],
    "Skirmisher": [
        Ability("Dart", 2, 0, "dmg", 4, 3, mold=("atk", 1)),
        Ability("Flank", 1, 1, "move", 3, 0, mold=("dfn", 1)),
        Ability("Volley", 3, 2, "dmg", 6, 3, mold=("atk", 1)),
        Ability("Snipe", 4, 3, "dmg", 9, 4, mold=("atk", 2)),
    ],
    "Oracle": [
        Ability("Mend", 2, 1, "heal", 6, 2, mold=("dfn", 1)),
        Ability("Blessing", 1, 2, "buff", 2, 2, mold=("dfn", 1)),
        Ability("Rebuke", 3, 2, "dmg", 5, 2, mold=("atk", 1)),
        Ability("Sanctuary", 4, 3, "shield", 10, 2, area=1, mold=("dfn", 2)),
    ],
    "Breaker": [
        Ability("Detonate", 4, 4, "dmg", 12, 2, area=1, mold=("atk", 2)),
        Ability("Crack", 3, 2, "dmg", 7, 1, mold=("atk", 1)),
        Ability("Shove", 2, 1, "dmg", 4, 1, mold=("atk", 0)),
        Ability("Tap", 1, 0, "dmg", 2, 1, mold=("dfn", 1)),
    ],
    "Sentinel": [
        Ability("Barrage", 3, 2, "dmg", 5, 3, area=1, mold=("atk", 1)),
        Ability("Pin", 1, 1, "dmg", 3, 2, mold=("dfn", 1)),
        Ability("Ward", 2, 2, "shield", 5, 2, mold=("dfn", 1)),
        Ability("Fusillade", 4, 3, "dmg", 8, 3, mold=("atk", 1)),
    ],
}

ROSTER = list(TEMPLATES.keys())


# ---------------------------------------------------------------- champion

class Champ:
    __slots__ = ("name", "team", "hp", "max_hp", "pos", "atk", "dfn",
                 "shield", "cds", "poison", "poison_left", "alive",
                 "respawn_in", "dying", "uses", "mold_atk", "mold_dfn")

    def __init__(self, name, team, pos):
        self.name = name
        self.team = team
        self.max_hp = 30
        self.hp = 30
        self.pos = pos
        self.atk = 0
        self.dfn = 0
        self.shield = 0
        self.cds = [0, 0, 0, 0, 0]  # index 4 == Reposition
        self.poison = 0
        self.poison_left = 0
        self.alive = True
        self.respawn_in = 0
        self.dying = False   # at <=0 hp via status phase; one more round to act
        self.uses = 0
        self.mold_atk = 0
        self.mold_dfn = 0

    def abilities(self):
        return TEMPLATES[self.name] + [REPOSITION]

    def clone(self):
        c = Champ.__new__(Champ)
        for s in Champ.__slots__:
            setattr(c, s, getattr(self, s))
        c.cds = list(self.cds)
        return c


class State:
    __slots__ = ("champs", "points", "rnd", "opener", "log")

    def __init__(self, draft0, draft1):
        self.champs = []
        for i, n in enumerate(draft0):
            self.champs.append(Champ(n, 0, (HOME[0][0], HOME[0][1] - 2 + i)))
        for i, n in enumerate(draft1):
            self.champs.append(Champ(n, 1, (HOME[1][0], HOME[1][1] - 2 + i)))
        self.points = [0, 0]
        self.rnd = 0
        self.opener = 0
        self.log = []

    def clone(self):
        s = State.__new__(State)
        s.champs = [c.clone() for c in self.champs]
        s.points = list(self.points)
        s.rnd = self.rnd
        s.opener = self.opener
        s.log = []
        return s

    def living(self, team=None):
        return [c for c in self.champs
                if c.alive and (team is None or c.team == team)]

    def winner(self):
        # NOTE (2026-08-14): this used to check team 0 first and return 0, so
        # any round where BOTH teams crossed the threshold was awarded to team
        # 0. Points are scored at round close, so simultaneous crossings are
        # common — this alone produced a ~65% team-0 win rate on boards where
        # points, damage and action counts were otherwise identical.
        a = self.points[0] >= POINTS_TO_WIN
        b = self.points[1] >= POINTS_TO_WIN
        if a and b:
            return 2          # simultaneous — a draw, not a team-0 win
        if a:
            return 0
        if b:
            return 1
        return None


# ---------------------------------------------------------------- actions

class Action:
    __slots__ = ("champ", "idx", "ability", "target")

    def __init__(self, champ, idx, ability, target):
        self.champ = champ
        self.idx = idx
        self.ability = ability
        self.target = target

    @property
    def init(self):
        return self.ability.init


def legal_actions(state, team, max_init, acted):
    """Every legal ability at initiative <= max_init, from any living champion
    of `team`. `acted` is the set of champion ids already used this round under
    the per-champion action economy (empty set disables that rule)."""
    out = []
    for ci, c in enumerate(state.champs):
        if c.team != team or not c.alive:
            continue
        if ci in acted:
            continue
        for ai, ab in enumerate(c.abilities()):
            if ab.init > max_init or c.cds[ai] > 0:
                continue
            for t in targets_for(state, c, ab):
                out.append(Action(ci, ai, ab, t))
    return out


def targets_for(state, c, ab):
    if ab.kind == "move":
        out = []
        for dq, dr in NEIGHBORS:
            for step in (1, 2):
                p = (c.pos[0] + dq * step, c.pos[1] + dr * step)
                if in_board(p) and not any(
                        o.alive and o.pos == p for o in state.champs):
                    out.append(p)
        # NOTE (2026-08-14): this used to `return out[:6]`, to cap fan-out.
        # NEIGHBORS begins [(1,0), (1,-1), (0,-1), ...], so the first six
        # entries are all in the +q / -r directions. Team 0 spawns at q=-3 and
        # needs +q to reach the objectives; team 1 spawns at q=+3 and needs -q,
        # which was never generated. Team 1 could not walk toward the centre.
        # That single truncation produced the entire 72-90% "first-mover"
        # asymmetry. Do not cap an action set along an ordered axis.
        return out
    friendly = ab.kind in ("shield", "heal", "buff")
    out = []
    for ti, t in enumerate(state.champs):
        if not t.alive:
            continue
        if friendly != (t.team == c.team):
            continue
        if hex_dist(c.pos, t.pos) <= max(ab.rng, 1):
            out.append(ti)
    return out


def apply_action(state, act):
    c = state.champs[act.champ]
    ab = act.ability
    c.cds[act.idx] = ab.cd
    c.uses += 1
    if ab.mold:
        stat, d = ab.mold
        if stat == "atk":
            c.atk += d
            c.mold_atk += d
        else:
            c.dfn += d
            c.mold_dfn += d

    if ab.kind == "move":
        c.pos = act.target
        return

    victims = [act.target]
    if ab.area:
        centre = state.champs[act.target].pos
        for ti, t in enumerate(state.champs):
            if t.alive and ti != act.target and hex_dist(t.pos, centre) <= ab.area:
                if (t.team == c.team) == (ab.kind in ("shield", "heal", "buff")):
                    victims.append(ti)

    for ti in victims:
        t = state.champs[ti]
        if ab.kind == "dmg":
            dmg = max(1, ab.power + c.atk - t.dfn)
            absorbed = min(t.shield, dmg)
            t.shield -= absorbed
            t.hp -= (dmg - absorbed)
        elif ab.kind == "poison":
            t.poison += ab.power
            t.poison_left = max(t.poison_left, 2)
        elif ab.kind == "shield":
            t.shield += ab.power
        elif ab.kind == "heal":
            t.hp = min(t.max_hp, t.hp + ab.power)
        elif ab.kind == "buff":
            t.atk += ab.power


# ---------------------------------------------------------------- round

class Metrics:
    def __init__(self):
        self.ladder_lengths = []
        self.pass_with_options = 0
        self.pass_total = 0
        self.decisions = 0
        self.branching = []
        self.nodes = 0
        self.dying_rounds = 0
        self.deaths_direct = 0
        self.deaths_status = 0
        self.rounds = []
        self.opener_ability_share = []


def play_round(state, agents, econ, pass_rule, m):
    """One round: ladder exchange, then death check, then status phase."""
    chain = 0
    team = state.opener
    max_init = MAX_INIT
    acted = set()
    consecutive_passes = 0
    opener_count = 0

    while True:
        legal = legal_actions(state, team, max_init,
                              acted if econ == "per_champion" else set())
        if econ.startswith("per_team"):
            cap = int(econ.split("_")[-1])   # e.g. per_team_5
            if chain >= cap * 2:
                legal = []

        m.decisions += 1
        m.branching.append(len(legal) + 1)

        act = agents[team].choose(state, legal, team, max_init, econ,
                                  pass_rule, m) if legal else None

        if act is None:
            m.pass_total += 1
            if legal:
                m.pass_with_options += 1
            consecutive_passes += 1
            if pass_rule == "single" or consecutive_passes >= 2:
                break
            team = 1 - team
            continue

        consecutive_passes = 0
        apply_action(state, act)
        acted.add(act.champ)
        chain += 1
        if team == state.opener:
            opener_count += 1
        max_init = act.init
        team = 1 - team
        if chain > 40:  # runaway guard
            break

    m.ladder_lengths.append(chain)
    if chain:
        m.opener_ability_share.append(opener_count / chain)

    # ---- death check (ladder damage kills here; no dying round) ----
    for c in state.champs:
        if c.alive and c.hp <= 0:
            c.alive = False
            c.dying = False
            m.deaths_direct += 1
            state.points[1 - c.team] += KILL_POINTS
            c.respawn_in = 1 + state.rnd // 8

    # ---- status phase (poison here grants a dying round) ----
    for c in state.champs:
        if not c.alive:
            continue
        if c.dying:
            # was already at <=0 from a previous status phase: now it dies
            c.alive = False
            c.dying = False
            m.deaths_status += 1
            state.points[1 - c.team] += KILL_POINTS
            c.respawn_in = 1 + state.rnd // 8
            continue
        if c.poison_left > 0:
            c.hp -= c.poison
            c.poison_left -= 1
            if c.poison_left == 0:
                c.poison = 0
            if c.hp <= 0:
                c.dying = True          # survives one more round, debuffed
                c.atk -= 2              # "nearly dead" penalty
                m.dying_rounds += 1

    # ---- scoring & cooldowns & respawns ----
    for obj in OBJECTIVES:
        holders = [c.team for c in state.champs if c.alive and c.pos == obj]
        if holders and len(set(holders)) == 1:
            state.points[holders[0]] += 1

    for c in state.champs:
        c.cds = [max(0, x - 1) for x in c.cds]
        c.shield = 0
        if not c.alive:
            c.respawn_in -= 1
            if c.respawn_in <= 0:
                c.alive = True
                c.hp = c.max_hp
                c.poison = c.poison_left = 0
                c.dying = False
                c.pos = HOME[c.team]

    state.rnd += 1
    state.opener = 1 - state.opener


def play_match(draft0, draft1, agents, econ="per_champion",
               pass_rule="single", m=None):
    m = m or Metrics()
    s = State(draft0, draft1)
    while s.winner() is None and s.rnd < ROUND_CAP:
        play_round(s, agents, econ, pass_rule, m)
    m.rounds.append(s.rnd)
    return s, m
