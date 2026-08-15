# PROTOTYPE - NOT FOR PRODUCTION
# Question: one action per champion per ROUND, or per HALF?
#           And how often does a half end by exhaustion, cutting a combo short?
# Date: 2026-08-14
#
# Implements the GDD rules that the original sim predates:
#   - two halves per round, each team opening one
#   - a pass grants the opponent one unanswerable Last Word
#   - action economy switchable: 'round' (GDD as written) vs 'half' (user's instinct)

import random
import statistics as st

from sim import (State, Champ, ROSTER, MAX_INIT, OBJECTIVES, HOME,
                 legal_actions, apply_action, KILL_POINTS, POINTS_TO_WIN)
from agents import GreedyAgent, SearchAgent, evaluate

ROUND_CAP = 40


class M2:
    def __init__(self):
        self.half_len = []          # resolutions per half
        self.round_len = []         # resolutions per round
        self.rounds = []
        self.end_pass = 0           # halves ended by a pass
        self.end_exhaust = 0        # halves ended because the mover had nothing
        self.end_exhaust_asym = 0   # ...and the OPPONENT still had actions
        self.opener_actions = []    # actions the half-opener got
        self.opener_stuck_1 = 0     # opener got exactly one action
        self.halves = 0
        self.last_words = 0
        self.last_word_declined = 0
        self.passes_with_options = 0
        self.decisions = 0
        self.spent_at_half2 = []    # champions already spent entering half 2
        self.no_answer_h2 = 0       # a team entered half 2 with zero available champions
        self.nodes = 0              # agents.py increments this


def play_half(state, opener, agents, econ, acted_round, m):
    """One ladder exchange. Returns resolutions in this half."""
    acted = acted_round if econ == "round" else set()
    ceiling = MAX_INIT
    team = opener
    chain = 0
    by_opener = 0
    m.halves += 1

    while True:
        legal = legal_actions(state, team, ceiling, acted)
        m.decisions += 1

        if not legal:
            # exhaustion: no Last Word (GDD Edge Cases)
            m.end_exhaust += 1
            opp_legal = legal_actions(state, 1 - team, ceiling, acted)
            if opp_legal:
                m.end_exhaust_asym += 1
            break

        act = agents[team].choose(state, legal, team, ceiling, econ, "single", m)

        if act is None:                      # PASS -> Last Word
            m.passes_with_options += 1
            m.end_pass += 1
            opp = 1 - team
            lw_legal = legal_actions(state, opp, ceiling, acted)
            if lw_legal:
                lw = agents[opp].choose(state, lw_legal, opp, ceiling, econ,
                                        "single", m)
                if lw is not None:
                    apply_action(state, lw)
                    acted.add(lw.champ)
                    acted_round.add(lw.champ)
                    chain += 1
                    if opp == opener:
                        by_opener += 1
                    m.last_words += 1
                else:
                    m.last_word_declined += 1
            break

        apply_action(state, act)
        acted.add(act.champ)
        acted_round.add(act.champ)
        chain += 1
        if team == opener:
            by_opener += 1
        ceiling = act.init
        team = 1 - team
        if chain > 60:
            break

    m.half_len.append(chain)
    m.opener_actions.append(by_opener)
    if by_opener == 1:
        m.opener_stuck_1 += 1
    return chain


def play_round(state, agents, econ, m):
    acted_round = set()
    total = 0
    total += play_half(state, state.opener, agents, econ, acted_round, m)

    # how many of the second-half opener's champions are already spent?
    h2_opener = 1 - state.opener
    if econ == "round":
        spent = sum(1 for ci, c in enumerate(state.champs)
                    if c.team == h2_opener and ci in acted_round and c.alive)
        avail = sum(1 for ci, c in enumerate(state.champs)
                    if c.team == h2_opener and ci not in acted_round and c.alive)
        m.spent_at_half2.append(spent)
        if avail == 0:
            m.no_answer_h2 += 1

    total += play_half(state, h2_opener, agents, econ, acted_round, m)
    m.round_len.append(total)

    # ---- round closure: death check, then status phase ----
    for c in state.champs:
        if c.alive and c.hp <= 0:
            c.alive = False
            state.points[1 - c.team] += KILL_POINTS
            c.respawn_in = 1 + state.rnd // 8
    for c in state.champs:
        if not c.alive:
            continue
        if c.dying:
            c.alive = False
            c.dying = False
            state.points[1 - c.team] += KILL_POINTS
            c.respawn_in = 1 + state.rnd // 8
            continue
        if c.poison_left > 0:
            c.hp -= c.poison
            c.poison_left -= 1
            if c.poison_left == 0:
                c.poison = 0
            if c.hp <= 0:
                c.dying = True
                c.atk -= 2

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


