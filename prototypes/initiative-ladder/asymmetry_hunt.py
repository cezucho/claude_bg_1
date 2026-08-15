# PROTOTYPE - NOT FOR PRODUCTION
# Question: why does team 0 win ~72-77% of MIRROR matches when damage dealt is
#           symmetric and alternating the match opener changes nothing?
# Date: 2026-08-14

import random
import statistics as st

from sim import State, ROSTER, OBJECTIVES, HOME, hex_dist, in_board
from agents import GreedyAgent, RandomAgent
import ladder_v2
from ladder_v2 import play_round, M2

MIRROR = lambda p: (-p[0], -p[1])


def build(draft0, draft1, spawn="current"):
    s = State(list(draft0), list(draft1))
    if spawn == "mirror":
        # team 0 at (-3, -1+i); team 1 at the true hex mirror (3, 1-i)
        for i in range(5):
            s.champs[i].pos = (-3, -1 + i)
            s.champs[5 + i].pos = MIRROR((-3, -1 + i))
    elif spawn == "mirror_swapped":
        # identical to "mirror" but the two teams trade sides of the board.
        # If the win bias follows the SIDE, it is positional.
        # If it stays with team 0, it is enumeration / tie-break order.
        for i in range(5):
            s.champs[i].pos = MIRROR((-3, -1 + i))
            s.champs[5 + i].pos = (-3, -1 + i)
    elif spawn == "current":
        pass
    return s


def trial(tag, spawn="current", objectives=True, matches=200, seed=41,
          mk=lambda i: GreedyAgent(), cap=25):
    saved = list(OBJECTIVES)
    if not objectives:
        OBJECTIVES.clear()          # kills only
    rng = random.Random(seed)
    wins = [0, 0, 0]
    pts = [0, 0]
    dmg = [0, 0]
    acts = [0, 0]
    for i in range(matches):
        pool = rng.sample(ROSTER, 5)
        s = build(pool, pool, spawn)
        s.opener = i % 2
        agents = {0: mk(i * 2), 1: mk(i * 2 + 1)}
        m = M2()
        while s.winner() is None and s.rnd < cap:
            play_round(s, agents, "half", m)
        w = s.winner()
        wins[w if w is not None else 2] += 1
        for t in (0, 1):
            pts[t] += s.points[t]
        for c in s.champs:
            dmg[1 - c.team] += (c.max_hp - c.hp)
            acts[c.team] += c.uses
    OBJECTIVES.clear()
    OBJECTIVES.extend(saved)
    d = wins[0] + wins[1]
    print(f"  {tag:<34} t0/t1/draw {wins[0]:>3}/{wins[1]:>3}/{wins[2]:>3}"
          f"   t0 {100*wins[0]/max(1,d):>5.1f}%"
          f"   pts {pts[0]:>5}/{pts[1]:<5}"
          f" dmg {dmg[0]:>6}/{dmg[1]:<6} acts {acts[0]:>5}/{acts[1]}")
    return wins


if __name__ == "__main__":
    print("\n=== SPAWN GEOMETRY ===")
    for tag, sp in (("A. current spawns (baseline)", "current"),
                    ("B. TRUE MIRROR spawns", "mirror")):
        st0 = build(ROSTER[:5], ROSTER[:5], sp)
        p0 = [c.pos for c in st0.champs[:5]]
        p1 = [c.pos for c in st0.champs[5:]]
        ok = all(MIRROR(a) == b for a, b in zip(p0, p1))
        d0 = [hex_dist(p, (0, 0)) for p in p0]
        d1 = [hex_dist(p, (0, 0)) for p in p1]
        off = sum(1 for p in p0 + p1 if not in_board(p))
        print(f"  {tag:<34} index-wise mirror: {ok}   d(centre) {d0} / {d1}"
              f"   off-board {off}")

    print("\n=== WIN SPLIT (mirror drafts, alternating match opener) ===")
    trial("A. current spawns", "current")
    trial("B. true-mirror spawns", "mirror")
    trial("C. true-mirror, NO objectives", "mirror", objectives=False)
    trial("D. current spawns, NO objectives", "current", objectives=False)
    trial("E. true-mirror, random agents", "mirror",
          mk=lambda i: RandomAgent(pass_prob=0.2, seed=i))
