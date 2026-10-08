# Authoring script for the v2 roster: writes assets/data/champions/*.json and spells.json,
# after checking every opening on Field 7 with solid towers (moves strict, casts free).
import json, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BOARD = json.load(open(f"{ROOT}/assets/data/boards/board_field7.json"))
HEXES = {tuple(h) for h in BOARD["hexes"]}
TOWERS = {tuple(h) for h in BOARD["towers"]}
ROLES = ["top", "jungle", "mid", "bottom", "support"]
START = {ROLES[i]: tuple(h) for i, h in enumerate(BOARD["startA"])}
DIRS = {"right": (1, 0), "back-right": (1, -1), "back-left": (0, -1), "left": (-1, 0), "forward-left": (-1, 1), "forward-right": (0, 1)}

def A(name, init, cd, effects, opening, scales, up, down, target=None, pattern=None, rb=0, printed=None, slot=None):
    a = {"name": name, "initiative": init, "cooldown": cd}
    if target: a["target"] = target
    if rb: a["rangeBonus"] = rb
    if pattern: a["pattern"] = pattern
    a["effects"] = effects
    a["scalesFrom"], a["moldUp"], a["moldDown"] = scales, up, down
    if printed is not None: a["printedSigil"] = printed
    if slot is not None: a["slotSigil"] = slot
    a["opening"] = opening
    return a

def C(num, cid, name, role, glyph, stats, passive, abilities, sig_name, sig, line):
    return num, cid, {"id": cid, "name": name, "role": role, "glyph": glyph, "line": line,
                      "stats": dict(zip(["vit", "pow", "arm", "rch", "spd"], stats)),
                      "passive": passive, "abilities": abilities, "signature": {"name": sig_name, "opening": sig}}

dmg = lambda p, **kw: dict({"kind": "Damage", "power": p}, **kw)

