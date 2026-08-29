using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Tests whether an Opening Phase built from multi-champion instruction sets is a real
/// sequencing puzzle or merely five independent choices wearing a costume.
/// </summary>
/// <remarks>
/// <para>The proposal: each champion spends one ability during the opening, and an
/// opening ability issues instructions to <b>several</b> champions at once — "jungler one
/// north then one north-east, top one north, bottom one west". Champions block each other
/// and the board has edges, so an instruction can be illegal.</para>
/// <para>The claim that makes it worth building is that <b>order matters</b>: playing the
/// same five abilities in a different sequence should land the team somewhere else. If
/// the number of distinct outcomes across all 120 orderings is near 1, the idea is fake
/// complexity. If it is near 120, the opening is a genuine puzzle.</para>
/// </remarks>
public static class OpeningSequencing
{
    private const int Radius = BoardLayout.Radius;
    private const int Team = 5;
    private const int Trials = 2_000;

    /// <summary>Deterministic LCG (ADR-0002) — the harness must reproduce exactly.</summary>
    private sealed class Rng(uint seed)
    {
        private uint _s = seed;
        private uint Next() => _s = _s * 1664525u + 1013904223u;
        public int Below(int n) => (int)((Next() >> 8) % (uint)n);
    }

    /// <summary>One line of an opening ability: move champion <c>Who</c> one hex.</summary>
    private readonly record struct Step(int Who, int Direction);

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("OPENING PHASE — is multi-champion sequencing a real puzzle?");
        Console.WriteLine($"{Trials:N0} random ability sets, all 120 orderings of each.");
        Console.WriteLine();
        Console.WriteLine("  Five champions start on the front line. Each plays one ability; an");
        Console.WriteLine("  ability issues N single-hex instructions to any champions. Champions");
        Console.WriteLine("  block, the board has edges, and an illegal instruction is SKIPPED");
        Console.WriteLine("  rather than making the ability illegal (partial execution).");
        Console.WriteLine();
        Console.WriteLine("   design                       distinct outcomes  spread  skipped");
        Console.WriteLine("   ───────────────────────────  ─────────────────  ──────  ───────");

        Report("independent: 1 self-move each", 1, selfOnly: true);
        Report("multi-champion, 2 steps", 2, selfOnly: false);
        Report("multi-champion, 3 steps", 3, selfOnly: false);
        Report("multi-champion, 4 steps", 4, selfOnly: false);

        Console.WriteLine();
        Console.WriteLine("   'distinct outcomes' = how many of the 120 orderings end in different");
        Console.WriteLine("        final formations. Near 1 means order is irrelevant and the idea");
        Console.WriteLine("        is fake complexity; near 120 means the opening is a real puzzle.");
        Console.WriteLine("   'spread' = mean hexes a champion sits away from where the same five");
        Console.WriteLine("        abilities in another order would have put it.");
        Console.WriteLine("   'skipped' = share of instructions that were illegal when reached —");
        Console.WriteLine("        the 'you are forced into your awkward ability' effect.");
        Console.WriteLine();
    }

    private static void Report(string label, int stepsPerAbility, bool selfOnly)
    {
        var rng = new Rng(0x09E01234u);   // same seed per design, so they are comparable
        long distinctTotal = 0, skipped = 0, issued = 0;
        double spreadTotal = 0;

        for (int t = 0; t < Trials; t++)
        {
            Step[][] abilities = new Step[Team][];
            for (int a = 0; a < Team; a++)
            {
                var steps = new Step[stepsPerAbility];
                for (int i = 0; i < stepsPerAbility; i++)
                {
                    steps[i] = new Step(selfOnly ? a : rng.Below(Team), rng.Below(6));
                }

                abilities[a] = steps;
            }

            var outcomes = new HashSet<string>();
            var configs = new List<HexCoord[]>();
            foreach (int[] order in Permutations(Team))
            {
                HexCoord[] final = Execute(abilities, order, ref skipped, ref issued);
                outcomes.Add(string.Join(";", final.Select(h => $"{h.Q},{h.R}")));
                configs.Add(final);
            }

            distinctTotal += outcomes.Count;
            spreadTotal += MeanPairwiseSpread(configs);
        }

        double distinct = (double)distinctTotal / Trials;
        Console.WriteLine($"   {label,-27}  {distinct,7:F1} of 120     {spreadTotal / Trials,5:F2}"
                          + $"   {(double)skipped / issued,6:P1}");
    }

    /// <summary>Plays the five abilities in the given order and returns final positions.</summary>
    private static HexCoord[] Execute(Step[][] abilities, int[] order,
                                      ref long skipped, ref long issued)
    {
        var at = new HexCoord[Team];
        for (int c = 0; c < Team; c++) at[c] = new HexCoord(c, -Radius);   // the front line

        foreach (int a in order)
        {
            foreach (Step step in abilities[a])
            {
                issued++;
                HexCoord from = at[step.Who];
                HexCoord to = from + Hex.Directions[step.Direction];

                bool legal = Hex.InBoard(to, Radius)
                             && !at.Any(o => o == to);
                if (legal) at[step.Who] = to;
                else skipped++;
            }
        }

        return at;
    }

    /// <summary>Mean hex distance between the same champion across two orderings.</summary>
    private static double MeanPairwiseSpread(List<HexCoord[]> configs)
    {
        // Sampled rather than exhaustive: 120 choose 2 per trial would dominate runtime.
        double total = 0;
        int pairs = 0;
        for (int i = 0; i < configs.Count; i += 17)
        {
            for (int j = i + 1; j < configs.Count; j += 23)
            {
                for (int c = 0; c < Team; c++)
                {
                    total += HexCoord.Distance(configs[i][c], configs[j][c]);
                }

                pairs++;
            }
        }

        return pairs == 0 ? 0 : total / (pairs * Team);
    }

    private static IEnumerable<int[]> Permutations(int n)
    {
        var idx = Enumerable.Range(0, n).ToArray();
        return Permute(idx, 0);

        static IEnumerable<int[]> Permute(int[] a, int k)
        {
            if (k == a.Length) { yield return (int[])a.Clone(); yield break; }
            for (int i = k; i < a.Length; i++)
            {
                (a[k], a[i]) = (a[i], a[k]);
                foreach (int[] p in Permute(a, k + 1)) yield return p;
                (a[k], a[i]) = (a[i], a[k]);
            }
        }
    }
}
