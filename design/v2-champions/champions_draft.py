"""Draft data for the v2 summoner spells and sample champions, with an opening checker.

Run from the repository root:  python3 design/v2-champions/champions_draft.py
It validates every opening on the Field 7 board (towers solid) and writes
design/v2-champions/champion-sheet.html, which tools render to champion-sheet.png.
This is a design draft, not game content: nothing here is loaded by the game yet.
"""
import json

BOARD = json.load(open("assets/data/boards/board_field7.json"))
HEXES = {tuple(h) for h in BOARD["hexes"]}
TOWERS = {tuple(h) for h in BOARD["towers"]}
ROLES = ["Top", "Jungle", "Mid", "Bottom", "Support"]
START = {ROLES[i]: tuple(h) for i, h in enumerate(BOARD["startA"])}
DIRS = {"right": (1, 0), "back-right": (1, -1), "back-left": (0, -1),
        "left": (-1, 0), "forward-left": (-1, 1), "forward-right": (0, 1)}

SPELLS = [
    # name, initiative, cooldown (rounds), effect, when you take it, its answer
    ("Flash", 1, 5, "Jump to an empty hex up to 2 away, over champions, walls and towers.",
     "Escape a root-and-collapse, or close the last hex for a kill.", "Being rooted first: root stops Flash."),
    ("Barrier", 1, 4, "Shield 6 until round close.", "Survive one burst you can see coming (preview it).", "Exhaust, Ignite's burn ignores shields."),
    ("Ignite", 2, 5, "An enemy within 3 burns: takes 2 every time it acts, for 2 rounds. Halves its healing.",
     "Against healers and champions that must act to matter.", "Cleanse; or simply act less."),
    ("Heal", 1, 5, "Heal yourself and the most wounded ally within 2 for 4.", "Teams without a support; races at round close.", "Ignite halves it."),
    ("Exhaust", 2, 5, "An enemy within 2 deals half damage until the end of the next half.", "Shut down the enemy's biggest hitter for a full ladder.", "Cleanse."),
    ("Teleport", 4, 6, "Move to any empty hex next to a friendly tower or beacon — from anywhere, even spawn.",
     "Defend a tower under siege, or join a fight the moment you respawn.", "Initiative 4: only at the top of the ladder, so it can be seen coming."),
    ("Smite", 1, 5, "Deal 6 to a tower or open nexus within 2. Defenders don't reduce it.", "Finish a tower before round close; steal the last nexus HP.", "Standing on the tower doesn't help; killing the smiter does."),
    ("Cleanse", 1, 4, "Remove root, burn, poison, mark and exhaust from yourself; can't be moved this half.",
     "Into Anchor, Ember and Lens: the status-heavy teams.", "Statuses applied after it."),
]

