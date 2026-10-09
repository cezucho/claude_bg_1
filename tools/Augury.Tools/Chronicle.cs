using Augury.Sim;
using Augury.Sim.AI;
using Augury.Sim.Content;

namespace Augury.Tools;

/// <summary>
/// One AI-vs-AI match, summarised round by round for a human to read: the drafts and their
/// synergy pairs, spells, the opening, then per round the nexus race, towers, kills (who killed
/// whom), captures and combos. Usage: <c>chronicle [seed] draft=great-decent [rule=value…]</c>.
/// The full play-by-play goes to <c>chronicle-[seed].log</c> beside it when <c>out=dir</c> is given.
/// </summary>
public static class Chronicle
{
    private sealed class RoundTally : IMatchObserver
    {
        public readonly List<string> Kills = new();
        public readonly int[] Dealt = new int[2], Payoffs = new int[2], Slams = new int[2], Healed = new int[2], Tower = new int[2];
        private readonly int[] _lastHit = Enumerable.Repeat(-1, 10).ToArray();
        public Func<int, string> Name = i => i.ToString();

        public void ChampionDamaged(int source, int target, int hpLost, int absorbed)
        {
            if (source < 0 || source / 5 == target / 5) return;
            Dealt[source / 5] += hpLost + absorbed;
            _lastHit[target] = source;
        }

        public void ChampionHealed(int source, int target, int amount)
        {
            if (source >= 0) Healed[source / 5] += amount;
        }

        public void Shielded(int source, int target, int amount) { }

        public void StructureDamaged(int source, int amount, bool nexus)
        {
            if (source >= 0) Tower[source / 5] += amount;
        }

        public void Payoff(int source, int target) => Payoffs[source / 5]++;

        public void Slam(int source, int target) => Slams[source / 5]++;

        public void Died(int victim)
        {
            Kills.Add(_lastHit[victim] >= 0 ? $"{Name(_lastHit[victim])} killed {Name(victim)}" : $"{Name(victim)} died to towers or poison");
            _lastHit[victim] = -1;
        }

        public void Reset()
        {
            Kills.Clear();
            Array.Clear(Dealt);
            Array.Clear(Payoffs);
            Array.Clear(Slams);
            Array.Clear(Healed);
            Array.Clear(Tower);
        }
    }

    public static void Run(string[] args)
    {
        uint seed = args.Length > 1 && uint.TryParse(args[1], out uint sd) ? sd : 1;
        string mode = args.FirstOrDefault(a => a.StartsWith("draft="))?["draft=".Length..] ?? "great-decent";
        string? outDir = args.FirstOrDefault(a => a.StartsWith("out="))?["out=".Length..];
        var overrides = args.Skip(1).Where(a => a.Contains('=') && !a.StartsWith("draft=") && !a.StartsWith("out="));
        Game baseline = Game.LoadDefault();
        Game game = new(baseline.Content, SelfPlay.Override(baseline.Rules, overrides));
        (string x, string y) = SelfPlay.Styles(mode);

        var draftA = SelfPlay.Drafter(game, x, seed);
        var draftB = SelfPlay.Drafter(game, y, seed + 7777);
        var ai = new HeuristicAgent(game, seed) { NoisePermille = 150 };
        var bi = new HeuristicAgent(game, seed + 1) { NoisePermille = 150 };

        MatchState s = game.NewMatch();
        var log = new List<GameEvent>();
        var tally = new RoundTally();
        MatchState names = s;
        tally.Name = i => game.Name(names, i);
        var legal = new List<Command>();
        int round = 0, logStart = 0;
        var w = Console.Out;
        w.WriteLine($"CHRONICLE — seed {seed}, team A drafts {x}, team B drafts {y}, board {Board.Layout.Name}, payoff scale {game.Rules.PayoffScale}");

        while (s.Phase != Phase.MatchOver)
        {
            legal.Clear();
            game.Legal(s, legal);
            Phase before = s.Phase;
            Command c = s.Phase == Phase.Draft ? (s.Active == Team.A ? draftA : draftB).Choose(s, legal) : (s.Active == Team.A ? ai : bi).Choose(s, legal);
            game.Apply(ref s, c, log, tally);
            names = s;

            if (before == Phase.Draft && s.Phase != Phase.Draft) DraftSummary(w, game, s, log);
            if (before == Phase.SpellPick && s.Phase != Phase.SpellPick) SpellSummary(w, game, s);
            if (before == Phase.Opening && s.Phase != Phase.Opening)
            {
                w.WriteLine();
                w.WriteLine("OPENING");
                foreach (GameEvent e in log.Skip(logStart).Where(e => e.Kind == EventKind.Opening && !e.Text.StartsWith("  ") || e.Text.Contains("casts (opening)") || e.Text.Contains("fizzles") || e.Text.Contains("ROOTED") || e.Text.Contains("BURNING") || e.Text.Contains("MARKED")))
                {
                    w.WriteLine("  " + e.Text.Trim());
                }

                Line(w, game, s, tally, "after the opening");
                tally.Reset();
                logStart = log.Count;
                round = 1;
            }
            else if (s.Round != round && round > 0 || s.Phase == Phase.MatchOver)
            {
                w.WriteLine();
                w.WriteLine($"ROUND {round}");
                foreach (GameEvent e in log.Skip(logStart).Where(e => e.Text.Contains("CAPTURES") || e.Text.Contains("PAYOFF") || e.Text.Contains("SLAMS") || e.Text.Contains("NEXUS") && e.Kind == EventKind.Structure || e.Kind == EventKind.MatchOver || (e.Kind == EventKind.AbilityResolved && SpellCast(game, e.Text))))
                {
                    w.WriteLine("  " + e.Text.Trim());
                }

                foreach (string k in tally.Kills) w.WriteLine($"  ✝ {k}");
                Line(w, game, s, tally, $"end of round {round}");
                tally.Reset();
                logStart = log.Count;
                round = s.Round;
            }
        }

        w.WriteLine();
        w.WriteLine(log[^1].Text);

        if (outDir is not null)
        {
            Directory.CreateDirectory(outDir);
            File.WriteAllLines(Path.Combine(outDir, $"chronicle-{seed}.log"), log.Select(e => e.Text));
        }
    }