ROSTER = [
    C(1, "anchor", "Anchor", "Top", "A", [35000, 950, 1000, 2000, 2000],
      {"name": "Iron Will", "trigger": "OnDamaged", "effect": "ShieldSelf", "amount": 1},
      [A("Stand Firm", 1, 2, [{"kind": "Shield", "amount": 4}, {"kind": "Unstoppable", "amount": 1}],
         ["move top forward-left", "beacon top 0", "move mid forward-right"], "arm", ["vit", 30], ["spd", 25], target="Self", printed=0),
       A("Chain Hook", 2, 2, [dmg(1100), {"kind": "Displace", "amount": -1}, {"kind": "Root", "amount": 2}],
         ["move top forward-right", "move top forward-right", "cast top w"], "pow", ["arm", 25], ["spd", 25], slot=0),
       A("Groundswell", 4, 3, [dmg(2600, bonusVs="Rooted", bonusPermille=1500, bonusFlat=2)],
         ["move top forward-right", "move mid forward-left", "move jungle left"], "pow", ["vit", 60], ["rch", 60], pattern=[[-1, 0], [-1, 1], [0, 1], [1, 0]])],
      "Iron March", ["move top forward-right", "move top forward-right", "move top forward-left"],
      "Pins one enemy in place so the team can collapse on it."),
    C(2, "ember", "Ember", "Jungle", "E", [26000, 1200, 0, 2000, 3000],
      {"name": "Ambush", "trigger": "OnEnemyEntersReach", "effect": "Strike", "amount": 1},
      [A("Kindle", 1, 1, [{"kind": "Burn", "amount": 2, "rounds": 2}],
         ["move jungle forward-right", "move jungle forward-right", "cast jungle q"], "pow", ["spd", 25], ["arm", 25], slot=1),
       A("Switch", 2, 2, [{"kind": "Swap"}],
         ["move jungle forward-left", "move top forward-left", "move jungle right"], "spd", ["pow", 25], ["vit", 25], target="Ally", rb=1),
       A("Wildfire", 3, 2, [dmg(1500, bonusVs="Burning", bonusPermille=1500, bonusFlat=2)],
         ["move jungle forward-right", "move mid forward-right", "move jungle forward-left"], "pow", ["spd", 60], ["vit", 60], pattern=[[-1, 1], [0, 1], [-1, 2]], printed=1)],
      "Smoke Trail", ["move jungle forward-left", "move jungle forward-right", "move bottom forward-left"],
      "Sets enemies alight so every action they take hurts — then cashes in."),
    C(3, "lens", "Lens", "Mid", "L", [25000, 1150, 0, 3000, 2000],
      {"name": "Clarity", "trigger": "OnRoundClose", "effect": "ShieldSelf", "amount": 2},
      [A("Mark", 1, 1, [dmg(700), {"kind": "Mark", "amount": 3}],
         ["move mid forward-right", "beacon mid 2", "move bottom forward-right"], "pow", ["arm", 25], ["spd", 25], slot=2),
       A("Prism Wall", 2, 3, [{"kind": "Wall", "amount": 2}],
         ["move mid forward-left", "move mid forward-right", "move support forward-left"], "rch", ["vit", 30], ["pow", 25], target="EmptyHex", rb=-1),
       A("Lance", 3, 1, [dmg(1800)],
         ["move mid forward-left", "cast mid e", "move mid back-right"], "pow", ["rch", 30], ["vit", 60], pattern=[[0, 1], [0, 2], [0, 3]], printed=2)],
      "Vantage", ["move mid forward-left", "move jungle forward-left", "beacon jungle 2"],
      "Marks one target for the whole team, and reshapes the board with walls."),
    C(4, "oriel", "Oriel", "Support", "O", [28000, 1000, 1000, 2000, 2000],
      {"name": "Grace", "trigger": "OnRoundClose", "effect": "HealSelf", "amount": 2},
      [A("Mend", 1, 1, [{"kind": "Heal", "power": 1000}, {"kind": "Cleanse"}],
         ["move support forward-left", "cast support q", "beacon support 1"], "pow", ["vit", 25], ["spd", 25], target="Ally", slot=1),
       A("Tether", 2, 2, [{"kind": "PullAlly", "amount": 2}, {"kind": "Shield", "amount": 2}],
         ["move support forward-right", "move support forward-left", "move bottom forward-right"], "spd", ["arm", 25], ["pow", 25], target="Ally", rb=1),
       A("Sanctuary", 4, 3, [{"kind": "Heal", "power": 1000}, {"kind": "Shield", "amount": 3}],
         ["move support forward-left", "move bottom forward-left", "move mid forward-left"], "pow", ["vit", 60], ["rch", 60], target="Ally", pattern=[[-1, 0], [1, 0], [-1, 1], [0, 1]])],
      "Vigil", ["move support forward-right", "beacon support 0", "move support left"],
      "Keeps one ally standing, and pulls it out of trouble."),
    C(5, "bulwark", "Bulwark", "Top", "B", [33000, 900, 2000, 1000, 2000],
      {"name": "Spiked Plate", "trigger": "OnDamaged", "effect": "Retaliate", "amount": 1},
      [A("Shove", 1, 1, [dmg(1000), {"kind": "Displace", "amount": 2, "slam": 3}],
         ["move top forward-left", "move jungle forward-left", "beacon top 0"], "pow", ["vit", 25], ["spd", 25], printed=0),
       A("Rampart", 2, 3, [{"kind": "Wall", "amount": 2}],
         ["move top forward-right", "move jungle forward-right", "move mid forward-right"], "arm", ["vit", 30], ["pow", 25], target="EmptyHex"),
       A("Quake", 4, 3, [dmg(2100), {"kind": "Displace", "amount": 1}],
         ["move top forward-right", "move top forward-right", "cast top e"], "pow", ["arm", 60], ["spd", 60], pattern=[[-1, 1], [0, 1], [-1, 2], [0, 2]])],
      "Hold the Line", ["move top forward-left", "move top right", "move jungle forward-right"],
      "Holds the ground in front of its tower: shoves enemies off it and walls the way."),
    C(6, "viper", "Viper", "Jungle", "V", [25000, 1200, 0, 1000, 3000],
      {"name": "Hunger", "trigger": "OnRoundClose", "effect": "EmpowerSelf", "amount": 15, "stat": "pow"},
      [A("Lunge", 1, 1, [{"kind": "Dash", "amount": 3}],
         ["move jungle forward-right", "move jungle forward-right", "beacon jungle 1"], "spd", ["pow", 25], ["arm", 25], target="EmptyHex"),
       A("Fang", 2, 1, [dmg(1200), {"kind": "Poison", "amount": 2, "rounds": 2}],
         ["move jungle forward-left", "cast jungle q", "cast jungle w"], "pow", ["spd", 40], ["vit", 40], slot=1),
       A("Ambush", 3, 2, [dmg(1600, bonusVs="Poisoned", bonusPermille=1500, bonusFlat=2)],
         ["move jungle forward-right", "move mid forward-right", "move jungle forward-right"], "pow", ["spd", 60], ["vit", 60], pattern=[[0, 1], [-1, 1]])],
      "Stalk", ["move jungle forward-left", "move jungle forward-right", "move top forward-left"],
      "Hunts the isolated: poisons a target, then dives on it."),
    C(7, "tempest", "Tempest", "Mid", "T", [23000, 1100, 0, 3000, 2000],
      {"name": "Static Skin", "trigger": "OnDamaged", "effect": "ShieldSelf", "amount": 1},
      [A("Zap", 1, 0, [dmg(900)],
         ["move mid forward-right", "move mid forward-left", "move top forward-right"], "pow", ["spd", 25], ["arm", 25], printed=2),
       A("Gust", 2, 1, [dmg(700), {"kind": "Displace", "amount": 2, "slam": 3}],
         ["move mid forward-left", "beacon mid 2", "move bottom forward-left"], "pow", ["rch", 30], ["vit", 30], slot=2),
       A("Eye of the Storm", 4, 3, [dmg(1900), {"kind": "Displace", "amount": 1}],
         ["cast top q", "cast jungle q", "cast mid q"], "pow", ["vit", 60], ["spd", 60], pattern=[[1, 0], [0, 1], [-1, 1], [-1, 0], [0, -1], [1, -1]])],
      "Updraft", ["move mid forward-right", "move mid forward-left", "move jungle forward-left"],
      "Throws the enemy line around: knockbacks that set up its allies."),
    C(8, "ranger", "Ranger", "Bottom", "R", [25000, 1100, 0, 3000, 2000],
      {"name": "Vengeance", "trigger": "OnAllyDies", "effect": "EmpowerSelf", "amount": 50, "stat": "pow"},
      [A("Volley", 1, 0, [dmg(1000, bonusVs="Marked", bonusPermille=1500, bonusFlat=2)],
         ["move bottom forward-right", "cast mid q", "cast bottom q"], "pow", ["vit", 25], ["arm", 25], slot=0),
       A("Pin Shot", 2, 2, [dmg(1100), {"kind": "Root", "amount": 1}],
         ["move bottom forward-left", "move support forward-left", "beacon bottom 0"], "pow", ["spd", 40], ["vit", 40]),
       A("Rain of Arrows", 4, 3, [dmg(2400)],
         ["move bottom forward-right", "move support forward-right", "move jungle forward-left"], "pow", ["arm", 60], ["spd", 60], pattern=[[0, 2], [-1, 2], [-1, 3], [0, 3], [1, 2]])],
      "Fall Back", ["move bottom forward-left", "move bottom right", "move support forward-right"],
      "Shoots from the back line and cashes in the marks its team sets."),
    C(9, "gunner", "Gunner", "Bottom", "G", [26000, 1150, 0, 3000, 2000],
      {"name": "Overwatch", "trigger": "OnEnemyEntersReach", "effect": "Strike", "amount": 1},
      [A("Snipe", 1, 1, [dmg(1100, bonusVs="Rooted", bonusPermille=1500, bonusFlat=2)],
         ["cast top w", "cast bottom q", "move bottom forward-right"], "pow", ["spd", 25], ["arm", 25], slot=2),
       A("Recoil", 2, 2, [{"kind": "Dash", "amount": 2}],
         ["move bottom forward-left", "move bottom forward-right", "beacon bottom 2"], "spd", ["pow", 25], ["vit", 25], target="EmptyHex"),
       A("Buckshot", 3, 1, [dmg(1600), {"kind": "Displace", "amount": 1, "slam": 3}],
         ["move bottom forward-right", "move support forward-right", "move mid forward-right"], "pow", ["vit", 60], ["rch", 60], pattern=[[0, 1], [-1, 1], [0, 2]], printed=0)],
      "Hold Position", ["move bottom forward-left", "move mid forward-left", "move support forward-left"],
      "Bursts from safety, hardest on a target that can't move."),
    C(10, "bastion", "Bastion", "Support", "S", [30000, 900, 2000, 2000, 2000],
      {"name": "Guardian", "trigger": "OnAllyDies", "effect": "ShieldSelf", "amount": 3},
      [A("Aegis", 1, 1, [{"kind": "Shield", "amount": 5}],
         ["move support forward-left", "move bottom forward-left", "beacon support 2"], "arm", ["vit", 25], ["pow", 25], target="Ally", slot=2),
       A("Rally", 2, 2, [{"kind": "Heal", "power": 1000}, {"kind": "Unstoppable", "amount": 1}],
         ["move support forward-right", "move support forward-left", "move bottom forward-left"], "pow", ["vit", 30], ["spd", 30], target="Ally"),
       A("Judgement", 4, 3, [dmg(2200), {"kind": "Exhaust", "amount": 2}],
         ["move support forward-left", "move support forward-left", "cast support e"], "pow", ["arm", 60], ["spd", 60], pattern=[[-1, 1], [-2, 2], [-3, 3]])],
      "Bastion Stance", ["move support forward-left", "beacon support 1", "move mid forward-right"],
      "Shields the team and drains the dive."),
    C(11, "briar", "Briar", "Top", "R", [33000, 950, 1000, 2000, 2000],
      {"name": "Thorns", "trigger": "OnDamaged", "effect": "Retaliate", "amount": 1},
      [A("Entangle", 1, 2, [dmg(600), {"kind": "Root", "amount": 1}, {"kind": "Poison", "amount": 1, "rounds": 2}],
         ["move top forward-right", "cast top q", "move top back-left"], "pow", ["arm", 25], ["spd", 25], printed=0),
       A("Thornwall", 2, 3, [{"kind": "Wall", "amount": 2}],
         ["move top forward-left", "beacon top 1", "move jungle forward-right"], "arm", ["vit", 30], ["pow", 25], target="EmptyHex"),
       A("Bloom", 4, 3, [dmg(2000, bonusVs="Poisoned", bonusPermille=1500, bonusFlat=2)],
         ["move top forward-right", "move top forward-right", "move mid forward-left"], "pow", ["vit", 60], ["rch", 60], pattern=[[-1, 1], [0, 1], [-1, 2], [0, 2], [-2, 2]])],
      "Overgrowth", ["move top forward-left", "move top right", "move jungle forward-right"],
      "Roots an enemy in thorns and lets the poison, and the team, finish it."),
    C(12, "talon", "Talon", "Jungle", "N", [25000, 1200, 0, 1000, 3000],
      {"name": "Scent", "trigger": "OnEnemyEntersReach", "effect": "Strike", "amount": 1},
      [A("Rend", 1, 1, [dmg(900), {"kind": "Mark", "amount": 2}],
         ["move jungle forward-left", "move jungle forward-right", "move top right"], "pow", ["spd", 25], ["arm", 25], printed=1),
       A("Pounce", 2, 2, [{"kind": "Dash", "amount": 3}],
         ["move jungle forward-right", "move jungle forward-right", "move top forward-right"], "spd", ["pow", 25], ["vit", 25], target="EmptyHex"),
       A("Execute", 3, 2, [dmg(1400, bonusVs="Marked", bonusPermille=1500, bonusFlat=2)],
         ["move jungle forward-right", "cast jungle q", "cast jungle e"], "pow", ["spd", 60], ["vit", 60], pattern=[[0, 1], [-1, 1]], slot=1)],
      "Prowl", ["move jungle forward-left", "move jungle forward-right", "beacon jungle 1"],
      "Dives onto whatever its team has marked, and doubles down on it."),
    C(13, "pyre", "Pyre", "Mid", "P", [23000, 1150, 0, 3000, 2000],
      {"name": "Ember Heart", "trigger": "OnRoundClose", "effect": "HealSelf", "amount": 1},
      [A("Scorch", 1, 1, [dmg(600), {"kind": "Burn", "amount": 2, "rounds": 2}],
         ["move mid forward-right", "cast mid q", "beacon mid 2"], "pow", ["rch", 25], ["vit", 25], slot=2),
       A("Smoke", 2, 2, [dmg(500), {"kind": "Exhaust", "amount": 1}],
         ["move mid forward-left", "move bottom forward-left", "move mid forward-right"], "pow", ["arm", 30], ["spd", 30]),
       A("Inferno", 4, 3, [dmg(1600, bonusVs="Burning", bonusPermille=1500, bonusFlat=2)],
         ["move mid forward-right", "move mid forward-left", "move support forward-left"], "pow", ["vit", 60], ["spd", 60], pattern=[[0, 1], [-1, 2], [0, 2], [-1, 3]], printed=2)],
      "Heat Haze", ["move mid forward-left", "beacon mid 2", "move jungle forward-left"],
      "Spreads fire along the enemy line, then burns it down with Ember."),
    C(14, "mortar", "Mortar", "Bottom", "M", [24000, 1100, 0, 3000, 2000],
      {"name": "Entrenched", "trigger": "OnRoundClose", "effect": "ShieldSelf", "amount": 2},
      [A("Shell", 1, 1, [dmg(800), {"kind": "Displace", "amount": 1, "slam": 4}],
         ["cast bottom q", "move bottom forward-left", "move support forward-left"], "pow", ["rch", 25], ["spd", 25], slot=0),
       A("Tripwire", 2, 3, [{"kind": "Wall", "amount": 2}],
         ["move bottom forward-right", "move bottom forward-left", "beacon bottom 0"], "rch", ["vit", 30], ["pow", 25], target="EmptyHex"),
       A("Barrage", 4, 3, [dmg(2200)],
         ["move support forward-left", "move bottom right", "move mid forward-right"], "pow", ["arm", 60], ["vit", 60], pattern=[[0, 2], [0, 3], [-1, 3], [-1, 4]])],
      "Dig In", ["move bottom forward-left", "move mid forward-left", "beacon bottom 1"],
      "Shells enemies from far back, into walls, towers and each other."),
    C(15, "seer", "Seer", "Support", "E", [27000, 900, 1000, 3000, 2000],
      {"name": "Foresight", "trigger": "OnRoundClose", "effect": "ShieldSelf", "amount": 1},
      [A("Omen", 1, 1, [{"kind": "Mark", "amount": 3}],
         ["move support forward-left", "cast support q", "move support back-right"], "pow", ["rch", 25], ["vit", 25], slot=1),
       A("Bind", 2, 2, [dmg(500), {"kind": "Root", "amount": 2}],
         ["move support forward-right", "move bottom forward-right", "beacon support 1"], "pow", ["spd", 30], ["vit", 30], rb=-1, printed=0),
       A("Revelation", 4, 3, [dmg(1200), {"kind": "Mark", "amount": 2}, {"kind": "Exhaust", "amount": 1}],
         ["move support forward-left", "move support forward-right", "move bottom forward-left"], "pow", ["arm", 60], ["spd", 60], pattern=[[0, 1], [-1, 2], [0, 2], [-1, 1]])],
      "Vision", ["move support forward-right", "move support left", "beacon support 2"],
      "Calls the target: marks and binds one enemy so the whole team knows where to hit."),
]