CHAMPIONS = [
    {
        "id": "anchor", "name": "Anchor", "role": "Top",
        "line": "Pins one enemy in place so the team can collapse on it.",
        "stats": {"hp": 34, "pow": 0.95, "arm": 1, "rch": 2, "spd": 2},
        "passive": ("Immovable", "Can't be pushed or pulled while it has a shield."),
        "abilities": [
            {"key": "Q", "name": "Chain Hook", "init": 2, "cd": 2, "aim": "free", "target": "enemy", "range": 2,
             "verbs": ["pull", "root"], "text": "Pull an enemy 1 hex toward you and ROOT it until the end of the next half (no moves, no dashes, can't be moved). Damage ×0.8.",
             "opening": [("move", "Top", "forward-right"), ("move", "Top", "forward-right"), ("cast", "Top", "Q")],
             "openingAttack": "Walks up the top lane, then hooks the nearest enemy that came forward. The root lasts into round 1 and freezes that champion for the rest of the opening: any of its team's plays that would move it can't be played."},
            {"key": "W", "name": "Stand Firm", "init": 1, "cd": 2, "aim": "free", "target": "self", "range": 0,
             "verbs": ["shield", "unstoppable"], "text": "Shield 4 and UNSTOPPABLE until round close: immune to root, push and pull.",
             "opening": [("move", "Top", "forward-left"), ("beacon", "Top", 1), ("move", "Mid", "forward-right")]},
            {"key": "E", "name": "Groundswell", "init": 4, "cd": 3, "aim": "fixed", "pattern": [(-1, 0), (-1, 1), (0, 1), (1, 0)],
             "verbs": ["area damage", "punish rooted"], "text": "Sweeps the four hexes in front and beside: damage ×2.4, and +3 to anyone rooted.",
             "opening": [("move", "Top", "forward-right"), ("move", "Mid", "forward-left"), ("move", "Jungle", "left")]},
        ],
        "signature": ("Iron March", [("move", "Top", "forward-right"), ("move", "Top", "forward-right"), ("move", "Top", "forward-left")]),
        "play": "Walk up the top lane behind your tower, Hook the enemy who steps closest, then Groundswell or let the team hit the rooted target. Stand Firm when you see their root or push coming.",
        "partners": "Ember and Lens: a rooted enemy can't walk out of Wildfire or a marked Lance.",
        "answers": "Cleanse and Oriel's Mend undo the root; Flash can't save someone already rooted.",
    },
    {
        "id": "ember", "name": "Ember", "role": "Jungle",
        "line": "Sets enemies alight so every action they take hurts — then cashes in.",
        "stats": {"hp": 26, "pow": 1.20, "arm": 0, "rch": 2, "spd": 3},
        "passive": ("Fuel", "When a burning enemy dies, Ember heals 3."),
        "abilities": [
            {"key": "Q", "name": "Kindle", "init": 1, "cd": 1, "aim": "free", "target": "enemy", "range": 2,
             "verbs": ["burn"], "text": "The enemy BURNS for 2 rounds: it takes 2 every time it resolves an ability or basic attack.",
             "opening": [("move", "Jungle", "forward-right"), ("move", "Jungle", "forward-right"), ("cast", "Jungle", "Q")],
             "openingAttack": "Advances through the middle and sets an enemy alight before round 1: it burns for every action it takes in the first round, opening casts included."},
            {"key": "W", "name": "Switch", "init": 2, "cd": 2, "aim": "free", "target": "ally", "range": 3,
             "verbs": ["swap"], "text": "SWAP places with an ally within 3: pull a wounded ally out, or put your diver where you stand.",
             "opening": [("move", "Jungle", "forward-left"), ("move", "Top", "forward-left"), ("move", "Jungle", "right")]},
            {"key": "E", "name": "Wildfire", "init": 3, "cd": 2, "aim": "rotatable", "pattern": [(-1, 1), (0, 1), (-1, 2)],
             "verbs": ["area damage", "punish burning"], "text": "A three-hex cone you turn to any of six facings: damage ×1.5, doubled on burning enemies.",
             "opening": [("move", "Jungle", "forward-right"), ("move", "Mid", "forward-right"), ("move", "Jungle", "forward-left")]},
        ],
        "signature": ("Smoke Trail", [("move", "Jungle", "forward-left"), ("move", "Jungle", "forward-right"), ("move", "Bottom", "forward-left")]),
        "play": "Kindle the champion the enemy needs to act — their healer or their carry — then make them choose between acting and burning. Switch to rescue or to engage. Save Wildfire for a burning cluster.",
        "partners": "Anchor's root holds a burning target in Wildfire's cone; Lens's walls corral it.",
        "answers": "Cleanse; Barrier doesn't help against burn. A team that passes and waits starves the burn.",
    },
    {
        "id": "lens", "name": "Lens", "role": "Mid",
        "line": "Marks one target for the whole team, and reshapes the board with walls.",
        "stats": {"hp": 23, "pow": 1.15, "arm": 0, "rch": 3, "spd": 2},
        "passive": ("Clarity", "At round close, if Lens took no damage this round, it gains 2 shield."),
        "abilities": [
            {"key": "Q", "name": "Mark", "init": 1, "cd": 1, "aim": "free", "target": "enemy", "range": 3,
             "verbs": ["mark"], "text": "MARK an enemy: the next hit it takes, from anyone, deals +3. Damage ×0.5.",
             "opening": [("move", "Mid", "forward-right"), ("beacon", "Mid", 3), ("move", "Bottom", "forward-right")]},
            {"key": "W", "name": "Prism Wall", "init": 2, "cd": 3, "aim": "free", "target": "empty", "range": 2,
             "verbs": ["wall"], "text": "Raise a WALL on an empty hex within 2 until the end of the next round: nothing walks, dashes or is pushed through it.",
             "opening": [("move", "Mid", "forward-left"), ("move", "Mid", "forward-right"), ("move", "Support", "forward-left")]},
            {"key": "E", "name": "Lance", "init": 3, "cd": 1, "aim": "rotatable", "pattern": [(0, 1), (0, 2), (0, 3)],
             "verbs": ["line damage"], "text": "A three-hex line you turn to any facing: damage ×1.6 to everyone on it.",
             "opening": [("move", "Mid", "forward-left"), ("cast", "Mid", "E"), ("move", "Mid", "back-right")],
             "openingAttack": "Poke and retreat: steps in, fires the line at whoever has advanced, and steps back to where it started."},
        ],
        "signature": ("Vantage", [("move", "Mid", "forward-left"), ("move", "Jungle", "forward-left"), ("beacon", "Jungle", 3)]),
        "play": "Stay at range 3 behind a wall. Mark whatever your team is about to hit, and use Prism Wall to cut off an escape or a dive. Lance when enemies line up between walls and towers.",
        "partners": "Anyone with a big single hit: Mark turns it into a kill. Anchor's Hook lines enemies up for Lance.",
        "answers": "Flash jumps walls. Divers that reach it: 23 HP and no armour.",
    },
    {
        "id": "oriel", "name": "Oriel", "role": "Support",
        "line": "Keeps one ally standing, and pulls it out of trouble.",
        "stats": {"hp": 28, "pow": 1.00, "arm": 1, "rch": 2, "spd": 2},
        "passive": ("Watchful", "When an adjacent ally takes damage, Oriel gains 1 shield."),
        "abilities": [
            {"key": "Q", "name": "Mend", "init": 1, "cd": 1, "aim": "free", "target": "ally", "range": 2,
             "verbs": ["heal", "cleanse"], "text": "Heal an ally (or itself) ×1.0 and CLEANSE it: removes root, burn, poison, mark and exhaust.",
             "opening": [("move", "Support", "forward-left"), ("cast", "Support", "Q"), ("beacon", "Support", 2)],
             "openingAttack": "A support's opening cast is the answer: cleanse and heal the ally the enemy just hooked or burned, before round 1."},
            {"key": "W", "name": "Tether", "init": 2, "cd": 2, "aim": "free", "target": "ally", "range": 3,
             "verbs": ["pull ally", "shield"], "text": "PULL an ally within 3 up to 2 hexes toward Oriel and shield it 2: the rescue.",
             "opening": [("move", "Support", "forward-right"), ("move", "Support", "forward-left"), ("move", "Bottom", "forward-right")]},
            {"key": "E", "name": "Sanctuary", "init": 4, "cd": 3, "aim": "fixed", "pattern": [(0, 0), (-1, 0), (1, 0), (-1, 1), (0, 1)],
             "verbs": ["area heal", "shield"], "text": "Oriel's hex and the four around its front: allies there heal ×1.0 and gain 3 shield.",
             "opening": [("move", "Support", "forward-left"), ("move", "Bottom", "forward-left"), ("move", "Mid", "forward-left")]},
        ],
        "signature": ("Vigil", [("move", "Support", "forward-right"), ("beacon", "Support", 1), ("move", "Support", "left")]),
        "play": "Stay one step behind the ally the enemy wants dead. Mend cleanses the root before they collapse; Tether drags the target out if they commit anyway. Sanctuary when the team has to stand and fight.",
        "partners": "Anchor (keeps the front line up), and any carry the enemy will focus.",
        "answers": "Ignite halves its heals; Exhaust can't stop a heal. Kill it first — 28 HP.",
    },
]


