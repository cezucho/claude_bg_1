using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>Draft (§2) and the Opening Phase (§3, <c>opening-phase.md</c>).</summary>
public sealed partial class Game
{
    // ───────────────────────────── draft ─────────────────────────────

    private void LegalDraft(in MatchState s, List<Command> into)
    {
        // A champion leaves the pool once either team picks it (D-020, revised). If a
        // roster is too small for that, mirror picks are allowed rather than stalling.
        int before = into.Count;
        AddDraftPicks(s, into, excludeDrafted: true);
        if (into.Count == before) AddDraftPicks(s, into, excludeDrafted: false);
    }

    private void AddDraftPicks(in MatchState s, List<Command> into, bool excludeDrafted)
    {
        Team t = s.Active;
        for (int r = 0; r < 5; r++)
        {
            int slot = MatchState.Slot(t, (Role)r);
            if (s.Champions[slot].Def != 255) continue;
            foreach (int def in Content.ForRole((Role)r))
            {
                if (excludeDrafted && Drafted(s, def)) continue;
                into.Add(new Command(CommandKind.Draft, (byte)slot, (byte)def, Target.None));
            }
        }
    }

    private static bool Drafted(in MatchState s, int def)
    {
        for (int i = 0; i < 10; i++)
        {
            if (s.Champions[i].Def == def) return true;
        }

        return false;
    }

    private void ApplyDraft(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        s.Champions[cmd.Champion].Def = cmd.Ability;
        Log(log, EventKind.Draft, $"{s.Active} drafts {Content.Champions[cmd.Ability].Name} ({(Role)(cmd.Champion % 5)}).");
        s.DraftPicks++;
        if (s.DraftPicks < 10) s.Active = DraftPicker(s.DraftPicks);
    }

    // ───────────────────────────── opening ─────────────────────────────

    private static bool OpeningComplete(in MatchState s, Team t)
    {
        int first = MatchState.FirstSlot(t);
        for (int i = first; i < first + 5; i++)
        {
            if (!s.Champions[i].Has(ChampFlags.OpeningDone)) return false;
        }

        return true;
    }

    private void LegalOpening(in MatchState s, List<Command> into)
    {
        Team t = s.Active;
        int first = MatchState.FirstSlot(t);

        if (s.OpeningFallback[TeamIndex(t)] == 0)
        {
            for (int slot = first; slot < first + 5; slot++)
            {
                if (s.Champions[slot].Has(ChampFlags.OpeningDone)) continue;
                for (int a = 0; a < 4; a++)
                {
                    if (OpeningAvailable(s, slot, a))
                    {
                        into.Add(new Command(CommandKind.OpeningPlay, (byte)slot, (byte)a, Target.None));
                    }
                }
            }

            if (into.Count > 0) return;   // must play if any is available (rule 4)
        }

        // Fallback (rule 5): the lowest remaining champion moves one hex, or stays if enclosed.
        for (int slot = first; slot < first + 5; slot++)
        {
            if (s.Champions[slot].Has(ChampFlags.OpeningDone)) continue;
            HexCoord at = s.Champions[slot].Pos;
            for (int k = 0; k < 6; k++)
            {
                int d = Board.FrameDirection(k, t);
                HexCoord to = at + Hex.Directions[d];
                if (Board.Playable(to) && !Occupied(s, to))
                {
                    into.Add(new Command(CommandKind.OpeningFallback, (byte)slot, 0, Target.Face(d)));
                }
            }

            if (into.Count == 0) into.Add(new Command(CommandKind.OpeningFallback, (byte)slot, 0, Target.Face(255)));
            return;
        }
    }

