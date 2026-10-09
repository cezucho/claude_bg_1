using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Per-champion statistics from inside the match, collected through <see cref="IMatchObserver"/>
/// (who damaged, healed or killed whom) and from the commands each champion was given. Answers
/// "why" a champion wins or loses, which a game win rate alone can't: a champion whose team loses
/// even with strong teammates is weak itself.
/// </summary>
internal sealed class ChampionStats
{
    /// <summary>One match's tallies per slot; reported to by the simulation.</summary>
    public sealed class MatchTally : IMatchObserver
    {
        private readonly Champion[] _teams = new Champion[10];
        private readonly int[] _lastHitBy = Enumerable.Repeat(-1, 10).ToArray();

        public readonly int[] Dealt = new int[10], Taken = new int[10], Healed = new int[10], Shields = new int[10];
        public readonly int[] Structure = new int[10], Kills = new int[10], Deaths = new int[10];
        public readonly int[] Payoffs = new int[10], Slams = new int[10], Abilities = new int[10], BasicAttacks = new int[10], Moves = new int[10];

        /// <summary>Teams of each slot (fixed for the match).</summary>
        public void SetTeams(in MatchState s)
        {
            for (int i = 0; i < 10; i++) _teams[i] = s.Champions[i];
        }

        /// <summary>Counts what a command asked of which champion.</summary>
        public void Command(in Command c)
        {
            switch (c.Kind)
            {
                case CommandKind.Ability:
                    Abilities[c.Champion]++;
                    if (c.IsChain) Abilities[c.Champion2]++;
                    break;
                case CommandKind.BasicAttack: BasicAttacks[c.Champion]++; break;
                case CommandKind.BasicMove: Moves[c.Champion]++; break;
            }
        }

        public void ChampionDamaged(int source, int target, int hpLost, int absorbed)
        {
            Taken[target] += hpLost;
            if (source < 0 || _teams[source].Team == _teams[target].Team) return;
            Dealt[source] += hpLost + absorbed;
            _lastHitBy[target] = source;
        }

        public void ChampionHealed(int source, int target, int amount)
        {
            if (source >= 0) Healed[source] += amount;
        }

        public void Shielded(int source, int target, int amount) => Shields[source] += amount;

        public void StructureDamaged(int source, int amount, bool nexus)
        {
            if (source >= 0) Structure[source] += amount;
        }

        public void Payoff(int source, int target) => Payoffs[source]++;

        public void Slam(int source, int target) => Slams[source]++;

        public void Died(int victim)
        {
            Deaths[victim]++;
            if (_lastHitBy[victim] >= 0) Kills[_lastHitBy[victim]]++;   // the last enemy to damage it
            _lastHitBy[victim] = -1;
        }
    }

    private sealed class Agg
    {
        public int Games, Wins;
        public long Dealt, Taken, Healed, Shields, Structure, Kills, Deaths, Payoffs, Slams, Abilities, BasicAttacks, Moves;
        public readonly Dictionary<string, (int Games, int Wins)> Lane = new();
    }

    private readonly Dictionary<string, Agg> _agg = new();
    private readonly List<(string[] TeamA, string[] TeamB, Team Winner)> _matches = new();

    /// <summary>Adds a finished match.</summary>
    public void Add(Game game, in MatchState s, MatchTally t)
    {
        var names = new string[10];
        for (int i = 0; i < 10; i++) names[i] = game.Def(s.Champions[i]).Name;
        _matches.Add((names[..5], names[5..], s.Winner));
        for (int i = 0; i < 10; i++)
        {
            Agg a = _agg.TryGetValue(names[i], out Agg? x) ? x : _agg[names[i]] = new Agg();
            bool won = s.Winner == s.Champions[i].Team;
            a.Games++;
            if (won) a.Wins++;
            a.Dealt += t.Dealt[i];
            a.Taken += t.Taken[i];
            a.Healed += t.Healed[i];
            a.Shields += t.Shields[i];
            a.Structure += t.Structure[i];
            a.Kills += t.Kills[i];
            a.Deaths += t.Deaths[i];
            a.Payoffs += t.Payoffs[i];
            a.Slams += t.Slams[i];
            a.Abilities += t.Abilities[i];
            a.BasicAttacks += t.BasicAttacks[i];
            a.Moves += t.Moves[i];

            // Its lane: the enemy drafted into the same role.
            string rival = names[i < 5 ? i + 5 : i - 5];
            var (g, w) = a.Lane.GetValueOrDefault(rival);
            a.Lane[rival] = (g + 1, w + (won ? 1 : 0));
        }
    }

    /// <summary>Prints the per-champion tables.</summary>
    public void Report()
    {
        var wr = _agg.ToDictionary(kv => kv.Key, kv => kv.Value.Wins / (double)kv.Value.Games);

        // Teammates' strength: in each of its matches, the average overall win rate of its four
        // teammates. A champion whose team wins much less than its teammates usually do is
        // dragging them down — the owner's test of a truly weak champion.
        var mates = _agg.Keys.ToDictionary(k => k, _ => (Sum: 0.0, N: 0));
        foreach (var (ta, tb, _) in _matches)
        {
            foreach (string[] team in new[] { ta, tb })
            {
                foreach (string c in team)
                {
                    double avg = team.Where(o => o != c).Average(o => wr[o]);
                    var (sum, n) = mates[c];
                    mates[c] = (sum + avg, n + 1);
                }
            }
        }

        Console.WriteLine("  CHAMPIONS IN THE MATCH (per match drafted; damage = HP + shield removed from enemy champions)");
        Console.WriteLine($"    {"champion",-9} {"win",5} {"mates",6} {"drag",6} │ {"dealt",6} {"taken",6} {"heal",5} {"shld",5} {"tower",6} │ {"K",4} {"D",4} │ {"payof",5} {"slam",5} │ {"abil",5} {"atk",4} {"move",5}");
        foreach (var (name, a) in _agg.OrderByDescending(kv => wr[kv.Key]))
        {
            double g = a.Games;
            double m = mates[name].Sum / mates[name].N;
            double drag = wr[name] - m;
            Console.WriteLine($"    {name,-9} {wr[name],5:P0} {m,6:P0} {drag * 100,5:+0;-0}  │ {a.Dealt / g,6:F1} {a.Taken / g,6:F1} {a.Healed / g,5:F1} {a.Shields / g,5:F1} {a.Structure / g,6:F1} │ {a.Kills / g,4:F1} {a.Deaths / g,4:F1} │ {a.Payoffs / g,5:F1} {a.Slams / g,5:F1} │ {a.Abilities / g,5:F1} {a.BasicAttacks / g,4:F1} {a.Moves / g,5:F1}");
        }

        Console.WriteLine("    win = its team's win rate · mates = its teammates' usual win rate · drag = the difference, in points");
        Console.WriteLine("    (strongly negative: its team loses even with good teammates)");
        Console.WriteLine();
        Console.WriteLine("  LANE — win rate against the enemy in the same role");
        foreach (var (name, a) in _agg.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"    {name,-9} " + string.Join("   ", a.Lane.OrderBy(l => l.Key).Select(l => $"vs {l.Key} {l.Value.Wins / (double)l.Value.Games,4:P0} ({l.Value.Games})")));
        }

        Console.WriteLine();
    }
}
