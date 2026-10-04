using System.Diagnostics;
using Augury.Sim;
using Augury.Sim.AI;

namespace Augury.Tools;

/// <summary>
/// AI-vs-AI matches, measured against the acceptance criteria the GDDs wrote as harness
/// assertions. Usage: <c>selfplay [matches] [a-agent] [b-agent]</c>, agents
/// <c>heuristic</c> or <c>random</c>.
/// </summary>
public static class SelfPlay
{
    private sealed class Stats
    {
        public int Matches, WinsA, WinsB, Draws, Nexus, Siege, Cap, Comebacks;
        public long Rounds, Halves, HalvesOpened, HalvesByPass, Resolutions, Chains, Deaths, Captures;
        public long BasicMoves, BasicAttacks, OpeningFallbackTeams, Decisions, PlayerDecisions, NexusOpenDecisions, MatchesNexusOpened, NexusHits, MatchesNexusHit;
        public long PatternChecks, PatternLive, GapSamples, GapTotal;
        public readonly long[] ResolvedByTier = new long[5];
        public double AiMsTotal, AiMsMax;
        public long AiCalls;
        public readonly List<int> RoundCounts = new();
        public readonly Dictionary<string, (int Picks, int Wins)> Champs = new();
    }

    public static void Run(string[] args)
    {
        // Positional: matches, a-agent, b-agent. Any "key=value" overrides a rules_config.json field.
        var positional = args.Skip(1).Where(a => !a.Contains('=')).ToList();
        int n = positional.Count > 0 ? int.Parse(positional[0]) : 100;
        string aName = positional.Count > 1 ? positional[1] : "heuristic";
        string bName = positional.Count > 2 ? positional[2] : "heuristic";
        Game baseline = Game.LoadDefault();
        Game game = new(baseline.Content, Override(baseline.Rules, args.Where(a => a.Contains('='))));
        Board.Use(Augury.Sim.BoardLayout.Load(game.Rules.Board));
        var stats = new Stats();
        var sw = Stopwatch.StartNew();

        for (int m = 0; m < n; m++)
        {
            IAgent a = Make(aName, game, (uint)(1000 + m));
            IAgent b = Make(bName, game, (uint)(5000 + m));
            PlayOne(game, a, b, stats, draftSeed: (uint)(77 + m));
        }

        Report(game, stats, aName, bName, sw.Elapsed.TotalSeconds);
    }

