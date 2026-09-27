namespace Augury.Sim.AI;

/// <summary>Anything that chooses a command for the active team.</summary>
public interface IAgent
{
    /// <summary>Display name.</summary>
    string Name { get; }

    /// <summary>Chooses one of <paramref name="legal"/>, which is never empty.</summary>
    Command Choose(in MatchState s, IReadOnlyList<Command> legal);
}

/// <summary>Uniform random legal play from a fixed seed. The baseline every real agent must beat.</summary>
public sealed class RandomAgent(uint seed) : IAgent
{
    private uint _s = seed;

    /// <inheritdoc/>
    public string Name => "Random";

    /// <inheritdoc/>
    public Command Choose(in MatchState s, IReadOnlyList<Command> legal)
    {
        _s = _s * 1664525u + 1013904223u;
        return legal[(int)((_s >> 8) % (uint)legal.Count)];
    }
}

/// <summary>
/// The MVP opponent: a hand-written evaluation with a shallow search. Greedy in the draft,
/// opening and basic phase; on the ladder it looks one reply ahead, which is the minimum
/// needed to respect the ladder's central bargain — a big opener invites a big answer.
/// </summary>
/// <remarks>
/// Deterministic: ties break toward the earliest legal command, and no randomness is used.
/// The weights are ⚠ guesses meant to be replaced by self-play tuning.
/// </remarks>
public sealed class HeuristicAgent(Game game, uint seed = 0) : IAgent
{
    private readonly Game _game = game;
    private uint _rng = seed;

    /// <summary>
    /// Permille chance of choosing among the top three instead of the best. A harness device
    /// so self-play samples different games; 0 (the default) is fully greedy.
    /// </summary>
    public int NoisePermille { get; init; }

    /// <summary>Ladder candidates kept for the two-ply search after a one-ply cut.</summary>
    public int Beam { get; init; } = 14;

    /// <inheritdoc/>
    public string Name => "Heuristic";

    /// <inheritdoc/>
    public Command Choose(in MatchState s, IReadOnlyList<Command> legal)
    {
        if (legal.Count == 1) return legal[0];
        Team me = s.Active;

        bool ladder = s.Phase == Phase.Ladder;
        var scored = new List<(Command Cmd, int Value)>(legal.Count);
        foreach (Command c in legal)
        {
            MatchState next = s;
            _game.Apply(ref next, c);
            scored.Add((c, Evaluation.Score(_game, next, me)));
        }

        if (!ladder) return Pick(scored);

        // Two-ply on the ladder: assume the opponent answers with their greedy best.
        var candidates = scored.OrderByDescending(x => x.Value).Take(Beam).ToList();
        var replies = new List<Command>();
        var deep = new List<(Command Cmd, int Value)>(candidates.Count);
        foreach ((Command cmd, int _) in candidates)
        {
            MatchState next = s;
            _game.Apply(ref next, cmd);
            deep.Add((cmd, ReplyValue(next, me, replies)));
        }

        return Pick(deep);
    }

    private Command Pick(List<(Command Cmd, int Value)> scored)
    {
        if (NoisePermille > 0 && scored.Count > 1)
        {
            _rng = _rng * 1664525u + 1013904223u;
            if ((_rng >> 8) % 1000 < (uint)NoisePermille)
            {
                var top = scored.OrderByDescending(x => x.Value).Take(3).ToList();
                _rng = _rng * 1664525u + 1013904223u;
                return top[(int)((_rng >> 8) % (uint)top.Count)].Cmd;
            }
        }

        return Best(scored);
    }

    /// <summary>Our evaluation after the opponent's best immediate reply, if it is their move.</summary>
    private int ReplyValue(in MatchState s, Team me, List<Command> buffer)
    {
        if (s.Phase is not (Phase.Ladder or Phase.LastWord) || s.Active == me)
        {
            return Evaluation.Score(_game, s, me);
        }

        buffer.Clear();
        _game.Legal(s, buffer);
        int worst = int.MaxValue;
        foreach (Command reply in buffer)
        {
            MatchState after = s;
            _game.Apply(ref after, reply);
            worst = Math.Min(worst, Evaluation.Score(_game, after, me));
        }

        return worst == int.MaxValue ? Evaluation.Score(_game, s, me) : worst;
    }

    private static Command Best(List<(Command Cmd, int Value)> scored)
    {
        Command best = scored[0].Cmd;
        int value = scored[0].Value;
        foreach ((Command c, int v) in scored)
        {
            if (v > value)
            {
                value = v;
                best = c;
            }
        }

        return best;
    }
}

/// <summary>
/// Static evaluation. Antisymmetric by construction: <c>Score(s, A) == −Score(s, B)</c>,
/// so the opponent minimising our score is the opponent maximising theirs.
/// </summary>
public static class Evaluation
{
    /// <summary>Value of <paramref name="s"/> for <paramref name="me"/>. Higher is better.</summary>
    public static int Score(Game game, in MatchState s, Team me) =>
        Side(game, s, me) - Side(game, s, MatchState.Other(me));

    private static int Side(Game game, in MatchState s, Team t)
    {
        if (s.Phase == Phase.MatchOver)
        {
            return s.Winner == t ? 1_000_000 : 0;
        }

        int idx = t == Team.A ? 0 : 1;
        int v = s.Score[idx] * 100;

        // Structures: owned towers score every round; progress on towers we don't own.
        for (int i = 0; i < 5; i++)
        {
            Tower tower = s.Towers[i];
            if (tower.Owner == t) v += 70;
            else v += (game.Rules.TowerHp - tower.Hp) * 3;
        }

        Team enemy = MatchState.Other(t);
        if (Game.NexusVulnerable(s, enemy))
        {
            v += 200 + (game.Rules.NexusHp - s.NexusHp[enemy == Team.A ? 0 : 1]) * 12;
        }

        int first = MatchState.FirstSlot(t);
        for (int slot = first; slot < first + 5; slot++)
        {
            Champion c = s.Champions[slot];
            if (c.Presence == Presence.Dead)
            {
                v -= 90;
                continue;
            }

            if (c.Presence == Presence.InSpawn)
            {
                v -= 40;
                continue;
            }

            if (c.Hp <= 0)
            {
                // Will die at the death check: concede the kill points now.
                v -= game.Rules.KillPoints * 100 + 60;
                continue;
            }

            v += Math.Min(c.Hp, game.MaxHp(c)) * 5 + c.Shield * 2;
            v -= c.PoisonAmount * c.PoisonRounds * 3;

            // Pressure: stand close to towers we don't own.
            int nearest = 99;
            for (int i = 0; i < 5; i++)
            {
                if (s.Towers[i].Owner != t) nearest = Math.Min(nearest, HexCoord.Distance(c.Pos, s.Towers[i].Pos));
            }

            if (nearest < 99) v -= nearest * 6;
        }

        return v;
    }
}
