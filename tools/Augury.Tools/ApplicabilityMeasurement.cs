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
    private const int BoardRadius = 4;
    private const int PerTeam = 5;
    private const int Trials = 20_000;
    private const int Seed = 20260814;

    /// <summary>A candidate ability pattern under test.</summary>
    private readonly record struct Pattern(string Name, int Tier, HexCoord[] Offsets, int FreeRange)
    {
        public bool IsFree => Offsets.Length == 0;
    }

    /// <summary>
    /// The three objective hexes champions contest. Placement in
    /// <see cref="Placement.Contested"/> is weighted toward these.
    /// </summary>
    private static readonly HexCoord[] Objectives =
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
        Console.WriteLine($"Applicability — board radius {BoardRadius} "
                          + $"({BuildBoard(BoardRadius).Length} hexes), {PerTeam}v{PerTeam}, "
                          + $"{Trials * PerTeam:N0} samples per mode, seed {Seed}");

        Measure(Placement.Uniform);
        Measure(Placement.Contested);

        Console.WriteLine();
        Console.WriteLine("legal  = at least one champion in the target set (F1 legality;");
        Console.WriteLine("         patterns are indiscriminate per ADR-0005)");
        Console.WriteLine("useful = at least one ENEMY in the target set — the figure that");
        Console.WriteLine("         actually belongs in F4's applicability(i)");
    }

    private static void Measure(Placement placement)
    {
        HexCoord[] board = BuildBoard(BoardRadius);
        int[] weights = BuildWeights(board, placement);
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
                    (bool anyChampion, bool anyEnemy) = Evaluate(patterns[p], origin, occupied);
                    if (anyChampion) legal[p]++;
                    if (anyEnemy) useful[p]++;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine(placement == Placement.Uniform
            ? "── UNIFORM placement — champions scattered anywhere ─────────────"
            : "── CONTESTED placement — champions drawn toward 3 objectives ────");
        Console.WriteLine($"{"Pattern",-38} {"Tier",4} {"legal",8} {"useful",8}");
        Console.WriteLine(new string('-', 62));
        for (int p = 0; p < patterns.Length; p++)
        {
            Console.WriteLine($"{patterns[p].Name,-38} {patterns[p].Tier,4} "
                              + $"{(double)legal[p] / samples,8:P1} {(double)useful[p] / samples,8:P1}");
        }
    }

    /// <summary>
    /// Sampling weight per board hex. Contested placement weights a hex by
    /// <c>1 / (1 + d)^2</c> where <c>d</c> is the distance to the nearest objective,
    /// so a champion is roughly nine times likelier to stand on an objective than
    /// two hexes off it.
    /// </summary>
    private static int[] BuildWeights(HexCoord[] board, Placement placement)
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
            foreach (HexCoord o in Objectives)
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
        Pattern pattern, HexCoord origin, Dictionary<HexCoord, int> occupied)
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
                if (!Hex.InBoard(target, BoardRadius)) continue;   // off-board hexes dropped
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
