using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Renders candidate board layouts so a board can be judged by looking at it
/// rather than by arguing about hex counts.
/// </summary>
/// <remarks>
/// <para>Two candidates are rendered because the choice between them is the whole
/// board design. <b>Laned</b> imports MOBA topology directly: two bases at opposite
/// corners, a mid route on the diagonal, flank routes, jungle in the leftovers.
/// <b>Open</b> drops routes entirely and keeps only what was actually asked for —
/// symmetry, towers, and jungle.</para>
/// <para>Symmetry is 180-degree rotation about the origin — <c>(q,r) → (-q,-r)</c>.
/// Mirror symmetry is rejected: a mirrored tier-4 pattern is a chirally different
/// shape that the six-facing rotation system cannot express, so the two teams would
/// not have access to the same abilities. Rotation preserves chirality; mirroring
/// does not.</para>
/// </remarks>
public static class BoardLayout
{
    /// <summary>What a hex is for.</summary>
    private enum Zone
    {
        Open,
        Jungle,
        Flank,
        Mid,
        Tower,
        Base
    }

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("CANDIDATE A — LANED (MOBA topology imported directly)");
        Render(4, laned: true);
        Render(6, laned: true);

        Console.WriteLine();
        Console.WriteLine("CANDIDATE B — OPEN (symmetry + towers + jungle, no routes)");
        Render(4, laned: false);
        Render(5, laned: false);

        Console.WriteLine();
        Console.WriteLine("  B base   T tower   = mid route   - flank route   . jungle   · open");
    }

    private static void Render(int radius, bool laned)
    {
        HexCoord[] board = BuildBoard(radius);
        var zones = new Dictionary<HexCoord, Zone>();

        foreach (HexCoord h in board)
        {
            zones[h] = laned ? LanedZone(h, radius) : OpenZone(h);
        }

        foreach (HexCoord t in TowerSites(radius, laned))
        {
            zones[t] = Zone.Tower;
            zones[new HexCoord(-t.Q, -t.R)] = Zone.Tower;
        }

        if (laned) zones[new HexCoord(0, 0)] = Zone.Mid;
        else zones[new HexCoord(0, 0)] = Zone.Tower;

        zones[new HexCoord(radius, -radius)] = Zone.Base;
        zones[new HexCoord(-radius, radius)] = Zone.Base;

        int asymmetric = board.Count(h => zones[h] != zones[new HexCoord(-h.Q, -h.R)]);

        var counts = new Dictionary<Zone, int>();
        foreach (Zone z in zones.Values)
        {
            counts[z] = counts.GetValueOrDefault(z) + 1;
        }

        Console.WriteLine();
        Console.WriteLine($"  ─── radius {radius} · {board.Length} hexes · "
                          + $"{(double)board.Length / 10:F1} per champion ───");
        Console.WriteLine();

        for (int r = -radius; r <= radius; r++)
        {
            var line = new char[4 * radius + 2];
            Array.Fill(line, ' ');
            for (int q = -radius; q <= radius; q++)
            {
                var h = new HexCoord(q, r);
                if (!Hex.InBoard(h, radius)) continue;
                line[2 * q + r + 2 * radius] = Glyph(zones[h]);
            }

            Console.WriteLine("   " + new string(line).TrimEnd());
        }

        int widest = WidestCorridor(zones, radius);
        Console.WriteLine();
        Console.WriteLine($"   jungle {counts.GetValueOrDefault(Zone.Jungle),3} "
                          + $"({(double)counts.GetValueOrDefault(Zone.Jungle) / board.Length,3:P0})"
                          + $"   towers {counts.GetValueOrDefault(Zone.Tower),2}"
                          + $"   asymmetry {asymmetric}"
                          + $"   narrowest route {widest} hex wide");
    }

    /// <summary>
    /// Laned zoning. Mid is 3 wide because it must be symmetric in <c>S</c>, and
    /// 180-degree rotation maps <c>S</c> to <c>-S</c> — so a 2-wide mid is
    /// geometrically impossible on a rotationally symmetric board.
    /// </summary>
    private static Zone LanedZone(HexCoord h, int radius)
    {
        if (h.Magnitude == radius) return Zone.Flank;
        return Math.Abs(h.S) <= 1 ? Zone.Mid : Zone.Jungle;
    }

    /// <summary>
    /// Open zoning. Jungle is the two deep pockets either side of the base-to-base
    /// diagonal; everything else is open ground. Rotation maps <c>S</c> to
    /// <c>-S</c>, so a rule on <c>|S|</c> is symmetric by construction.
    /// </summary>
    private static Zone OpenZone(HexCoord h) => Math.Abs(h.S) >= 3 ? Zone.Jungle : Zone.Open;

    private static HexCoord[] TowerSites(int radius, bool laned)
    {
        int d = Math.Max(1, radius / 2);
        return laned
            ? [new HexCoord(d + 1, -(d + 1)), new HexCoord(radius, -d)]
            : [new HexCoord(d + 1, -1), new HexCoord(1, -(d + 1))];
    }

    /// <summary>
    /// The narrowest point of any traversable corridor, in hexes. A corridor 1 hex
    /// wide cannot hold two champions abreast — one is always behind the other,
    /// which is the formation problem that prompted this whole layout question.
    /// </summary>
    private static int WidestCorridor(Dictionary<HexCoord, Zone> zones, int radius)
    {
        int narrowest = int.MaxValue;
        for (int s = -radius; s <= radius; s++)
        {
            int run = 0, best = 0;
            for (int q = -radius; q <= radius; q++)
            {
                var h = new HexCoord(q, -q - s);
                bool passable = Hex.InBoard(h, radius) && zones.TryGetValue(h, out Zone z)
                                && z != Zone.Jungle;
                run = passable ? run + 1 : 0;
                best = Math.Max(best, run);
            }

            if (best > 0) narrowest = Math.Min(narrowest, best);
        }

        return narrowest == int.MaxValue ? 0 : narrowest;
    }

    private static char Glyph(Zone zone) => zone switch
    {
        Zone.Base => 'B',
        Zone.Tower => 'T',
        Zone.Mid => '=',
        Zone.Flank => '-',
        Zone.Jungle => '.',
        _ => '·'
    };

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
}
