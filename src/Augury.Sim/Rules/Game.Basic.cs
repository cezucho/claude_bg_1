using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>The basic phase (§4, <c>movement-and-targeting.md</c>).</summary>
public sealed partial class Game
{
    private bool CanBasic(in MatchState s, Team t)
    {
        if (s.BasicsTaken[TeamIndex(t)] >= Rules.BasicsPerHalf) return false;
        var probe = new List<Command>();
        LegalBasics(s, t, probe, stopAtFirst: true);
        return probe.Count > 0;
    }

    private void LegalBasics(in MatchState s, Team t, List<Command> into, bool stopAtFirst = false)
    {
        if (s.BasicsTaken[TeamIndex(t)] >= Rules.BasicsPerHalf) return;
        int first = MatchState.FirstSlot(t);
        for (int slot = first; slot < first + 5; slot++)
        {
            Champion c = s.Champions[slot];
            if (c.Presence == Presence.Dead || c.Has(ChampFlags.BasicUsed)) continue;

            foreach (HexCoord dest in MoveDestinations(s, slot))
            {
                into.Add(new Command(CommandKind.BasicMove, (byte)slot, 0, Target.At(dest)));
                if (stopAtFirst) return;
            }

            if (!c.OnBoard) continue;
            foreach (Target target in BasicTargets(s, slot))
            {
                into.Add(new Command(CommandKind.BasicAttack, (byte)slot, 0, target));
                if (stopAtFirst) return;
            }
        }
    }

    /// <summary>
    /// Hexes reachable by a path of 1…SPD unoccupied playable hexes. From the spawn hex the
    /// first step must land on the board.
    /// </summary>
    public List<HexCoord> MoveDestinations(in MatchState s, int slot)
    {
        Champion c = s.Champions[slot];
        int speed = Speed(c);
        var result = new List<HexCoord>();
        if (speed <= 0 || c.Rooted) return result;

        var seen = new HashSet<HexCoord> { c.Pos };
        var frontier = new List<HexCoord> { c.Pos };
        for (int step = 1; step <= speed && frontier.Count > 0; step++)
        {
            var next = new List<HexCoord>();
            foreach (HexCoord at in frontier)
            {
                for (int k = 0; k < 6; k++)
                {
                    HexCoord to = at + Hex.Directions[Board.FrameDirection(k, c.Team)];
                    if (!Board.Playable(to) || IsSolidTower(to) || WallAt(s, to) >= 0 || !seen.Add(to)) continue;
                    int occupant = ChampionAt(s, to);
                    if (occupant >= 0)
                    {
                        if (Rules.FriendliesBlock || s.Champions[occupant].Team != c.Team) continue;
                        next.Add(to);   // pass through a friend, never end on one
                        continue;
                    }

                    next.Add(to);
                    result.Add(to);
                }
            }

            frontier = next;
        }

        return result;
    }

    /// <summary>Everything a basic attack from this champion may hit (§4).</summary>
    public List<Target> BasicTargets(in MatchState s, int slot)
    {
        Champion c = s.Champions[slot];
        var result = new List<Target>();
        int reach = Reach(c);
        Team enemy = MatchState.Other(c.Team);

        for (int i = 0; i < 10; i++)
        {
            Champion e = s.Champions[i];
            if (e.Team == enemy && e.OnBoard && HexCoord.Distance(c.Pos, e.Pos) <= reach) result.Add(Target.Champ(i));
        }

        AddStructureTargets(s, c.Team, c.Pos, reach, result);

        for (int b = 0; b < 12; b++)
        {
            Beacon beacon = s.Beacons[b];
            if (beacon.Team == enemy && HexCoord.Distance(c.Pos, beacon.Pos) <= reach)
            {
                result.Add(new Target(TargetKind.Beacon, (byte)b, beacon.Pos));
            }
        }

        return result;
    }

