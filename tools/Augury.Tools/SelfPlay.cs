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
        public int Matches, WinsA, WinsB, Draws, Nexus, Score, Cap, NexusFromBehind;
        public long Rounds, Halves, HalvesOpened, HalvesByPass, Resolutions, Chains, Deaths, Captures;
        public long BasicMoves, BasicAttacks, OpeningFallbackTeams, Decisions;
        public double AiMsTotal, AiMsMax;
        public long AiCalls;
        public readonly List<int> RoundCounts = new();
    }

    public static void Run(string[] args)
    {
        int n = args.Length > 1 ? int.Parse(args[1]) : 100;
        string aName = args.Length > 2 ? args[2] : "heuristic";
        string bName = args.Length > 3 ? args[3] : "heuristic";
        Game game = Game.LoadDefault();
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

            game.Apply(ref s, c, log);
            st.Decisions++;
        }

        st.Matches++;
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
            case EndReason.Nexus:
                st.Nexus++;
                int w = s.Winner == Team.A ? 0 : 1;
                if (s.Score[w] < s.Score[1 - w]) st.NexusFromBehind++;
                break;
            case EndReason.Score: st.Score++; break;
            case EndReason.RoundCap: st.Cap++; break;
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

            if (e.Kind == EventKind.Opening && e.Text.Contains("fallback")) st.OpeningFallbackTeams++;
        }
    }

    private static void Report(Game game, Stats st, string a, string b, double seconds)
    {
        double m = st.Matches;
        Console.WriteLine();
        Console.WriteLine($"SELF-PLAY — {st.Matches} matches, A = {a}, B = {b}, {seconds:F1}s");
        Console.WriteLine($"  rules: target {game.Rules.TargetScore}, kill {game.Rules.KillPoints}, tower {game.Rules.TowerPoints}/round, nexus HP {game.Rules.NexusHp}, tower HP {game.Rules.TowerHp}");
        Console.WriteLine();
        Console.WriteLine($"  Results       A {st.WinsA / m,6:P0}   B {st.WinsB / m,6:P0}   draw {st.Draws / m,6:P0}");
        Console.WriteLine($"  Endings       score {st.Score / m,6:P0}   nexus {st.Nexus / m,6:P0}   round cap {st.Cap / m,6:P0}");
        st.RoundCounts.Sort();
        Console.WriteLine($"  Rounds        mean {st.Rounds / m,5:F1}   median {st.RoundCounts[st.RoundCounts.Count / 2]}   min {st.RoundCounts[0]}   max {st.RoundCounts[^1]}");
        Console.WriteLine($"  Per round     deaths {st.Deaths / (double)st.Rounds,4:F2}   captures {st.Captures / (double)st.Rounds,4:F2}   resolutions {st.Resolutions / (double)st.Rounds,5:F1}   chains {st.Chains / (double)st.Rounds,4:F2}");
        Console.WriteLine($"  Basics        moves {st.BasicMoves / (double)(st.BasicMoves + st.BasicAttacks),6:P0}   attacks {st.BasicAttacks / (double)(st.BasicMoves + st.BasicAttacks),6:P0}");
        Console.WriteLine($"  Opening       teams hitting fallback {st.OpeningFallbackTeams / (2 * m),6:P0} of champion-plays ÷ 5 ≈ openings");
        if (st.AiCalls > 0)
        {
            Console.WriteLine($"  AI time       mean {st.AiMsTotal / st.AiCalls,6:F1} ms   max {st.AiMsMax,7:F1} ms   (budget 1500 ms)");
        }

        Console.WriteLine();
        Console.WriteLine("  ACCEPTANCE CRITERIA (from the GDDs)");
        Check("ladder opened in ≥70% of halves (Movement #12)", st.HalvesOpened / (double)st.Halves, v => v >= 0.70);
        Check("halves ending by deliberate pass ≥50% (Sigils #14)", st.HalvesByPass / (double)st.Halves, v => v >= 0.50);
        Check("nexus endings a non-zero minority (Map #24)", st.Nexus / m, v => v > 0 && v < 0.5);
        Check("some nexus endings come from behind (Map #25)", st.Nexus == 0 ? 0 : st.NexusFromBehind / (double)st.Nexus, v => v > 0);
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
