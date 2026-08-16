using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Renders the AUGURY board and asserts its geometric invariants.
/// </summary>
/// <remarks>
/// <para>Symmetry is 180-degree rotation about the origin — <c>(q,r) → (-q,-r)</c>.
/// Mirror symmetry is rejected: a mirrored tier-4 pattern is a chirally different
/// shape that the six-facing rotation system cannot express, so the two teams would
/// not have access to the same abilities. Rotation preserves chirality; mirroring
/// does not.</para>
/// <para>Routes were considered and rejected. On a hexagon the outer edge is a
/// single ring, so any edge-hugging lane is one hex wide at every board radius —
/// which forces two champions sharing a lane into single file. That formation
/// problem is created by lanes, not by one-champion-per-hex occupancy.</para>
/// </remarks>
public static class BoardLayout
{
    /// <summary>Board radius in hexes. 61 hexes total, 6.1 per champion.</summary>
    public const int Radius = 4;

    /// <summary>Jungle is the two deep wedges either side of the base diagonal.</summary>
    public const int JungleDepth = 3;

    /// <summary>What a hex is for.</summary>
    private enum Zone
    {
        Open,
        Jungle,
        Tower,
        Base
    }

    private static readonly HexCoord CentreTower = new(0, 0);

    /// <summary>Team A's two towers. Team B's are the antipodes of these.</summary>
    private static readonly HexCoord[] TeamATowers = { new(3, -1), new(1, -3) };

    private static readonly HexCoord BaseA = new(Radius, -Radius);

    public static void Run()
    {
        HexCoord[] board = BuildBoard(Radius);
        Dictionary<HexCoord, Zone> zones = Zones(board);

        Console.WriteLine();
        Console.WriteLine($"AUGURY BOARD — radius {Radius}, {board.Length} hexes, "
                          + $"{(double)board.Length / 10:F1} per champion");
        Console.WriteLine();

        for (int r = -Radius; r <= Radius; r++)
        {
            var line = new char[4 * Radius + 2];
            Array.Fill(line, ' ');
            for (int q = -Radius; q <= Radius; q++)
            {
                var h = new HexCoord(q, r);
                if (!Hex.InBoard(h, Radius)) continue;
                line[2 * q + r + 2 * Radius] = Glyph(zones[h]);
            }

            Console.WriteLine("    " + new string(line).TrimEnd());
        }

        Console.WriteLine();
        Console.WriteLine("    B base    T tower    . jungle    · open ground");
        Console.WriteLine();

        var counts = new Dictionary<Zone, int>();
        foreach (Zone z in zones.Values) counts[z] = counts.GetValueOrDefault(z) + 1;

        Console.WriteLine($"    open {counts.GetValueOrDefault(Zone.Open),3}"
                          + $"    jungle {counts.GetValueOrDefault(Zone.Jungle),3}"
                          + $" ({(double)counts.GetValueOrDefault(Zone.Jungle) / board.Length,3:P0})"
                          + $"    towers {counts.GetValueOrDefault(Zone.Tower),2}"
                          + $"    bases {counts.GetValueOrDefault(Zone.Base),2}");
        Console.WriteLine();

        Invariants(board, zones);
    }

    private static Dictionary<HexCoord, Zone> Zones(HexCoord[] board)
    {
        var zones = new Dictionary<HexCoord, Zone>();
        foreach (HexCoord h in board)
        {
            zones[h] = Math.Abs(h.S) >= JungleDepth ? Zone.Jungle : Zone.Open;
        }

        zones[CentreTower] = Zone.Tower;
        foreach (HexCoord t in TeamATowers)
        {
            zones[t] = Zone.Tower;
            zones[Antipode(t)] = Zone.Tower;
        }

        zones[BaseA] = Zone.Base;
        zones[Antipode(BaseA)] = Zone.Base;
        return zones;
    }

    private static void Invariants(HexCoord[] board, Dictionary<HexCoord, Zone> zones)
    {
        Console.WriteLine("    INVARIANTS");

        int asym = board.Count(h => zones[h] != zones[Antipode(h)]);
        Report("180-degree rotational symmetry", asym == 0, $"{asym} mismatched hexes");

        bool towersEquidistant = TeamATowers.All(t =>
            HexCoord.Distance(BaseA, t) == HexCoord.Distance(Antipode(BaseA), Antipode(t)));
        Report("each team's towers equidistant from its own base", towersEquidistant,
               $"{string.Join(", ", TeamATowers.Select(t => HexCoord.Distance(BaseA, t)))} hexes");

        int dA = HexCoord.Distance(BaseA, CentreTower);
        int dB = HexCoord.Distance(Antipode(BaseA), CentreTower);
        Report("centre tower equidistant from both bases", dA == dB, $"{dA} hexes each");

        bool towersOpen = TeamATowers.Append(CentreTower)
            .All(t => Math.Abs(t.S) < JungleDepth);
        Report("no tower sits inside jungle", towersOpen, "towers are contestable in the open");

        // Two champions must be able to stand abreast anywhere they can walk, or the
        // single-file problem returns. Every open hex needs an open neighbour.
        HexCoord[] open = board.Where(h => zones[h] != Zone.Jungle).ToArray();
        bool abreast = open.All(h => Hex.Directions.ToArray()
            .Any(d => zones.TryGetValue(h + d, out Zone z) && z != Zone.Jungle));
        Report("every walkable hex has a walkable neighbour (no single file)", abreast,
               "champions can always pair up");

        int span = HexCoord.Distance(BaseA, Antipode(BaseA));
        Console.WriteLine($"      · base-to-base distance: {span} hexes "
                          + $"(respawn walk-back; Movement & Targeting sets the round cost)");
    }

    private static void Report(string name, bool ok, string detail)
        => Console.WriteLine($"      {(ok ? "PASS" : "FAIL")}  {name} — {detail}");

    private static HexCoord Antipode(HexCoord h) => new(-h.Q, -h.R);

    private static char Glyph(Zone zone) => zone switch
    {
        Zone.Base => 'B',
        Zone.Tower => 'T',
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
