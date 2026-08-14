# PROTOTYPE - NOT FOR PRODUCTION
# Question: When an agent can see the opponent's reply, does it ever choose to
#           PASS while holding legal answers? And what does the search cost?
# Date: 2026-08-14
#
# Samples real mid-ladder positions from greedy self-play, then interrogates a
# search agent at each one. Far cheaper than full search-vs-search matches, and
# it targets the actual question.

import random
import statistics as st

from sim import State, play_round, Metrics, ROSTER, legal_actions
from agents import GreedyAgent, SearchAgent, evaluate

SAMPLE_RATE = 0.30


class ProbeAgent:
    """Plays greedy (cheap, keeps matches moving) but at sampled decision
    points also asks a SearchAgent what it would do, and records the answer."""

    def __init__(self, depth, width, rec, rng):
        self.greedy = GreedyAgent()
        self.search = SearchAgent(depth=depth, width=width)
        self.rec = rec
        self.rng = rng

    def choose(self, state, legal, team, max_init, econ, pass_rule, m):
        pick = self.greedy.choose(state, legal, team, max_init, econ,
                                  pass_rule, m)
        if legal and self.rng.random() < SAMPLE_RATE:
            before = m.nodes
            s_pick = self.search.choose(state.clone(), legal, team, max_init,
                                        econ, pass_rule, m)
            self.rec["nodes"].append(m.nodes - before)
            self.rec["branch"].append(len(legal) + 1)
            self.rec["n"] += 1
            if s_pick is None:
                self.rec["search_pass"] += 1
                if pick is not None:
                    self.rec["strategic_pass"] += 1
            elif pick is not None and (s_pick.champ, s_pick.idx) != (
                    pick.champ, pick.idx):
                self.rec["disagree"] += 1
            self.rec["init_chosen"].append(
                s_pick.init if s_pick else 0)
        return pick


def probe(depth, width, matches, econ, pass_rule, seed=3):
    rng = random.Random(seed)
    rec = {"nodes": [], "branch": [], "n": 0, "search_pass": 0,
           "strategic_pass": 0, "disagree": 0, "init_chosen": []}
    m = Metrics()
    for i in range(matches):
        pool = rng.sample(ROSTER, 8)
        s = State(pool[:5], pool[3:8])
        agents = {0: ProbeAgent(depth, width, rec, rng),
                  1: ProbeAgent(depth, width, rec, rng)}
        while s.winner() is None and s.rnd < 25:
            play_round(s, agents, econ, pass_rule, m)

    n = max(1, rec["n"])
    print(f"\n=== SEARCH PROBE  depth={depth} width={width} "
          f"econ={econ} pass={pass_rule} ===")
    print(f"  positions interrogated   : {rec['n']}")
    print(f"  search chose to PASS     : {rec['search_pass']} "
          f"({100*rec['search_pass']/n:.1f}%)")
    print(f"  ...of which STRATEGIC    : {rec['strategic_pass']} "
          f"({100*rec['strategic_pass']/n:.1f}% of positions)")
    print(f"     (greedy wanted to act, search declined)")
    print(f"  search != greedy choice  : {rec['disagree']} "
          f"({100*rec['disagree']/n:.1f}%)")
    print(f"  branching mean           : {st.mean(rec['branch']):.1f}")
    print(f"  nodes/decision mean/med  : {st.mean(rec['nodes']):,.0f} / "
          f"{st.median(rec['nodes']):,.0f}")
    print(f"  nodes/decision max       : {max(rec['nodes']):,}")
    if rec["init_chosen"]:
        picked = [x for x in rec["init_chosen"] if x]
        if picked:
            print(f"  initiative chosen mean   : {st.mean(picked):.2f}")
    return rec


if __name__ == "__main__":
    for d in (2, 3):
        probe(d, 6, 6, "per_champion", "single")
    probe(3, 6, 6, "per_champion", "both")