    private static bool SpellCast(Game game, string text) => game.Content.Spells.Any(sp => text.Contains($"casts {sp.Name} ") || text.Contains($"casts (opening) {sp.Name} "));

    private static void DraftSummary(TextWriter w, Game game, MatchState s, List<GameEvent> log)
    {
        w.WriteLine();
        w.WriteLine("DRAFT (pick order)");
        foreach (GameEvent e in log.Where(e => e.Kind == EventKind.Draft && e.Text.Contains("drafts"))) w.WriteLine("  " + e.Text);
        foreach (Team t in new[] { Team.A, Team.B })
        {
            int first = MatchState.FirstSlot(t);
            var defs = Enumerable.Range(first, 5).Select(i => (int)s.Champions[i].Def).ToList();
            var pairs = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = i + 1; j < 5; j++)
                {
                    string a = game.Content.Champions[defs[i]].Id, b = game.Content.Champions[defs[j]].Id;
                    foreach (SynergyGroup g in game.Content.Synergies.Where(g => g.Links(a, b)))
                    {
                        pairs.Add($"{game.Content.Champions[defs[i]].Name}+{game.Content.Champions[defs[j]].Name} ({g.Name})");
                    }
                }
            }

            w.WriteLine($"  Team {t}: {string.Join(", ", defs.Select(d => game.Content.Champions[d].Name))} — synergy {game.Content.SynergyScore(defs)} pair(s){(pairs.Count > 0 ? ": " + string.Join(", ", pairs) : "")}");
        }
    }

    private static void SpellSummary(TextWriter w, Game game, MatchState s)
    {
        for (int t = 0; t < 2; t++)
        {
            var parts = new List<string>();
            for (int i = t * 5; i < t * 5 + 5; i++) parts.Add($"{game.Def(s.Champions[i]).Name} {game.Content.Spells[s.Champions[i].Spell].Name}");
            w.WriteLine($"  Spells {(t == 0 ? "A" : "B")}: {string.Join(", ", parts)}");
        }
    }

    private static void Line(TextWriter w, Game game, MatchState s, RoundTally t, string when)
    {
        string towers = string.Join(" ", Enumerable.Range(0, 5).Select(i => $"{(i == 0 ? "C" : i <= 2 ? "a" : "b")}{i}:{(s.Towers[i].Owner == Team.None ? "–" : s.Towers[i].Owner.ToString())}"));
        int hpA = Enumerable.Range(0, 5).Where(i => s.Champions[i].OnBoard).Sum(i => Math.Max(0, s.Champions[i].Hp));
        int hpB = Enumerable.Range(5, 5).Where(i => s.Champions[i].OnBoard).Sum(i => Math.Max(0, s.Champions[i].Hp));
        int upA = Enumerable.Range(0, 5).Count(i => s.Champions[i].Presence != Presence.Dead);
        int upB = Enumerable.Range(5, 5).Count(i => s.Champions[i].Presence != Presence.Dead);
        w.WriteLine($"  ── {when}: NEXUS A {Math.Max(0, s.NexusHp[0])} · B {Math.Max(0, s.NexusHp[1])} │ towers {towers} │ alive A {upA} ({hpA} HP) · B {upB} ({hpB} HP)");
        w.WriteLine($"     this stretch: damage A {t.Dealt[0]} · B {t.Dealt[1]} │ to structures A {t.Tower[0]} · B {t.Tower[1]} │ healing A {t.Healed[0]} · B {t.Healed[1]} │ combos (payoffs+slams) A {t.Payoffs[0] + t.Slams[0]} · B {t.Payoffs[1] + t.Slams[1]}");
    }
}
