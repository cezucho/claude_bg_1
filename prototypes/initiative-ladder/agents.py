# PROTOTYPE - NOT FOR PRODUCTION
# Question: Is passing ever correct for a competent agent? What does searching
#           the ladder cost?
# Date: 2026-08-14

import random
from sim import (legal_actions, apply_action, MAX_INIT, OBJECTIVES, hex_dist)


def evaluate(state, team):
    """Positive is good for `team`. Deliberately crude."""
    me = other = 0.0
    for c in state.champs:
        v = 0.0
        if c.alive:
            v += c.hp + c.shield * 0.5 + c.atk * 1.5 + c.dfn * 1.5
            v -= c.poison * 2.0
            if c.dying:
                v -= 12
            # objective proximity
            v += max(0.0, 6.0 - min(hex_dist(c.pos, o) for o in OBJECTIVES)) * 1.2
        else:
            v -= 15
        if c.team == team:
            me += v
        else:
            other += v
    pts = (state.points[team] - state.points[1 - team]) * 8.0
    return me - other + pts


class RandomAgent:
    def __init__(self, pass_prob=0.25, seed=0):
        self.rng = random.Random(seed)
        self.pass_prob = pass_prob

    def choose(self, state, legal, team, max_init, econ, pass_rule, m):
        if not legal or self.rng.random() < self.pass_prob:
            return None
        return self.rng.choice(legal)


class GreedyAgent:
    """Takes the single action with the best immediate evaluation delta.
    Passes only if nothing improves the position."""

    def choose(self, state, legal, team, max_init, econ, pass_rule, m):
        if not legal:
            return None
        base = evaluate(state, team)
        best, best_v = None, base
        for a in legal:
            s2 = state.clone()
            apply_action(s2, a)
            m.nodes += 1
            v = evaluate(s2, team)
            if v > best_v:
                best, best_v = a, v
        return best


class SearchAgent:
    """Depth-limited negamax over the remainder of the ladder.

    This is the agent that answers the real question: when a policy can see the
    opponent's reply, does it ever choose to pass while holding legal answers?
    """

    def __init__(self, depth=4, width=8):
        self.depth = depth
        self.width = width

    def choose(self, state, legal, team, max_init, econ, pass_rule, m):
        if not legal:
            return None
        cand = self._shortlist(state, legal, team, m)
        # Value of passing depends on the pass rule, and this is the crux of
        # the whole experiment:
        #   "single" -> my pass ENDS the round; the position freezes here.
        #   "both"   -> my pass yields priority; the opponent may keep going.
        if pass_rule == "single":
            pass_value = evaluate(state, team)
        else:
            pass_value = -self._negamax(state, 1 - team, max_init,
                                        self.depth - 1, econ, pass_rule, m, 1)
        best, best_v = None, pass_value
        for a in cand:
            s2 = state.clone()
            apply_action(s2, a)
            m.nodes += 1
            v = -self._negamax(s2, 1 - team, a.init, self.depth - 1,
                               econ, pass_rule, m, passes=0)
            if v > best_v:
                best, best_v = a, v
        return best

    def _shortlist(self, state, legal, team, m):
        scored = []
        for a in legal:
            s2 = state.clone()
            apply_action(s2, a)
            m.nodes += 1
            scored.append((evaluate(s2, team), a))
        scored.sort(key=lambda t: -t[0])
        return [a for _, a in scored[:self.width]]

    def _negamax(self, state, team, max_init, depth, econ, pass_rule, m,
                 passes):
        if depth <= 0:
            return evaluate(state, team)
        legal = legal_actions(state, team, max_init, set())
        if not legal:
            return evaluate(state, team)
        best = evaluate(state, team)          # value of passing
        for a in self._shortlist(state, legal, team, m)[:self.width]:
            s2 = state.clone()
            apply_action(s2, a)
            m.nodes += 1
            v = -self._negamax(s2, 1 - team, a.init, depth - 1, econ,
                               pass_rule, m, 0)
            if v > best:
                best = v
        return best
