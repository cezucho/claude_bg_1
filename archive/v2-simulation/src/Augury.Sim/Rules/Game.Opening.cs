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

    // ───────────────────────────── summoner spells (v2) ─────────────────────────────

    /// <summary>True while a team has a champion with a spell slot and no spell.</summary>
    private bool NeedsSpells(in MatchState s, Team t)
    {
        if (Content.Spells.Count == 0) return false;
        int first = MatchState.FirstSlot(t);
        for (int i = first; i < first + 5; i++)
        {
            if (s.Champions[i].Def != 255 && Def(s.Champions[i]).HasSpellSlot && s.Champions[i].Spell == 255) return true;
        }

        return false;
    }

    /// <summary>
    /// Spells for the active team's next champion without one, in role order: every spell its
    /// team hasn't already taken (no duplicates within a team).
    /// </summary>
    private void LegalSpellPick(in MatchState s, List<Command> into)
    {
        int first = MatchState.FirstSlot(s.Active);
        for (int slot = first; slot < first + 5; slot++)
        {
            Champion c = s.Champions[slot];
            if (!Def(c).HasSpellSlot || c.Spell != 255) continue;
            for (int sp = 0; sp < Content.Spells.Count; sp++)
            {
                bool taken = false;
                for (int j = first; j < first + 5; j++) taken |= s.Champions[j].Spell == sp;
                if (!taken) into.Add(new Command(CommandKind.SpellPick, (byte)slot, (byte)sp, Target.None));
            }

            return;
        }
    }

    private void ApplySpellPick(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        s.Champions[cmd.Champion].Spell = cmd.Ability;
        // The pick is hidden from the other team until the opening, so the log only says one was made.
        Log(log, EventKind.Draft, $"{s.Active} chooses a summoner spell for its {s.Champions[cmd.Champion].Role}.");
    }

    private void RevealSpells(in MatchState s, List<GameEvent>? log)
    {
        if (log is null) return;
        foreach (Team t in new[] { Team.A, Team.B })
        {
            int first = MatchState.FirstSlot(t);
            var names = new List<string>();
            for (int i = first; i < first + 5; i++)
            {
                if (s.Champions[i].Spell < Content.Spells.Count) names.Add($"{Def(s.Champions[i]).Name} {Content.Spells[s.Champions[i].Spell].Name}");
            }

            Log(log, EventKind.Draft, $"{t} spells revealed: {string.Join(", ", names)}.");
        }
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
            for (int k = 0; k < 6 && !s.Champions[slot].Rooted; k++)
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
    /// against the board the previous one left. Moves are strict: off the board, onto a
    /// champion, tower or wall, or moving a rooted champion, makes the play unavailable.
    /// Casts never do: with nothing to hit they fizzle (owner, 2026-10-04).
    /// </summary>
    public bool OpeningAvailable(in MatchState s, int slot, int ability)
    {
        Champion caster = s.Champions[slot];
        IReadOnlyList<OpeningInstruction> instructions = OpeningOf(caster, ability);
        Span<HexCoord> pos = stackalloc HexCoord[10];
        Span<bool> onBoard = stackalloc bool[10];
        for (int i = 0; i < 10; i++)
        {
            pos[i] = s.Champions[i].Pos;
            onBoard[i] = s.Champions[i].OnBoard;
        }

        foreach (OpeningInstruction ins in instructions)
        {
            if (ins.Kind != InstructionKind.Move) continue;   // beacons always execute; casts may fizzle
            int who = MatchState.Slot(caster.Team, ins.Role);
            if (s.Champions[who].Rooted) return false;
            HexCoord to = pos[who] + Board.Frame(Hex.Directions[ins.Direction], caster.Team);
            if (!Board.Playable(to) || IsSolidTower(to) || WallAt(s, to) >= 0) return false;
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
        Log(log, EventKind.Opening, $"{Name(s, cmd.Champion)} plays {OpeningName(caster, cmd.Ability)} in the opening.");

        foreach (OpeningInstruction ins in OpeningOf(caster, cmd.Ability))
        {
            int who = MatchState.Slot(caster.Team, ins.Role);
            switch (ins.Kind)
            {
                case InstructionKind.Move:
                    HexCoord to = s.Champions[who].Pos + Board.Frame(Hex.Directions[ins.Direction], caster.Team);
                    s.Champions[who].Pos = to;
                    Log(log, EventKind.Opening, $"  {Name(s, who)} → {Fmt(to)}");
                    break;
                case InstructionKind.PlaceBeacon:
                    PlaceBeacon(ref s, caster.Team, s.Champions[who].Pos, (byte)ins.Sigil, log);
                    break;
                case InstructionKind.Cast:
                    CastInOpening(ref s, who, ins.Slot, log);
                    break;
            }
        }

        s.Champions[cmd.Champion].Flags |= ChampFlags.OpeningDone;
        s.Active = NextOpeningTeam(s, caster.Team);
    }

    /// <summary>
    /// An opening cast (v2): the champion fires its ability, aimed by D-043's rules. With
    /// nothing to hit it fizzles but still goes on cooldown; already on cooldown, nothing
    /// happens. Opening damage can't kill (D-042).
    /// </summary>
    private void CastInOpening(ref MatchState s, int who, int slot, List<GameEvent>? log)
    {
        Champion c = s.Champions[who];
        AbilityDef? a = Kit(c, slot);
        if (a is null) return;
        if (c.Cooldowns[slot] > 0)
        {
            Log(log, EventKind.Opening, $"  {Name(s, who)}'s {a.Name} is already on cooldown — nothing happens.");
            return;
        }

        // Every opening cast costs round 1 at least, even for a cooldown-0 ability (D-044).
        int cooldown = Math.Max(1, a.Cooldown);
        Target? aim = OpeningAim(s, who, slot);
        if (aim is null)
        {
            s.Champions[who].Cooldowns[slot] = (byte)cooldown;
            Log(log, EventKind.Opening, $"  {Name(s, who)} casts {a.Name} — nothing in reach, it fizzles (on cooldown {cooldown}).");
            return;
        }

        ResolveOne(ref s, who, slot, aim.Value, log);
        s.Champions[who].Cooldowns[slot] = (byte)cooldown;
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