def run(tag, econ, matches=150, seed=17, agent=lambda: GreedyAgent(),
        random_first_opener=False):
    rng = random.Random(seed)
    m = M2()
    wins = [0, 0, 0]
    for i in range(matches):
        pool = rng.sample(ROSTER, 5)          # MIRROR drafts
        s = State(list(pool), list(pool))
        if random_first_opener:
            s.opener = i % 2                  # alternate who opens the match
        agents = {0: agent(), 1: agent()}
        while s.winner() is None and s.rnd < ROUND_CAP:
            play_round(s, agents, econ, m)
        m.rounds.append(s.rnd)
        w = s.winner()
        wins[w if w is not None else 2] += 1

    hl, rl = m.half_len, m.round_len
    ends = m.end_pass + m.end_exhaust
    print(f"\n=== {tag} (econ = one action per champion per {econ.upper()}) ===")
    print(f"  matches / rounds per match : {matches} / {st.mean(m.rounds):.1f}")
    print(f"  resolutions per HALF       : mean {st.mean(hl):.2f}  median {st.median(hl):.0f}  max {max(hl)}")
    print(f"  resolutions per ROUND      : mean {st.mean(rl):.2f}  median {st.median(rl):.0f}  max {max(rl)}")
    print(f"  halves played              : {m.halves}")
    print(f"  half ended by PASS         : {m.end_pass} ({100*m.end_pass/max(1,ends):.0f}%)")
    print(f"  half ended by EXHAUSTION   : {m.end_exhaust} ({100*m.end_exhaust/max(1,ends):.0f}%)")
    print(f"    ...opponent still had actions (asymmetric) : {m.end_exhaust_asym}"
          f"  ({100*m.end_exhaust_asym/max(1,m.halves):.1f}% of halves)")
    print(f"  Last Words taken/declined  : {m.last_words} / {m.last_word_declined}")
    print(f"  opener actions per half    : mean {st.mean(m.opener_actions):.2f}  max {max(m.opener_actions)}")
    print(f"  >> opener got only ONE act : {m.opener_stuck_1} "
          f"({100*m.opener_stuck_1/max(1,m.halves):.1f}% of halves)  <-- combo cut short")
    if m.spent_at_half2:
        print(f"  champs spent entering h2   : mean {st.mean(m.spent_at_half2):.2f}/5")
        print(f"  >> team entered h2 with ZERO available : {m.no_answer_h2} "
              f"({100*m.no_answer_h2/max(1,len(m.spent_at_half2)):.1f}% of rounds)")
    print(f"  passes with options        : {m.passes_with_options} "
          f"({100*m.passes_with_options/max(1,m.decisions):.1f}% of decisions)")
    print(f"  mirror win split t0/t1/dr  : {wins[0]}/{wins[1]}/{wins[2]}"
          f"   first-mover {100*wins[0]/max(1,wins[0]+wins[1]):.1f}%")
    # F5 round-duration check: 5 decisions x 6s + resolutions x 1.5s
    rsec = 5 * 6 + st.mean(rl) * 1.5
    print(f"  F5 round seconds (est)     : {rsec:.0f}s  ->  "
          f"{900/rsec:.0f} rounds in a 15-min match")
    return m


if __name__ == "__main__":
    run("PER ROUND (GDD as written)", "round")
    run("PER HALF (user's instinct)", "half")
    # Is the first-mover advantage structural, or an artefact of team 0 always
    # striking first in a snowballing scoring system?
    run("PER HALF + match opener alternated", "half", random_first_opener=True)
    run("PER ROUND + match opener alternated", "round", random_first_opener=True)


def diagnose(matches=200, seed=5):
    """Is the mirror-match bias caused by the agents, or by the board/rules?"""
    from agents import RandomAgent
    for label, mk in (("random agents", lambda i: RandomAgent(pass_prob=0.2, seed=i)),
                      ("greedy agents", lambda i: GreedyAgent())):
        rng = random.Random(seed)
        wins = [0, 0, 0]
        dmg = [0, 0]
        for i in range(matches):
            pool = rng.sample(ROSTER, 5)
            s = State(list(pool), list(pool))
            s.opener = i % 2
            agents = {0: mk(i * 2), 1: mk(i * 2 + 1)}
            m = M2()
            while s.winner() is None and s.rnd < 25:
                play_round(s, agents, "half", m)
            w = s.winner()
            wins[w if w is not None else 2] += 1
            for c in s.champs:
                dmg[1 - c.team] += (c.max_hp - c.hp)
        d = wins[0] + wins[1]
        print(f"  {label:15s} t0/t1/draw {wins[0]}/{wins[1]}/{wins[2]}"
              f"   t0 share {100*wins[0]/max(1,d):.1f}%"
              f"   damage dealt t0/t1 {dmg[0]}/{dmg[1]}")