# Champions that work well together. Data for the draft screen, the AI drafter and the
# harness; the rules never read it. Score = pairs of a group's members on one team.
SYNERGIES = [
    {"id": "lockdown", "name": "Lockdown", "members": ["anchor", "briar", "seer", "ranger", "gunner"],
     "idea": "Root a target, then hit what can't move: Snipe and Groundswell hit half again as hard, and a rooted target slams when pushed."},
    {"id": "called-shot", "name": "Called Shot", "members": ["lens", "seer", "talon", "ranger"],
     "idea": "Mark one enemy and spend the mark: Execute and Volley hit half again as hard, and every hit adds the mark."},
    {"id": "wildfire", "name": "Wildfire", "members": ["ember", "pyre"],
     "idea": "Set the line alight; Wildfire and Inferno hit burning targets half again as hard."},
    {"id": "venom", "name": "Venom", "members": ["viper", "briar"],
     "idea": "Poison first; Ambush and Bloom hit poisoned targets half again as hard."},
    {"id": "crush", "name": "Crush", "members": ["bulwark", "tempest", "mortar", "lens"],
     "idea": "Walls and pushes: shove enemies into walls, towers and each other — a stopped push slams for extra damage."},
    {"id": "dive", "name": "Dive and Retrieve", "members": ["viper", "talon", "oriel"],
     "idea": "Divers go deep for the kill; Oriel's Tether and Mend bring them back out."},
    {"id": "hold", "name": "Hold the Line", "members": ["bastion", "bulwark", "anchor", "briar"],
     "idea": "Stand in front of your towers: shields, unstoppable front-liners and thorns that punish whoever hits them."},
]

