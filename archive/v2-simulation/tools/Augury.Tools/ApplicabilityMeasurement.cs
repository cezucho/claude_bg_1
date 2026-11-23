using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Measures how often each targeting rigidity tier actually has a legal target,
/// across randomly sampled board configurations.
/// </summary>
/// <remarks>
/// <para>Closes Open Question 2 of <c>design/gdd/initiative-ladder.md</c>. Formula
/// F4 assumes <c>applicability</c> of [1.00, 0.90, 0.55, 0.30] across tiers 1-4,
/// and states plainly that this is "a measurement of pattern geometry, not a free
/// parameter". Until now it was a guess.</para>
/// <para>Randomness lives here, in the tool, never in the simulation (ADR-0002).</para>
/// </remarks>
public static class ApplicabilityMeasurement
{
    private static int[] Radii => new[] { 3, 4, 5, 6 };
    private const int PerTeam = 5;
    private const int Trials = 20_000;
    private const int Seed = 20260814;

    /// <summary>A candidate ability pattern under test.</summary>
    private readonly record struct Pattern(string Name, int Tier, HexCoord[] Offsets, int FreeRange)
    {
        public bool IsFree => Offsets.Length == 0;
    }

    /// <summary>
    /// The board's real contested points: the five towers fixed by Map & Terrain.
    /// </summary>
    private static readonly HexCoord[] Towers =
    {
        new(0, 0), new(0, -2), new(2, -2), new(0, 2), new(-2, 2)
    };

    /// <summary>
    /// The three placeholder objectives this measurement originally used, invented
    /// before Map &amp; Terrain existed. Kept only to show how far they misled.
    /// </summary>
    private static readonly HexCoord[] LegacyObjectives =
    {
        new(0, 0), new(0, -3), new(0, 3)
    };

    /// <summary>How champions are distributed across the board when sampling.</summary>
    private enum Placement
    {
        /// <summary>Uniform over every board hex. A floor, not a forecast.</summary>
        Uniform,

        /// <summary>Weighted toward the objective hexes, approximating real play.</summary>
        Contested
    }

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine($"Applicability — {PerTeam}v{PerTeam}, "
                          + $"{Trials * PerTeam:N0} samples per cell, seed {Seed}");
        Console.WriteLine("CONTESTED placement, 'useful' = reaches >=1 enemy");
        Console.WriteLine();
        Console.WriteLine("Champions are now drawn toward the FIVE REAL TOWERS fixed by");
        Console.WriteLine("Map & Terrain — (0,0) (0,-2) (2,-2) (0,2) (-2,2) — replacing the");
        Console.WriteLine("three placeholder hexes this harness used before the board existed.");
        Console.WriteLine();
        Console.WriteLine("NOTE: RCH now caps at 3 (Movement & Targeting). Range-4 rows are");
        Console.WriteLine("kept only as a reference point and must not be used to price abilities.");

        Console.WriteLine();
        Console.WriteLine("=== THE BOARD AS BUILT: radius 4, five towers ===");
        Dictionary<string, double> real = Measure(Placement.Contested, 4, Towers);

        Console.WriteLine();
        Console.WriteLine("=== WHAT THE PLACEHOLDER OBJECTIVES SAID: radius 4, 3 invented hexes ===");
        Measure(Placement.Contested, 4, LegacyObjectives);

        Console.WriteLine();
        Console.WriteLine("=== SENSITIVITY: real towers, other radii (board is fixed at 4) ===");
        foreach (int radius in Radii.Where(r => r != 4))
        {
            Measure(Placement.Contested, radius, Towers);
        }

        F4Conformance(real);