    private void AddStructureTargets(in MatchState s, Team attacker, HexCoord from, int reach, List<Target> result)
    {
        for (int t = 0; t < 5; t++)
        {
            if (s.Towers[t].Owner != attacker && HexCoord.Distance(from, s.Towers[t].Pos) <= reach)
            {
                result.Add(new Target(TargetKind.Tower, (byte)t, s.Towers[t].Pos));
            }
        }

        Team enemy = MatchState.Other(attacker);
        if (NexusVulnerable(s, enemy) && Board.NexusHexes(enemy).Any(h => HexCoord.Distance(from, h) <= reach))
        {
            result.Add(new Target(TargetKind.Nexus, (byte)enemy, default));
        }
    }

    /// <summary>
    /// A nexus is vulnerable once its team has lost <c>NexusGateTowers</c> of its two home
    /// towers (D-002). The gate count lives in the state so this stays a static read.
    /// </summary>
    public static bool NexusVulnerable(in MatchState s, Team owner)
    {
        int lost = 0;
        for (int t = 0; t < 5; t++)
        {
            if (s.Towers[t].Home == owner && s.Towers[t].Owner != owner) lost++;
        }

        return lost >= s.NexusGate;
    }

    private void ApplyBasicMove(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        ref Champion c = ref s.Champions[cmd.Champion];
        bool entering = c.Presence == Presence.InSpawn;
        c.Pos = cmd.Target.Hex;
        c.Presence = Presence.OnBoard;
        c.Flags |= ChampFlags.BasicUsed;
        Team team = c.Team;
        s.BasicsTaken[TeamIndex(team)]++;
        Log(log, EventKind.Basic, $"{Name(s, cmd.Champion)} {(entering ? "enters play at" : "moves to")} {Fmt(cmd.Target.Hex)}");

        // OnEnemyEntersReach (§10).
        for (int i = 0; i < 10; i++)
        {
            Champion e = s.Champions[i];
            if (e.Team == team || !e.OnBoard) continue;
            if (Def(e).Passive.Trigger == PassiveTrigger.OnEnemyEntersReach
                && HexCoord.Distance(e.Pos, cmd.Target.Hex) <= Reach(e))
            {
                FirePassive(ref s, i, PassiveTrigger.OnEnemyEntersReach, cmd.Champion, log);
            }
        }

        s.Active = MatchState.Other(team);
    }

    private void ApplyBasicAttack(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        ref Champion c = ref s.Champions[cmd.Champion];
        c.Flags |= ChampFlags.BasicUsed;
        Team team = c.Team;
        s.BasicsTaken[TeamIndex(team)]++;
        int raw = (int)Arith.FloorDiv((long)Rules.BasicBase * Pow(s.Champions[cmd.Champion]), 1000);
        if (s.Champions[cmd.Champion].ExhaustHalves > 0) raw = Math.Max(1, raw / 2);
        BurnOnAct(ref s, cmd.Champion, log);

        switch (cmd.Target.Kind)
        {
            case TargetKind.Champion:
                int dmg = Math.Max(1, raw - Armour(s.Champions[cmd.Target.Index]));
                Log(log, EventKind.Basic, $"{Name(s, cmd.Champion)} basic-attacks {Name(s, cmd.Target.Index)}");
                DamageChampion(ref s, cmd.Target.Index, dmg, cmd.Champion, fromPassive: false, log);
                break;

            case TargetKind.Tower:
            case TargetKind.Nexus:
                Log(log, EventKind.Basic, $"{Name(s, cmd.Champion)} basic-attacks {StructureName(cmd.Target)}");
                DamageStructure(ref s, team, cmd.Target, Math.Max(1, raw), log);
                break;

            case TargetKind.Beacon:
                ref Beacon b = ref s.Beacons[cmd.Target.Index];
                b.Durability--;
                Log(log, EventKind.Beacon, $"{Name(s, cmd.Champion)} strikes the {b.Team} beacon at {Fmt(b.Pos)} ({b.Durability} left)");
                if (b.Durability == 0)
                {
                    Log(log, EventKind.Beacon, $"  The beacon at {Fmt(b.Pos)} breaks.");
                    b.Team = Team.None;
                }

                break;
        }

        s.Active = MatchState.Other(team);
    }

    /// <summary>"tower (0,-2)" or "nexus B".</summary>
    public static string StructureName(in Target t) =>
        t.Kind == TargetKind.Nexus ? $"nexus {(Team)t.Index}" : $"tower {Fmt(Board.TowerHexes[t.Index])}";
}
