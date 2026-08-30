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

        Availability();
        Coherent();

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

    /// <summary>
    /// Tests the adopted rule: an ability is either AVAILABLE or not — no partial
    /// execution — you MUST play an available one if any exists, and only when no unacted
    /// champion has any available ability does the fallback apply (move one hex, any
    /// direction).
    /// </summary>
    /// <remarks>
    /// The failure mode being hunted: if availability is too rare the phase degenerates
    /// into the fallback, which is the independent one-hex-each design the whole idea
    /// exists to escape. The opposite failure is availability so common that the
    /// constraint never bites and ordering stops mattering.
    /// </remarks>
    private static void Availability()
    {
        const int PerChampion = 4;

        Console.WriteLine();
        Console.WriteLine("  ALL-OR-NOTHING AVAILABILITY (the adopted rule)");
        Console.WriteLine("  An ability is available only if EVERY instruction in it can execute.");
        Console.WriteLine("  'lenient' relaxes that to: available if its FIRST instruction can.");
        Console.WriteLine($"  4 abilities per champion, {Team} champions, {Trials:N0} openings each.");
        Console.WriteLine();
        Console.WriteLine("   steps  rule      avail. at step 1   at step 5   fallback   enabled");
        Console.WriteLine("   ─────  ────────  ────────────────   ─────────   ────────   ───────");

        foreach (int steps in new[] { 2, 3, 4 })
        {
            foreach (bool strict in new[] { true, false })
            {
                var rng = new Rng(0x0AE0FF21u);
                double first = 0, last = 0;
                int fellBack = 0, enabled = 0, enableChances = 0;

                for (int t = 0; t < Trials; t++)
                {
                    Step[][][] kit = new Step[Team][][];
                    for (int c = 0; c < Team; c++)
                    {
                        kit[c] = new Step[PerChampion][];
                        for (int a = 0; a < PerChampion; a++)
                        {
                            var st = new Step[steps];
                            for (int i = 0; i < steps; i++)
                                st[i] = new Step(rng.Below(Team), rng.Below(6));
                            kit[c][a] = st;
                        }
                    }

                    var at = new HexCoord[Team];
                    for (int c = 0; c < Team; c++) at[c] = new HexCoord(c, -Radius);
                    var acted = new bool[Team];
                    bool anyFallback = false;
                    bool[,] wasLegal = new bool[Team, PerChampion];

                    for (int turn = 0; turn < Team; turn++)
                    {
                        var options = new List<(int C, int A)>();
                        for (int c = 0; c < Team; c++)
                        {
                            if (acted[c]) continue;
                            for (int a = 0; a < PerChampion; a++)
                            {
                                bool ok = Available(kit[c][a], at, strict);
                                if (turn > 0 && !wasLegal[c, a])
                                {
                                    enableChances++;
                                    if (ok) enabled++;
                                }

                                wasLegal[c, a] = ok;
                                if (ok) options.Add((c, a));
                            }
                        }

                        if (turn == 0) first += options.Count;
                        if (turn == Team - 1) last += options.Count;

                        if (options.Count == 0)
                        {
                            anyFallback = true;
                            // Fallback: every remaining champion takes one free hex.
                            for (int c = 0; c < Team; c++)
                            {
                                if (acted[c]) continue;
                                acted[c] = true;
                            }

                            break;
                        }

                        (int ch, int ab) = options[rng.Below(options.Count)];
                        Apply(kit[ch][ab], at);
                        acted[ch] = true;
                    }

                    if (anyFallback) fellBack++;
                }

                Console.WriteLine($"   {steps,5}  {(strict ? "strict" : "lenient"),-8}"
                                  + $"  {first / Trials,7:F1} of 20      {last / Trials,5:F1} of 4"
                                  + $"   {(double)fellBack / Trials,7:P1}"
                                  + $"   {(enableChances == 0 ? 0 : (double)enabled / enableChances),7:P1}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("   'avail. at step 1' — legal (champion, ability) pairs out of 20 at the start.");
        Console.WriteLine("   'at step 5'        — out of the last champion's 4. This is where the");
        Console.WriteLine("                        squeeze lands, and where the fallback gets used.");
        Console.WriteLine("   'fallback'         — openings in which no unacted champion had ANY legal");
        Console.WriteLine("                        ability. High means the phase degenerates into");
        Console.WriteLine("                        move-one-hex, the design this replaces.");
        Console.WriteLine("   'enabled'          — abilities that were illegal and became legal after");
        Console.WriteLine("                        someone else moved. This is the constructive half of");
        Console.WriteLine("                        the puzzle: playing X on purpose to unlock Y.");
    }

    /// <summary>
    /// Re-runs availability with <b>authored-looking</b> abilities instead of random ones,
    /// and tests whether draft alignment is a real strategic axis.
    /// </summary>
    /// <remarks>
    /// <para>Two corrections to the random model. First, an opening ability has a
    /// <b>coherent intent</b> — its instructions push in a consistent lateral direction
    /// rather than contradicting each other. Second, no designer authors a step that walks
    /// a champion backward off its own front line, so directions are drawn from the
    /// forward and lateral set only.</para>
    /// <para>Instructions name a <b>role</b>, not a champion — "bottom moves west" — so
    /// whether an ability helps depends on which champion was drafted into that role. That
    /// is the axis under test: a team whose champions want to be where its opening kit
    /// pushes them should end up measurably better placed than one that drafted for combat
    /// alone.</para>
    /// </remarks>
    private static void Coherent()
    {
        const int PerChampion = 4;
        // Directions by file delta (file = 2q + r): forward-west, forward-east, west, east.
        int[] forwardWest = [4], forwardEast = [5], west = [3], east = [0];

        Console.WriteLine();
        Console.WriteLine("  AUTHORED-LOOKING ABILITIES, AND WHETHER THE DRAFT MATTERS");
        Console.WriteLine("  Instructions now push a consistent lateral intent and never walk a");
        Console.WriteLine("  champion backward off its own front line. They name ROLES, so an");
        Console.WriteLine("  ability's value depends on which champion was drafted into that role.");
        Console.WriteLine();
        Console.WriteLine("   steps  draft        fallback   avail@1   misplacement");
        Console.WriteLine("   ─────  ───────────  ────────   ───────   ────────────");

        foreach (int steps in new[] { 2, 3, 4 })
        {
            foreach (bool aligned in new[] { true, false })
            {
                // Two streams: abilities must be IDENTICAL across the two draft
                // conditions, or the comparison comes apart. Only 'want' may differ.
                var rng = new Rng(0xC04E1200u);
                var wantRng = new Rng(0x5EED0417u);
                int fellBack = 0;
                double availFirst = 0, misplace = 0;

                for (int t = 0; t < Trials; t++)
                {
                    // Author each champion's four abilities with one lateral intent each.
                    var kit = new Step[Team][][];
                    var pushOnRole = new int[Team];
                    for (int c = 0; c < Team; c++)
                    {
                        kit[c] = new Step[PerChampion][];
                        for (int a = 0; a < PerChampion; a++)
                        {
                            int intent = rng.Below(2) == 0 ? -1 : +1;      // west or east
                            var st = new Step[steps];
                            for (int i = 0; i < steps; i++)
                            {
                                int role = rng.Below(Team);
                                int[] pool = rng.Below(2) == 0
                                    ? (intent < 0 ? forwardWest : forwardEast)
                                    : (intent < 0 ? west : east);
                                st[i] = new Step(role, pool[0]);
                                pushOnRole[role] += intent;
                            }

                            kit[c][a] = st;
                        }
                    }

                    // Aligned: each champion wants the file its own kit tends to push it toward.
                    var want = new int[Team];
                    for (int c = 0; c < Team; c++)
                    {
                        int roll = wantRng.Below(5);
                        want[c] = aligned
                            ? Math.Sign(pushOnRole[c]) * 4
                            : (roll - 2) * 2;
                    }

                    var at = new HexCoord[Team];
                    for (int c = 0; c < Team; c++) at[c] = new HexCoord(c, -Radius);
                    var acted = new bool[Team];
                    bool anyFallback = false;

                    for (int turn = 0; turn < Team; turn++)
                    {
                        var options = new List<(int C, int A)>();
                        for (int c = 0; c < Team; c++)
                        {
                            if (acted[c]) continue;
                            for (int a = 0; a < PerChampion; a++)
                            {
                                if (Available(kit[c][a], at, strict: true)) options.Add((c, a));
                            }
                        }

                        if (turn == 0) availFirst += options.Count;
                        if (options.Count == 0)
                        {
                            anyFallback = true;
                            break;
                        }

                        // Play the option that best serves the team's positional wants.
                        (int C, int A) best = options[0];
                        int bestScore = int.MinValue;
                        foreach ((int c, int a) in options)
                        {
                            var sim = (HexCoord[])at.Clone();
                            Apply(kit[c][a], sim);
                            int score = -Enumerable.Range(0, Team)
                                .Sum(k => Math.Abs(File(sim[k]) - want[k]));
                            if (score > bestScore) { bestScore = score; best = (c, a); }
                        }

                        Apply(kit[best.C][best.A], at);
                        acted[best.C] = true;
                    }

                    if (anyFallback) fellBack++;
                    misplace += Enumerable.Range(0, Team)
                        .Average(k => Math.Abs(File(at[k]) - want[k]));
                }

                Console.WriteLine($"   {steps,5}  {(aligned ? "aligned" : "mismatched"),-11}"
                                  + $"  {(double)fellBack / Trials,7:P1}   {availFirst / Trials,5:F1}/20"
                                  + $"   {misplace / Trials,10:F2}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("   'misplacement' = mean file distance between where a champion ended and");
        Console.WriteLine("        where it wanted to be. Lower is better. If aligned and mismatched");
        Console.WriteLine("        drafts score the same, the opening kit is NOT a draft axis and");
        Console.WriteLine("        picking for it would be wasted effort.");
        Console.WriteLine();
    }

    /// <summary>Position across the board: 0 on the centre axis, +/-8 at the side corners.</summary>
    private static int File(HexCoord h) => 2 * h.Q + h.R;

    private static bool Available(Step[] ability, HexCoord[] at, bool strict)
    {
        var sim = (HexCoord[])at.Clone();
        bool firstOk = false;
        for (int i = 0; i < ability.Length; i++)
        {
            Step st = ability[i];
            HexCoord to = sim[st.Who] + Hex.Directions[st.Direction];
            bool ok = Hex.InBoard(to, Radius) && !sim.Any(o => o == to);
            if (i == 0) firstOk = ok;
            if (!ok && strict) return false;
            if (ok) sim[st.Who] = to;
        }

        return strict || firstOk;
    }

    private static void Apply(Step[] ability, HexCoord[] at)
    {
        foreach (Step st in ability)
        {
            HexCoord to = at[st.Who] + Hex.Directions[st.Direction];
            if (Hex.InBoard(to, Radius) && !at.Any(o => o == to)) at[st.Who] = to;
        }
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
