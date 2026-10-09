using Augury.Sim.Content;

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

    /// <summary>
    /// Value setups (v2): a status on an enemy, or an enemy pinned against an obstacle, counts
    /// for the bonus damage our ready abilities could still land on it. Default on.
    /// </summary>
    public bool ComboAware { get; init; } = true;

    /// <summary>
    /// With <see cref="ComboAware"/>: also price setups in the evaluation. Off by default — measured
    /// 2026-10-08, it added nothing over the depth-3 search, which already sees the cash-in (D-054).
    /// </summary>
    public bool ValueSetups { get; init; }

    private bool Setups => ComboAware && ValueSetups;

    /// <summary>Ladder candidates kept for the two-ply search after a one-ply cut.</summary>
    public int Beam { get; init; } = 14;

    /// <inheritdoc/>
    public string Name => ComboAware ? "Heuristic (combo-aware)" : "Heuristic";

    /// <inheritdoc/>
    public Command Choose(in MatchState s, IReadOnlyList<Command> legal)
    {
        if (legal.Count == 1) return legal[0];
        Team me = s.Active;
        if (s.Phase == Phase.SpellPick) return PickSpell(s, legal);

        bool ladder = s.Phase == Phase.Ladder;
        var scored = new List<(Command Cmd, int Value)>(legal.Count);
        foreach (Command c in legal)
        {
            MatchState next = s;
            _game.Apply(ref next, c);
            scored.Add((c, Evaluation.Score(_game, next, me, Setups)));
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
            deep.Add((cmd, ComboAware ? FollowUpValue(next, me) : ReplyValue(next, me, replies)));
        }

        return Pick(deep);
    }

    /// <summary>
    /// Spell preference by role (v2). The evaluation can't price a spell that hasn't been
    /// cast, so the agent takes the first free spell on its role's list. ⚠ A guess, like the weights.
    /// Picks are hidden, so this deliberately ignores the opponent's choices.
    /// </summary>
    private static readonly string[][] SpellPreference =
    [
        ["Teleport", "Cleanse", "Flash", "Barrier"],          // Top
        ["Smite", "Flash", "Ignite", "Exhaust"],              // Jungle
        ["Ignite", "Flash", "Barrier", "Cleanse"],            // Mid
        ["Heal", "Barrier", "Flash", "Cleanse"],              // Bottom
        ["Exhaust", "Ignite", "Heal", "Flash"],               // Support
    ];

    private Command PickSpell(in MatchState s, IReadOnlyList<Command> legal)
    {
        Role role = s.Champions[legal[0].Champion].Role;
        foreach (string name in SpellPreference[(int)role])
        {
            foreach (Command c in legal)
            {
                if (_game.Content.Spells[c.Ability].Name == name) return c;
            }
        }

        return legal[0];
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
            return Evaluation.Score(_game, s, me, Setups);
        }

        buffer.Clear();
        _game.Legal(s, buffer);
        int worst = int.MaxValue;
        foreach (Command reply in buffer)
        {
            MatchState after = s;
            _game.Apply(ref after, reply);
            worst = Math.Min(worst, Evaluation.Score(_game, after, me, Setups));
        }

        return worst == int.MaxValue ? Evaluation.Score(_game, s, me, Setups) : worst;
    }

    /// <summary>Opponent replies examined at depth 3: the ones that hurt us most at one ply.</summary>
    public int ReplyBeam { get; init; } = 5;

    /// <summary>
    /// Three plies (combo-aware): the opponent answers with one of its most damaging replies,
    /// then we take our best follow-up. A setup (root, mark, wall) is chosen because the
    /// follow-up that cashes it in is seen — unless the opponent's answer removes it.
    /// </summary>
    private int FollowUpValue(in MatchState s, Team me)
    {
        if (s.Phase is not (Phase.Ladder or Phase.LastWord) || s.Active == me) return BestOwn(s, me);

        var replies = new List<Command>();
        _game.Legal(s, replies);
        var ranked = new List<(MatchState After, int Value)>(replies.Count);
        foreach (Command reply in replies)
        {
            MatchState after = s;
            _game.Apply(ref after, reply);
            ranked.Add((after, Evaluation.Score(_game, after, me, Setups)));
        }

        int worst = int.MaxValue;
        foreach ((MatchState after, int _) in ranked.OrderBy(x => x.Value).Take(ReplyBeam))
        {
            worst = Math.Min(worst, BestOwn(after, me));
        }

        return worst == int.MaxValue ? Evaluation.Score(_game, s, me, Setups) : worst;
    }

    /// <summary>Our best one-ply continuation if it is our move on the ladder, else the static value.</summary>
    private int BestOwn(in MatchState s, Team me)
    {
        int stay = Evaluation.Score(_game, s, me, Setups);
        if (s.Phase is not (Phase.Ladder or Phase.LastWord) || s.Active != me) return stay;
        var mine = new List<Command>();
        _game.Legal(s, mine);
        int best = int.MinValue;
        foreach (Command c in mine)
        {
            MatchState after = s;
            _game.Apply(ref after, c);
            best = Math.Max(best, Evaluation.Score(_game, after, me, Setups));
        }

        return best == int.MinValue ? stay : best;
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
    public static int Score(Game game, in MatchState s, Team me, bool combo = false) =>
        Side(game, s, me, combo) - Side(game, s, MatchState.Other(me), combo);

    private static int Side(Game game, in MatchState s, Team t, bool combo)
    {
        if (s.Phase == Phase.MatchOver)
        {
            return s.Winner == t ? 1_000_000 : 0;
        }

        int v = 0;

        // Structures: owned towers score every round; progress on towers we don't own.
        for (int i = 0; i < 5; i++)
        {
            Tower tower = s.Towers[i];
            if (tower.Owner == t) v += 70;
            else v += (game.Rules.TowerHp - tower.Hp) * 3;
        }

        // Damage to the enemy nexus is the race itself (v2: it replaced points, which were
        // weighted 100 each). It is permanent, so it counts whether or not the gate is open.
        Team enemy = MatchState.Other(t);
        v += (game.Rules.NexusHp - s.NexusHp[enemy == Team.A ? 0 : 1]) * 100;
        if (Game.NexusVulnerable(s, enemy)) v += 200;

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
                // Will die at the death check: concede the nexus damage now.
                v -= game.Rules.KillSiege * 100 + 60;
                continue;
            }

            v += Math.Min(c.Hp, game.MaxHp(c)) * 5 + c.Shield * 2;
            v -= c.PoisonAmount * c.PoisonRounds * 3;

            // v2 statuses. ⚠ Guessed weights: a status is worth roughly the damage it threatens.
            v -= c.BurnAmount * c.BurnRounds * 4;
            v -= c.Mark * 4;
            if (c.Rooted) v -= 12;
            if (c.ExhaustHalves > 0) v -= 15;
            if (c.Unstoppable) v += 6;
            v -= c.WoundRounds * 4;

            // Pressure: stand close to towers we don't own.
            int nearest = 99;
            for (int i = 0; i < 5; i++)
            {
                if (s.Towers[i].Owner != t) nearest = Math.Min(nearest, HexCoord.Distance(c.Pos, s.Towers[i].Pos));
            }

            if (nearest < 99) v -= nearest * 6;
        }

        if (combo) v += SetupValue(game, s, t);
        return v;
    }

    /// <summary>
    /// What team <paramref name="t"/>'s setups are worth: for each of our champions, the bonus
    /// damage of its best ready payoff on any enemy carrying the status it keys on (or pinned so
    /// a push would slam), at a third less than HP value.
    /// ⚠ Weights are guesses, like the rest of the evaluation.
    /// </summary>
    private static int SetupValue(Game game, in MatchState s, Team t)
    {
        int v = 0;
        int mine = MatchState.FirstSlot(t), theirs = MatchState.FirstSlot(MatchState.Other(t));
        for (int m = mine; m < mine + 5; m++)
        {
            Champion c = s.Champions[m];
            if (!c.OnBoard || c.Hp <= 0) continue;

            // One action per champion: its single best payoff on any enemy, not one per enemy.
            // Otherwise cashing in (which puts the ability on cooldown) would cost more
            // potential than it gains, and the AI hoards setups instead of using them.
            int best = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                AbilityDef? a = game.Kit(c, slot);
                if (a is null || c.Cooldowns[slot] > 0) continue;
                int reach = a.IsFree ? game.AbilityRange(c, a) : 3;
                for (int e = theirs; e < theirs + 5; e++)
                {
                    Champion foe = s.Champions[e];
                    if (!foe.OnBoard || foe.Hp <= 0 || HexCoord.Distance(c.Pos, foe.Pos) > reach + 1) continue;   // one step of slack
                    foreach (EffectDef ef in a.Effects)
                    {
                        int bonus = 0;
                        StatusKind wants = game.Def(c).Wants;
                        if (ef.Kind == EffectKind.Damage && wants != StatusKind.None && Has(foe, wants))
                        {
                            // The champion's trait (D-056): every hit on a target carrying what it wants.
                            int raw = ef.Power > 0 ? (int)Arith.FloorDiv((long)game.Rules.AbilityBase * ef.Power * game.Pow(c), 1_000_000) : ef.Amount;
                            int hit = Math.Max(1, raw - game.Armour(foe));
                            bonus = Math.Max(hit * (game.Rules.WantBonus - 1000) / 1000, game.Rules.WantFloor) * game.Rules.PayoffScale / 1000;
                        }
                        else if (ef.Kind == EffectKind.Displace && ef.Slam > 0 && ef.Amount > 0 && !foe.Unstoppable
                                 && (foe.Rooted || Pinned(game, s, foe.Pos)))
                        {
                            bonus = ef.Slam;
                        }

                        best = Math.Max(best, bonus);
                    }
                }
            }

            v += best * 5 / 3;   // below the value of the damage itself, so cashing in always pays
        }

        return v;
    }

    private static bool Has(in Champion c, StatusKind k) => k switch
    {
        StatusKind.Rooted => c.Rooted,
        StatusKind.Burning => c.Burning,
        StatusKind.Poisoned => c.PoisonRounds > 0,
        StatusKind.Marked => c.Mark > 0,
        StatusKind.Exhausted => c.ExhaustHalves > 0,
        _ => false,
    };

    /// <summary>Next to the board's edge, a solid tower or a wall: a push there is likely to slam.</summary>
    private static bool Pinned(Game game, in MatchState s, HexCoord at)
    {
        for (int d = 0; d < 6; d++)
        {
            HexCoord n = at + Hex.Directions[d];
            if (!Board.Playable(n) || Game.WallAt(s, n) >= 0 || (game.Rules.TowersBlock && Board.TowerAt(n) >= 0)) return true;
        }

        return false;
    }
}