SPELLS = [
    {"name": "Flash", "initiative": 1, "cooldown": 5, "target": "EmptyHex", "range": 2, "effects": [{"kind": "Dash", "amount": 2}],
     "text": "Jump to an empty hex up to 2 away, over champions, walls and towers."},
    {"name": "Barrier", "initiative": 1, "cooldown": 4, "target": "Self", "effects": [{"kind": "Shield", "amount": 6}],
     "text": "Shield 6 until round close."},
    {"name": "Ignite", "initiative": 2, "cooldown": 5, "target": "Enemy", "range": 3,
     "effects": [{"kind": "Burn", "amount": 2, "rounds": 2}, {"kind": "Wound", "rounds": 2}],
     "text": "An enemy within 3 burns for 2 rounds (2 each time it acts) and its healing is halved."},
    {"name": "Heal", "initiative": 1, "cooldown": 5, "target": "Self",
     "effects": [{"kind": "Heal", "amount": 4}, {"kind": "HealWoundedAlly", "amount": 4, "rounds": 2}],
     "text": "Heal yourself and the most wounded ally within 2 for 4."},
    {"name": "Exhaust", "initiative": 2, "cooldown": 5, "target": "Enemy", "range": 2, "effects": [{"kind": "Exhaust", "amount": 2}],
     "text": "An enemy within 2 deals half damage until the end of the next half."},
    {"name": "Teleport", "initiative": 4, "cooldown": 6, "target": "TeleportHex", "effects": [{"kind": "Teleport"}],
     "text": "Move to an empty hex next to a friendly tower or beacon — from anywhere, even your spawn hex."},
    {"name": "Smite", "initiative": 1, "cooldown": 5, "target": "Structure", "range": 2, "effects": [{"kind": "StructureDamage", "amount": 6}],
     "text": "Deal 6 to a tower or open nexus within 2. Defenders don't reduce it."},
    {"name": "Cleanse", "initiative": 1, "cooldown": 4, "target": "Self", "effects": [{"kind": "Cleanse"}, {"kind": "Unstoppable", "amount": 1}],
     "text": "Remove root, burn, poison, mark, exhaust and wound from yourself; unstoppable this half."},
]