def run_opening(steps):
    """Plays one opening from the starting line, rest of the team on the line. Returns
    (ok, path) where path lists (kind, role, from, to-or-None-if-blocked)."""
    pos = dict(START)
    path = []
    for kind, role, arg in steps:
        if kind == "beacon":
            path.append(("beacon", role, pos[role], arg))
            continue
        if kind == "cast":
            # Resolves the ability's combat effect from where the caster stands now. The
            # target is chosen when the opening is played; with no legal target, the whole
            # opening is unavailable (proposed rule, design/v2-champions.md).
            path.append(("cast", role, pos[role], arg))
            continue
        d = DIRS[arg]
        to = (pos[role][0] + d[0], pos[role][1] + d[1])
        if to not in HEXES or to in TOWERS or to in pos.values():
            path.append(("blocked", role, pos[role], to))
            return False, path, pos
        path.append(("move", role, pos[role], to))
        pos[role] = to
    return True, path, pos


def check():
    problems = 0
    for c in CHAMPIONS:
        sets = [(a["name"], a["opening"]) for a in c["abilities"]] + [c["signature"]]
        for name, steps in sets:
            ok, path, _ = run_opening(steps)
            if not ok:
                problems += 1
                print(f"  BLOCKED  {c['name']:7} {name:12} {steps}  at {path[-1]}")
            assert all(s[1] in ROLES for s in steps)
            assert len(steps) == 3
            for s in steps:
                if s[0] == "cast":
                    assert s[1] == c["role"], f"{c['name']}: a champion can only cast its own ability"
                    assert s[2] in [a["key"] for a in c["abilities"]]
        casts = [a for a in c["abilities"] if any(st[0] == "cast" for st in a["opening"])]
        assert len(casts) == 1, f"{c['name']}: exactly one opening attack"
        for a in casts:
            _, path, _ = run_opening(a["opening"])
            at = next(p[2] for p in path if p[0] == "cast")
            print(f"  opening attack: {c['name']:7} {a['name']:11} cast from {at} (row {at[1]})")
    print(f"{sum(4 for _ in CHAMPIONS)} openings checked on Field 7 with solid towers; {problems} blocked from the starting line")
    return problems


def sheet():
    data = {"board": BOARD, "champions": [], "spells": SPELLS}
    for c in CHAMPIONS:
        cc = dict(c)
        cc["openings"] = []
        for name, steps in [(a["name"], a["opening"]) for a in c["abilities"]] + [c["signature"]]:
            ok, path, final = run_opening(steps)
            cc["openings"].append({"name": name, "ok": ok, "path": path, "final": {k: list(v) for k, v in final.items()},
                                   "steps": [list(s) for s in steps]})
        data["champions"].append(cc)
    tpl = open("design/v2-champions/champion-sheet.template.html").read()
    open("design/v2-champions/champion-sheet.html", "w").write(tpl.replace("__DATA__", json.dumps(data)))


if __name__ == "__main__":
    check()
    sheet()