/// <summary>
/// A drafter that builds around synergy groups (<c>assets/data/synergies.json</c>): each pick
/// adds the most synergy pairs to its own team, then keeps the most group members still
/// reachable for its open roles. Ties, and an optional share of noisy picks, break by a fixed
/// seed. Draft commands only.
/// </summary>
public sealed class SynergyDrafter(Game game, uint seed = 0) : IAgent
{
    private readonly Game _game = game;
    private uint _rng = seed;

    /// <summary>
    /// How many synergy pairs to aim for: <see cref="int.MaxValue"/> builds the best team it can
    /// (a "great" draft), 0 avoids every pair (a "tragic" one), 2 stops at a pair or two ("decent").
    /// </summary>
    public int TargetPairs { get; init; } = int.MaxValue;

    /// <summary>Permille chance of a uniformly random legal pick instead of the best one.</summary>
    public int NoisePermille { get; init; }

    /// <inheritdoc/>
    public string Name => TargetPairs == int.MaxValue ? "Synergy drafter" : $"Synergy drafter (aims for {TargetPairs} pairs)";

    /// <inheritdoc/>
    public Command Choose(in MatchState s, IReadOnlyList<Command> legal)
    {
        if (legal.Count == 1 || s.Phase != Phase.Draft) return legal[0];
        if (NoisePermille > 0 && Next() % 1000 < (uint)NoisePermille) return legal[(int)(Next() % (uint)legal.Count)];

        ContentDb db = _game.Content;
        int first = MatchState.FirstSlot(s.Active);
        var mine = new List<int>();
        var open = new HashSet<Content.Role>();
        var taken = new HashSet<int>();
        for (int i = 0; i < 10; i++)
        {
            if (s.Champions[i].Def != 255) taken.Add(s.Champions[i].Def);
        }

        for (int i = first; i < first + 5; i++)
        {
            if (s.Champions[i].Def != 255) mine.Add(s.Champions[i].Def);
            else open.Add(s.Champions[i].Role);
        }

        int baseScore = db.SynergyScore(mine);
        var best = new List<Command>();
        int bestValue = int.MinValue;
        foreach (Command c in legal)
        {
            int def = c.Ability;
            int gain = db.SynergyScore(mine.Append(def)) - baseScore;

            // Potential: partners of this champion still free for the roles we have left.
            var rolesAfter = new HashSet<Content.Role>(open);
            rolesAfter.Remove(db.Champions[def].Role);
            int potential = 0;
            foreach (int other in db.PartnersOf(def))
            {
                if (!taken.Contains(other) && rolesAfter.Contains(db.Champions[other].Role)) potential++;
            }

            int after = baseScore + gain;
            int value = TargetPairs == int.MaxValue
                ? gain * 10 + potential
                : after <= TargetPairs
                    ? gain * 10 + (after < TargetPairs ? potential : -potential)   // build up to the target, then stay clear
                    : -(after - TargetPairs) * 10 - potential;
            if (value > bestValue)
            {
                bestValue = value;
                best.Clear();
            }

            if (value == bestValue) best.Add(c);
        }

        return best[(int)(Next() % (uint)best.Count)];
    }

    private uint Next()
    {
        _rng = _rng * 1664525u + 1013904223u;
        return _rng >> 8;
    }
}