def run(ops):
    pos = dict(START)
    for ins in ops:
        kind, role, arg = ins.split()
        if kind != "move": continue
        d = DIRS[arg]
        to = (pos[role][0] + d[0], pos[role][1] + d[1])
        if to not in HEXES or to in TOWERS or to in pos.values():
            return f"blocked: {role} {arg} from {pos[role]} to {to}"
        pos[role] = to
    return None

bad = 0
for num, cid, c in ROSTER:
    sets = [(a["name"], a["opening"]) for a in c["abilities"]] + [(c["signature"]["name"], c["signature"]["opening"])]
    casts = [n for n, ops in sets if any(o.startswith("cast") for o in ops)]
    if len(casts) != 1: print(f"{c['name']}: {len(casts)} attack openings"); bad += 1
    for n, ops in sets:
        err = run(ops)
        if err: print(f"  {c['name']:8} {n:16} {err}"); bad += 1
print("problems:", bad)
if bad and "--force" not in sys.argv: sys.exit(1)

out = f"{ROOT}/assets/data/champions"
os.makedirs(out, exist_ok=True)
for f in os.listdir(out):
    if f.endswith(".json"): os.remove(os.path.join(out, f))
for num, cid, c in ROSTER:
    with open(f"{out}/champion_{num:02d}_{cid}.json", "w") as fh:
        fh.write(json.dumps(c, indent=2, ensure_ascii=False) + "\n")
with open(f"{ROOT}/assets/data/spells.json", "w") as fh:
    fh.write(json.dumps({"spells": [{k: v for k, v in sp.items() if k != "text"} for sp in SPELLS]}, indent=2) + "\n")
ids = {cid for _, cid, _ in ROSTER}
for g in SYNERGIES:
    missing = [m for m in g["members"] if m not in ids]
    if missing: sys.exit(f"synergy {g['id']}: unknown {missing}")
with open(f"{ROOT}/assets/data/synergies.json", "w") as fh:
    fh.write(json.dumps({"groups": SYNERGIES}, indent=2) + "\n")
print("wrote", len(ROSTER), "champions,", len(SPELLS), "spells and", len(SYNERGIES), "synergy groups")
