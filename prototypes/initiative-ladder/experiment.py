# PROTOTYPE - NOT FOR PRODUCTION
# Question: measure ladder length, pass behaviour, branching factor, match
#           length, molding divergence, dying-round frequency, snowball.
# Date: 2026-08-14

import random
import statistics as st
import sys

from sim import play_match, ROSTER, Metrics, State, play_round
from agents import RandomAgent, GreedyAgent, SearchAgent


def draft_pair(rng):
    pool = rng.sample(ROSTER, 8)
    return pool[:5], pool[3:8]      # deliberate overlap: mirror-ish matchups


def summarise(tag, m, wins):
    ll = m.ladder_lengths
    br = m.branching
    n = len(ll) or 1
    print(f"\n=== {tag} ===")
    print(f"  matches                : {len(m.rounds)}")
    print(f"  rounds/match  mean/med : {st.mean(m.rounds):.1f} / "
          f"{st.median(m.rounds):.0f}   max {max(m.rounds)}")
    print(f"  ladder length mean/med : {st.mean(ll):.2f} / {st.median(ll):.0f}"
          f"   max {max(ll)}")
    dist = {}
    for x in ll:
        dist[x] = dist.get(x, 0) + 1
    top = sorted(dist.items())[:9]
    print(f"  ladder length dist     : "
          + "  ".join(f"{k}:{100*v/n:.0f}%" for k, v in top))
    print(f"  decisions              : {m.decisions}")
    print(f"  passes (total)         : {m.pass_total}")
    print(f"  passes WITH options    : {m.pass_with_options} "
          f"({100*m.pass_with_options/max(1,m.decisions):.1f}% of decisions)")
    print(f"  branching mean/max     : {st.mean(br):.1f} / {max(br)}")
    print(f"  nodes evaluated        : {m.nodes:,}")
    print(f"  deaths direct/status   : {m.deaths_direct} / {m.deaths_status}")
    print(f"  dying rounds granted   : {m.dying_rounds}")
    if m.opener_ability_share:
        print(f"  opener share of chain  : "
              f"{100*st.mean(m.opener_ability_share):.0f}%")
    print(f"  win split (t0/t1/draw) : {wins[0]}/{wins[1]}/{wins[2]}")


def run(tag, agent_factory, econ, pass_rule, matches, seed=7):
    rng = random.Random(seed)
    m = Metrics()
    wins = [0, 0, 0]
    molds = []
    leads = []
    for i in range(matches):
        d0, d1 = draft_pair(rng)
        agents = {0: agent_factory(i * 2), 1: agent_factory(i * 2 + 1)}
        s, m = play_match(d0, d1, agents, econ, pass_rule, m)
        w = s.winner()
        wins[w if w is not None else 2] += 1
        molds.append(tuple(sorted((c.mold_atk, c.mold_dfn)
                                  for c in s.champs if c.team == 0)))
        leads.append((s.points[0] - s.points[1], w))
    summarise(tag, m, wins)
    uniq = len(set(molds))
    print(f"  distinct mold profiles : {uniq}/{len(molds)}")
    return m


def snowball_probe(matches=80, seed=11, checkpoints=(3, 5, 8)):
    """Does an early lead predict the win? Checked at several points, because
    'leading near the end wins' is trivially true and proves nothing."""
    rng = random.Random(seed)
    agree = {c: 0 for c in checkpoints}
    total = {c: 0 for c in checkpoints}
    for i in range(matches):
        d0, d1 = draft_pair(rng)
        agents = {0: GreedyAgent(), 1: GreedyAgent()}
        m = Metrics()
        s = State(d0, d1)
        leads = {}
        while s.winner() is None and s.rnd < 60:
            play_round(s, agents, "per_champion", "single", m)
            if s.rnd in checkpoints:
                leads[s.rnd] = s.points[0] - s.points[1]
        w = s.winner()
        if w is None:
            continue
        for c in checkpoints:
            if leads.get(c, 0) == 0:
                continue
            total[c] += 1
            if (leads[c] > 0) == (w == 0):
                agree[c] += 1
    print(f"\n=== SNOWBALL PROBE ({matches} matches) ===")
    for c in checkpoints:
        t = max(1, total[c])
        print(f"  lead at round {c:>2} predicted win : {agree[c]:>3}/{total[c]:<3} "
              f"({100*agree[c]/t:.0f}%)")


if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "all"

    if which in ("all", "econ"):
        print("\n########## ACTION ECONOMY VARIANTS (greedy agents) ##########")
        run("per_champion / pass=single", lambda s: GreedyAgent(),
            "per_champion", "single", 120)
        run("per_champion / pass=both", lambda s: GreedyAgent(),
            "per_champion", "both", 120)
        run("uncapped / pass=single", lambda s: GreedyAgent(),
            "uncapped", "single", 120)
        run("uncapped / pass=both", lambda s: GreedyAgent(),
            "uncapped", "both", 120)
        run("per_team_5 / pass=both", lambda s: GreedyAgent(),
            "per_team_5", "both", 120)

    if which in ("all", "random"):
        print("\n########## RANDOM BASELINE ##########")
        run("random / per_champion / single", lambda s: RandomAgent(seed=s),
            "per_champion", "single", 120)

    if which in ("all", "search"):
        print("\n########## SEARCH AGENT (does it ever pass?) ##########")
        run("search d4 / per_champion / single",
            lambda s: SearchAgent(depth=4, width=6),
            "per_champion", "single", 12)
        run("search d4 / per_champion / both",
            lambda s: SearchAgent(depth=4, width=6),
            "per_champion", "both", 12)

    if which in ("all", "snowball"):
        snowball_probe()