    private static RulesConfig Override(RulesConfig rules, IEnumerable<string> pairs)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToNode(rules)!.AsObject();
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=', 2);
            string key = json.Select(p => p.Key).FirstOrDefault(k => k.Equals(kv[0], StringComparison.OrdinalIgnoreCase))
                         ?? throw new ArgumentException($"Unknown rule '{kv[0]}'.");
            json[key] = bool.TryParse(kv[1], out bool b) ? b : int.TryParse(kv[1], out int i) ? i : kv[1];
        }

        return System.Text.Json.JsonSerializer.Deserialize<RulesConfig>(json)!;
    }

    private static IAgent Make(string name, Game game, uint seed) => name switch
    {
        "random" => new RandomAgent(seed),
        "greedy" => new HeuristicAgent(game),
        _ => new HeuristicAgent(game, seed) { NoisePermille = 150 },
    };

    private static void PlayOne(Game game, IAgent a, IAgent b, Stats st, uint draftSeed)
    {
        // A seeded random draft so matches differ; the roster decides whether it matters.
        var draft = new RandomAgent(draftSeed);
        MatchState s = game.NewMatch();
        var log = new List<GameEvent>();
        var legal = new List<Command>();
        bool opened = false;
        int maxLeadA = 0, maxLeadB = 0;

        while (s.Phase != Phase.MatchOver)
        {
            legal.Clear();
            game.Legal(s, legal);
            Command c;
            if (s.Phase == Phase.Draft)
            {
                c = draft.Choose(s, legal);
            }
            else
            {
                IAgent agent = s.Active == Team.A ? a : b;
                long t0 = Stopwatch.GetTimestamp();
                c = agent.Choose(s, legal);
                double ms = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
                if (agent is HeuristicAgent)
                {
                    st.AiMsTotal += ms;
                    st.AiMsMax = Math.Max(st.AiMsMax, ms);
                    st.AiCalls++;
                }
            }

            if (s.Phase == Phase.Ladder && legal.Count > 1)
            {
                // Pattern applicability: of the ready tier-3/4 abilities the side to act holds,
                // how many have something to hit right now.
                int first = MatchState.FirstSlot(s.Active);
                for (int slot = first; slot < first + 5; slot++)
                {
                    Champion ch = s.Champions[slot];
                    if (!ch.OnBoard || ch.Has(ChampFlags.Acted)) continue;
                    for (int ab = 0; ab < 4; ab++)
                    {
                        if (ch.Cooldowns[ab] > 0 || game.Def(ch).Abilities[ab].Initiative < 3) continue;
                        st.PatternChecks++;
                        if (game.AbilityTargets(s, slot, ab).Count > 0) st.PatternLive++;
                    }
                }
            }

            int roundBefore = s.Round;
            Phase phaseBefore = s.Phase;
            game.Apply(ref s, c, log);
            if (phaseBefore == Phase.Opening && s.Phase != Phase.Opening)
            {
                // How close the teams stand when the opening ends: each champion's distance
                // to its nearest enemy.
                for (int i = 0; i < 10; i++)
                {
                    int nearest = 99;
                    for (int j = 0; j < 10; j++)
                    {
                        if (s.Champions[j].Team != s.Champions[i].Team) nearest = Math.Min(nearest, HexCoord.Distance(s.Champions[i].Pos, s.Champions[j].Pos));
                    }

                    st.GapTotal += nearest;
                    st.GapSamples++;
                }
            }
            st.Decisions++;
            if (s.Phase != Phase.Draft && legal.Count > 1) st.PlayerDecisions++;
            if (s.Round != roundBefore || s.Phase == Phase.MatchOver)
            {
                // A round closed: who leads the nexus race, and by how much.
                int lead = s.NexusHp[0] - s.NexusHp[1];
                maxLeadA = Math.Max(maxLeadA, lead);
                maxLeadB = Math.Max(maxLeadB, -lead);
            }
            if (Game.NexusVulnerable(s, Team.A) || Game.NexusVulnerable(s, Team.B))
            {
                st.NexusOpenDecisions++;
                opened = true;
            }
        }

        if (opened) st.MatchesNexusOpened++;
        int hits = log.Count(e => e.Kind == EventKind.Structure && e.Text.Contains("NEXUS"));
        st.NexusHits += hits;
        if (hits > 0) st.MatchesNexusHit++;

        st.Matches++;
        for (int slot = 0; slot < 10; slot++)
        {
            Champion ch = s.Champions[slot];
            string name = game.Def(ch).Name;
            var (p, w) = st.Champs.GetValueOrDefault(name);
            st.Champs[name] = (p + 1, w + (s.Winner == ch.Team ? 1 : 0));
        }

        st.Rounds += s.Round;
        st.RoundCounts.Add(s.Round);
        switch (s.Winner)
        {
            case Team.A: st.WinsA++; break;
            case Team.B: st.WinsB++; break;
            default: st.Draws++; break;
        }

        switch (s.EndReason)
        {
            case EndReason.Nexus: st.Nexus++; break;
            case EndReason.Siege: st.Siege++; break;
            case EndReason.RoundCap: st.Cap++; break;
        }

        // A comeback: the winner trailed the nexus race by at least 5 at some round close.
        if ((s.Winner == Team.A && maxLeadB >= 5) || (s.Winner == Team.B && maxLeadA >= 5)) st.Comebacks++;

        foreach (GameEvent e in log)
        {
            var m = System.Text.RegularExpressions.Regex.Match(e.Text, @"\[init (\d)\]");
            if (e.Kind == EventKind.AbilityResolved && m.Success) st.ResolvedByTier[int.Parse(m.Groups[1].Value)]++;
        }

        // Walk the event stream for per-half ladder facts.
        bool inLadder = false;
        int castsThisHalf = 0;
        bool passedThisHalf = false;
        foreach (GameEvent e in log)
        {
            bool halfBoundary = e.Kind is EventKind.Phase or EventKind.DeathCheck or EventKind.MatchOver;
            if (halfBoundary && inLadder)
            {
                st.Halves++;
                if (castsThisHalf > 0) st.HalvesOpened++;
                if (passedThisHalf) st.HalvesByPass++;
                inLadder = false;
            }

            if (e.Kind == EventKind.Phase && e.Text.Contains("ladder"))
            {
                inLadder = true;
                castsThisHalf = 0;
                passedThisHalf = false;
            }

            if (e.Kind == EventKind.AbilityResolved)
            {
                if (e.Text.Contains("CHAIN")) st.Chains++;
                else
                {
                    castsThisHalf++;
                    st.Resolutions++;
                }
            }

            if (e.Kind == EventKind.Pass) passedThisHalf = true;
            if (e.Kind == EventKind.Death) st.Deaths++;
            if (e.Kind == EventKind.Structure && e.Text.Contains("CAPTURES")) st.Captures++;
            if (e.Kind == EventKind.Basic)
            {
                if (e.Text.Contains("basic-attacks")) st.BasicAttacks++;
                else if (e.Text.Contains("moves") || e.Text.Contains("enters")) st.BasicMoves++;
            }

        }

        foreach (Team t in new[] { Team.A, Team.B })
        {
            if (log.Any(e => e.Kind == EventKind.Opening && e.Text.Contains("fallback") && e.Text.StartsWith($"{t}:"))) st.OpeningFallbackTeams++;
        }
    }

    private static void Report(Game game, Stats st, string a, string b, double seconds)
    {
        double m = st.Matches;
        Console.WriteLine();
        Console.WriteLine($"SELF-PLAY — {st.Matches} matches, A = {a}, B = {b}, {seconds:F1}s");
        Console.WriteLine($"  rules: nexus HP {game.Rules.NexusHp}, kill siege {game.Rules.KillSiege}, tower siege {game.Rules.TowerSiege}/round, tower HP {game.Rules.TowerHp}, tower shot {game.Rules.TowerShot}, defender {game.Rules.DefenderWeight}");
        Console.WriteLine();
        Console.WriteLine($"  Results       A {st.WinsA / m,6:P0}   B {st.WinsB / m,6:P0}   draw {st.Draws / m,6:P0}");
        Console.WriteLine($"  Endings       siege {st.Siege / m,6:P1}   direct nexus kill {st.Nexus / m,6:P1}   round cap {st.Cap / m,6:P1}   comebacks {st.Comebacks / m,6:P1}");
        Console.WriteLine($"  Decisions     {st.PlayerDecisions / m,5:F0} per match with a real choice (both teams, after the draft)");
        Console.WriteLine($"  Board         {Board.Layout.Name}: {Board.AllHexes.Length} hexes; front lines {Math.Abs(Board.StartHex(Team.A, Sim.Content.Role.Mid).R - Board.StartHex(Team.B, Sim.Content.Role.Mid).R)} rows apart; towers {(game.Rules.TowersBlock ? "solid" : "walkable")}");
        Console.WriteLine($"  Contact       after the opening, each champion's nearest enemy is {st.GapTotal / (double)Math.Max(1, st.GapSamples),4:F1} hexes away on average");
        long tiers = st.ResolvedByTier.Sum();
        Console.WriteLine($"  Abilities     resolved by initiative tier: 1 {st.ResolvedByTier[1] / (double)tiers,4:P0} · 2 {st.ResolvedByTier[2] / (double)tiers,4:P0} · 3 {st.ResolvedByTier[3] / (double)tiers,4:P0} · 4 {st.ResolvedByTier[4] / (double)tiers,4:P0}");
        Console.WriteLine($"  Patterns      ready tier-3/4 abilities with a target on the ladder: {st.PatternLive / (double)Math.Max(1, st.PatternChecks),5:P0}");
        st.RoundCounts.Sort();
        Console.WriteLine($"  Rounds        mean {st.Rounds / m,5:F1}   median {st.RoundCounts[st.RoundCounts.Count / 2]}   min {st.RoundCounts[0]}   max {st.RoundCounts[^1]}");
        Console.WriteLine($"  Per round     deaths {st.Deaths / (double)st.Rounds,4:F2}   captures {st.Captures / (double)st.Rounds,4:F2}   resolutions {st.Resolutions / (double)st.Rounds,5:F1}   chains {st.Chains / (double)st.Rounds,4:F2}");
        Console.WriteLine($"  Nexus open    in {st.MatchesNexusOpened / m,6:P0} of matches, {st.NexusOpenDecisions / (double)st.Decisions,6:P1} of decisions; hit in {st.MatchesNexusHit / m,6:P0} of matches ({st.NexusHits / m:F1} hits/match)");
        Console.WriteLine($"  Basics        moves {st.BasicMoves / (double)(st.BasicMoves + st.BasicAttacks),6:P0}   attacks {st.BasicAttacks / (double)(st.BasicMoves + st.BasicAttacks),6:P0}");
        Console.WriteLine($"  Opening       team-openings hitting the fallback {st.OpeningFallbackTeams / (2 * m),6:P0}   (Opening #10 wants 10–25%)");
        if (st.AiCalls > 0)
        {
            Console.WriteLine($"  AI time       mean {st.AiMsTotal / st.AiCalls,6:F1} ms   max {st.AiMsMax,7:F1} ms   (budget 1500 ms)");
        }

        Console.WriteLine();
        Console.WriteLine("  CHAMPIONS (win rate when picked; mirrors count once per side)");
        foreach (var (name, (picks, wins)) in st.Champs.OrderByDescending(kv => kv.Value.Wins / (double)kv.Value.Picks))
        {
            double wr = wins / (double)picks;
            string flag = wr > 0.56 ? "  ▲ strong" : wr < 0.44 ? "  ▼ weak" : "";
            Console.WriteLine($"    {name,-12} {wr,6:P0}  ({picks} picks){flag}");
        }

        Console.WriteLine();
        Console.WriteLine("  ACCEPTANCE CRITERIA (from the GDDs)");
        Check("ladder opened in ≥70% of halves (Movement #12)", st.HalvesOpened / (double)st.Halves, v => v >= 0.70);
        Check("halves ending by deliberate pass ≥50% (Sigils #14)", st.HalvesByPass / (double)st.Halves, v => v >= 0.50);
        Check("every match ends with a nexus falling (v2)", (st.Nexus + st.Siege) / m, v => v >= 0.99);
        Check("some matches are comebacks (Map #25, v2 reading)", st.Comebacks / m, v => v > 0 && v < 0.5);
        Check("chains occur in a minority of halves (Sigils #15)", st.Chains / (double)st.Halves, v => v > 0 && v < 0.5);
        Check("basic attacks ≤80% of basics (Movement #11)", st.BasicAttacks / (double)(st.BasicMoves + st.BasicAttacks), v => v <= 0.80);
        Check("≤16 ability resolutions per round (Ladder F5)", st.Resolutions / (double)st.Rounds, v => v <= 16, pct: false);
        if (st.AiCalls > 0) Check("AI max decision ≤1500 ms", st.AiMsMax, v => v <= 1500, pct: false);
        Console.WriteLine();
    }

    private static void Check(string label, double value, Func<double, bool> ok, bool pct = true)
    {
        string shown = pct ? value.ToString("P0") : value.ToString("F1");
        Console.WriteLine($"    {(ok(value) ? "PASS" : "FAIL")}  {label,-52} {shown,8}");
    }
}
