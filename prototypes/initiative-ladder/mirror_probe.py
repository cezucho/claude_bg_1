# PROTOTYPE - NOT FOR PRODUCTION
# Question: (a) Is there a first-mover advantage once drafts are identical?
#           (b) Pillar 3 test — do IDENTICAL drafts mold into different teams?
# Date: 2026-08-14

import random
import statistics as st

from sim import State, play_round, Metrics, ROSTER
from agents import GreedyAgent, RandomAgent


def run_mirror(matches=150, seed=23, jitter=0.0):
    """Both teams draft the same five champions."""
    rng = random.Random(seed)
    wins = [0, 0, 0]
    profiles = []
    per_draft = {}
    for i in range(matches):
        draft = rng.sample(ROSTER, 5)
        s = State(list(draft), list(draft))
        # jitter>0 gives each side a slightly different policy so that
        # identical drafts do not resolve identically by construction
        agents = {0: (RandomAgent(pass_prob=jitter, seed=i * 2)
                      if jitter else GreedyAgent()),
                  1: (RandomAgent(pass_prob=jitter, seed=i * 2 + 1)
                      if jitter else GreedyAgent())}
        m = Metrics()
        while s.winner() is None and s.rnd < 40:
            play_round(s, agents, "per_champion", "single", m)
        w = s.winner()
        wins[w if w is not None else 2] += 1

        prof = tuple((c.name, c.mold_atk, c.mold_dfn)
                     for c in s.champs if c.team == 0)
        profiles.append(prof)
        key = tuple(sorted(draft))
        per_draft.setdefault(key, []).append(prof)

    print(f"\n=== MIRROR MATCHES (identical drafts, "
          f"{'random' if jitter else 'greedy'} agents) ===")
    print(f"  matches                    : {matches}")
    print(f"  win split t0/t1/draw       : {wins[0]}/{wins[1]}/{wins[2]}")
    tot = wins[0] + wins[1]
    if tot:
        print(f"  first-mover (t0) win rate  : {100*wins[0]/tot:.1f}%")

    # Pillar 3: same draft -> different molded team?
    repeated = {k: v for k, v in per_draft.items() if len(v) > 1}
    div = same = 0
    for k, v in repeated.items():
        for a in range(len(v)):
            for b in range(a + 1, len(v)):
                if v[a] == v[b]:
                    same += 1
                else:
                    div += 1
    print(f"  drafts seen more than once : {len(repeated)}")
    print(f"  same-draft pairs compared  : {div + same}")
    if div + same:
        print(f"  ...molded DIFFERENTLY      : {div} "
              f"({100*div/(div+same):.0f}%)")
    spread = []
    for k, v in repeated.items():
        for idx in range(5):
            atks = [p[idx][1] for p in v]
            if len(set(atks)) > 1:
                spread.append(max(atks) - min(atks))
    if spread:
        print(f"  atk-mold spread on repeats : mean {st.mean(spread):.1f}, "
              f"max {max(spread)}")


if __name__ == "__main__":
    run_mirror(matches=150, jitter=0.0)
    run_mirror(matches=150, jitter=0.25, seed=31)