        Console.WriteLine();
        Console.WriteLine("legal  = at least one champion in the target set (F1 legality;");
        Console.WriteLine("         patterns are indiscriminate per ADR-0005)");
        Console.WriteLine("useful = at least one ENEMY in the target set — the figure that");
        Console.WriteLine("         actually belongs in F4's applicability(i)");
    }

    /// <summary>
    /// Re-checks ladder F4 against the measured numbers. F4 targets an effective value
    /// that is flat across tiers within +/-2%; that flatness IS the balance target,
    /// because no initiative tier should be systematically correct to play.
    /// </summary>
    private static void F4Conformance(Dictionary<string, double> measured)
    {
        const double K = 0.25;
        double[] m = { 1.0, 1.3, 2.0, 4.0 };

        // F4's reference patterns. Tier 1's was "free targeting, range 4" — now illegal,
        // because Movement & Targeting caps RCH at 3. Substituting range 3.
        (int Tier, string Reference, string Key)[] refs =
        [
            (1, "free targeting, range 3  (was range 4 — RCH now caps at 3)", "T1 free, range 3"),
            (2, "free targeting, range 2", "T2 free, range 2"),
            (3, "rotatable 2-hex arc at range 2", "T3 rot. 2 hex @ r2 (arc)"),
            (4, "fixed 5-hex pattern", "T4 fixed 5 hex")
        ];

        // The applicability F4 was authored against, from the placeholder objectives.
        double[] old = { 0.99, 0.81, 0.59, 0.31 };

        Console.WriteLine();
        Console.WriteLine("=== LADDER F4 RE-CHECKED ==============================================");
        Console.WriteLine("  effective_value(i) = M(i) x applicability(i) x (1 - k x i/4),  k = 0.25");
        Console.WriteLine("  Target: flat across tiers within +/-2%.");
        Console.WriteLine();
        Console.WriteLine("   i  reference pattern                          was    now     M    eff.value");
        Console.WriteLine("   ─  ────────────────────────────────────────  ─────  ─────  ────  ─────────");

        var effective = new double[4];
        foreach ((int tier, string reference, string key) in refs)
        {
            double a = measured[key];
            double e = m[tier - 1] * a * (1 - K * tier / 4.0);
            effective[tier - 1] = e;
            Console.WriteLine($"   {tier}  {reference,-42} {old[tier - 1],5:F2}  {a,5:F3}  {m[tier - 1],4:F1}"
                              + $"  {e,9:F3}");
        }

        double lo = effective.Min(), hi = effective.Max(), mid = effective.Average();
        double spread = (hi - lo) / mid;
        Console.WriteLine();
        Console.WriteLine($"   spread {lo:F3}-{hi:F3}, mean {mid:F3} = +/-{spread / 2,6:P1}"
                          + $"   {(spread <= 0.04 ? "WITHIN the +/-2% band" : "OUT OF CONFORMANCE")}");

        Console.WriteLine();
        Console.WriteLine("   M required to restore flatness, anchoring M(1) = 1.0:");
        Console.WriteLine();
        double anchor = effective[0];
        Console.Write("     M = [");
        for (int t = 1; t <= 4; t++)
        {
            double a = measured[refs[t - 1].Key];
            double needed = anchor / (a * (1 - K * t / 4.0));
            Console.Write($"{needed:F2}{(t < 4 ? ", " : "")}");
        }

        Console.WriteLine($"]   was [{string.Join(", ", m.Select(v => v.ToString("F2")))}]");
        Console.WriteLine();
        Console.WriteLine("   Tiers 3 and 4 are MORE applicable against the real towers than against");
        Console.WriteLine("   the placeholders, so they were being paid for a scarcity they do not");
        Console.WriteLine("   have. Their multipliers come down; tiers 1-2 barely move.");
    }

    private static Dictionary<string, double> Measure(Placement placement, int radius,
                                                     HexCoord[] objectives)
    {
        HexCoord[] board = BuildBoard(radius);
        // Towers are fixed to the radius-4 board; on other radii keep only those in bounds.
        HexCoord[] inBounds = objectives.Where(o => Hex.InBoard(o, radius)).ToArray();
        int[] weights = BuildWeights(board, placement, inBounds);
        var rng = new Random(Seed);   // same seed per mode — modes differ only by placement

        Pattern[] patterns =
        [
            new("T1 free, range 1 (melee)",        1, [], 1),
            new("T1 free, range 2",                1, [], 2),
            new("T1 free, range 3",                1, [], 3),
            new("T1 free, range 4",                1, [], 4),
            new("T2 free, range 2",                2, [], 2),
            new("T2 free, range 3",                2, [], 3),
            new("T3 rot. 1 hex @ r2",              3, [new(2, 0)], 0),
            new("T3 rot. 2 hex @ r2 (arc)",        3, [new(2, 0), new(2, -1)], 0),
            new("T3 rot. line r1-2",               3, [new(1, 0), new(2, 0)], 0),
            new("T3 rot. wedge-3 r1-2",            3, [new(1, 0), new(2, 0), new(1, -1)], 0),
            new("T4 fixed 2 hex @ r2",             4, [new(2, 0), new(-1, 2)], 0),
            new("T4 fixed line r1-2",              4, [new(1, 0), new(2, 0)], 0),
            new("T4 fixed 3 hex",                  4, [new(2, 0), new(-1, 2), new(-1, -1)], 0),
            new("T4 fixed 4 hex",                  4, [new(2, 0), new(-1, 2), new(-1, -1), new(1, 1)], 0),
            new("T4 fixed 5 hex",                  4, [new(2, 0), new(-1, 2), new(-1, -1), new(1, 1), new(0, -2)], 0),
            new("T4 fixed 6 hex",                  4, [new(2, 0), new(-1, 2), new(-1, -1), new(1, 1), new(0, -2), new(-2, 1)], 0),
        ];

        var legal = new int[patterns.Length];
        var useful = new int[patterns.Length];
        int samples = 0;

        var occupied = new Dictionary<HexCoord, int>(PerTeam * 2);
        var placed = new HexCoord[PerTeam * 2];

        for (int t = 0; t < Trials; t++)
        {
            occupied.Clear();
            DrawWithoutReplacement(board, weights, placed, rng);
            for (int i = 0; i < placed.Length; i++)
            {
                occupied[placed[i]] = i < PerTeam ? 0 : 1;   // team 0 first, then team 1
            }

            // Every champion of team 0 acts as the origin once.
            for (int a = 0; a < PerTeam; a++)
            {
                HexCoord origin = placed[a];
                samples++;
                for (int p = 0; p < patterns.Length; p++)
                {
                    (bool anyChampion, bool anyEnemy) = Evaluate(patterns[p], origin, occupied, radius);
                    if (anyChampion) legal[p]++;
                    if (anyEnemy) useful[p]++;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"── radius {radius} — {board.Length} hexes, "
                          + $"{(double)(PerTeam * 2) / board.Length,4:P0} occupied ──────────");
        Console.WriteLine($"{"Pattern",-38} {"Tier",4} {"useful",8}");
        Console.WriteLine(new string('-', 62));
        var rates = new Dictionary<string, double>(patterns.Length);
        for (int p = 0; p < patterns.Length; p++)
        {
            double rate = (double)useful[p] / samples;
            rates[patterns[p].Name] = rate;
            Console.WriteLine($"{patterns[p].Name,-38} {patterns[p].Tier,4} {rate,8:P1}");
        }

        return rates;
    }

    /// <summary>
    /// Sampling weight per board hex. Contested placement weights a hex by
    /// <c>1 / (1 + d)^2</c> where <c>d</c> is the distance to the nearest objective,
    /// so a champion is roughly nine times likelier to stand on an objective than
    /// two hexes off it.
    /// </summary>
    private static int[] BuildWeights(HexCoord[] board, Placement placement,
                                      HexCoord[] objectives)
    {
        var weights = new int[board.Length];
        for (int i = 0; i < board.Length; i++)
        {
            if (placement == Placement.Uniform)
            {
                weights[i] = 1;
                continue;
            }

            int nearest = int.MaxValue;
            foreach (HexCoord o in objectives)
            {
                nearest = Math.Min(nearest, HexCoord.Distance(board[i], o));
            }

            weights[i] = 10_000 / ((1 + nearest) * (1 + nearest));
        }

        return weights;
    }

    private static void DrawWithoutReplacement(
        HexCoord[] board, int[] weights, HexCoord[] into, Random rng)
    {
        Span<bool> taken = stackalloc bool[board.Length];
        for (int n = 0; n < into.Length; n++)
        {
            long total = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (!taken[i]) total += weights[i];
            }

            long roll = (long)(rng.NextDouble() * total);
            for (int i = 0; i < board.Length; i++)
            {
                if (taken[i]) continue;
                roll -= weights[i];
                if (roll < 0 || i == board.Length - 1)
                {
                    taken[i] = true;
                    into[n] = board[i];
                    break;
                }
            }
        }
    }

    private static (bool AnyChampion, bool AnyEnemy) Evaluate(
        Pattern pattern, HexCoord origin, Dictionary<HexCoord, int> occupied, int radius)
    {
        if (pattern.IsFree)
        {
            bool champ = false, enemy = false;
            foreach ((HexCoord hex, int team) in occupied)
            {
                if (hex == origin) continue;
                if (HexCoord.Distance(origin, hex) <= pattern.FreeRange)
                {
                    champ = true;
                    if (team == 1) enemy = true;
                }
            }

            return (champ, enemy);
        }

        int facings = pattern.Tier == 3 ? 6 : 1;   // tier 4 never rotates (ADR-0005)
        bool anyChampion = false, anyEnemy = false;

        for (int f = 0; f < facings; f++)
        {
            foreach (HexCoord offset in pattern.Offsets)
            {
                HexCoord target = origin + Hex.Rotate(offset, f);
                if (!Hex.InBoard(target, radius)) continue;   // off-board hexes dropped
                if (occupied.TryGetValue(target, out int team))
                {
                    anyChampion = true;
                    if (team == 1) anyEnemy = true;
                }
            }
        }

        return (anyChampion, anyEnemy);
    }

    private static HexCoord[] BuildBoard(int radius)
    {
        var hexes = new List<HexCoord>();
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                var h = new HexCoord(q, r);
                if (Hex.InBoard(h, radius)) hexes.Add(h);
            }
        }

        return hexes.ToArray();
    }

    private static void Shuffle(HexCoord[] a, Random rng)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }
}