    /// <summary>
    /// Opening F2: available iff all three instructions can execute in order, each judged
    /// against the board the previous one left.
    /// </summary>
    public bool OpeningAvailable(in MatchState s, int slot, int ability)
    {
        Champion caster = s.Champions[slot];
        AbilityDef a = Def(caster).Abilities[ability];
        Span<HexCoord> pos = stackalloc HexCoord[10];
        Span<bool> onBoard = stackalloc bool[10];
        for (int i = 0; i < 10; i++)
        {
            pos[i] = s.Champions[i].Pos;
            onBoard[i] = s.Champions[i].OnBoard;
        }

        foreach (OpeningInstruction ins in a.Opening)
        {
            if (ins.Kind == InstructionKind.PlaceBeacon) continue;   // always executes
            int who = MatchState.Slot(caster.Team, ins.Role);
            HexCoord to = pos[who] + Board.Frame(Hex.Directions[ins.Direction], caster.Team);
            if (!Board.Playable(to) || IsSolidTower(to)) return false;
            for (int i = 0; i < 10; i++)
            {
                if (onBoard[i] && pos[i] == to) return false;
            }

            pos[who] = to;
        }

        return true;
    }

    private void ApplyOpeningPlay(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        Champion caster = s.Champions[cmd.Champion];
        AbilityDef a = Def(caster).Abilities[cmd.Ability];
        Log(log, EventKind.Opening, $"{Name(s, cmd.Champion)} plays {a.Name} in the opening.");

        foreach (OpeningInstruction ins in a.Opening)
        {
            int who = MatchState.Slot(caster.Team, ins.Role);
            if (ins.Kind == InstructionKind.Move)
            {
                HexCoord to = s.Champions[who].Pos + Board.Frame(Hex.Directions[ins.Direction], caster.Team);
                s.Champions[who].Pos = to;
                Log(log, EventKind.Opening, $"  {Name(s, who)} → {Fmt(to)}");
            }
            else
            {
                PlaceBeacon(ref s, caster.Team, s.Champions[who].Pos, (byte)ins.Sigil, log);
            }
        }

        s.Champions[cmd.Champion].Flags |= ChampFlags.OpeningDone;
        s.Active = NextOpeningTeam(s, caster.Team);
    }

    private void ApplyOpeningFallback(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        Team t = s.Champions[cmd.Champion].Team;
        s.OpeningFallback[TeamIndex(t)] = 1;
        if (cmd.Target.Facing != 255)
        {
            s.Champions[cmd.Champion].Pos += Hex.Directions[cmd.Target.Facing];
        }

        Log(log, EventKind.Opening, $"{Name(s, cmd.Champion)} takes the fallback → {Fmt(s.Champions[cmd.Champion].Pos)}");
        s.Champions[cmd.Champion].Flags |= ChampFlags.OpeningDone;

        // The fallback is one play: the same team finishes all its remaining champions.
        s.Active = OpeningComplete(s, t) ? NextOpeningTeam(s, t) : t;
    }

    private static Team NextOpeningTeam(in MatchState s, Team justPlayed)
    {
        Team other = MatchState.Other(justPlayed);
        return OpeningComplete(s, other) ? justPlayed : other;
    }

    private void PlaceBeacon(ref MatchState s, Team team, HexCoord at, byte sigil, List<GameEvent>? log)
    {
        int free = -1;
        for (int b = 0; b < 12; b++)
        {
            if (s.Beacons[b].Team != Team.None && s.Beacons[b].Pos == at)
            {
                free = b;   // replaces whatever beacon stands here (Opening edge case 7)
                break;
            }

            if (free < 0 && s.Beacons[b].Team == Team.None) free = b;
        }

        if (free < 0)
        {
            Log(log, EventKind.Beacon, "  Beacon slots full — placement ignored.");
            return;
        }

        s.Beacons[free] = new Beacon { Pos = at, Team = team, Sigil = sigil, Durability = (byte)Rules.BeaconDurability };
        Log(log, EventKind.Beacon, $"  {team} beacon {SigilName(sigil)} placed at {Fmt(at)}");
    }

    /// <summary>Roman-numeral sigil label.</summary>
    public static string SigilName(int sigil) => sigil switch { 0 => "I", 1 => "II", 2 => "III", _ => "-" };

    /// <summary>"(q,r)" label.</summary>
    public static string Fmt(HexCoord h) => $"({h.Q},{h.R})";
}
