using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Renders the AUGURY board and asserts its geometric invariants.
/// </summary>
/// <remarks>
/// <para>Teams face each other across opposite <b>edges</b> of the hexagon, not
/// opposite corners. A radius-4 edge is exactly five hexes — one per champion — and
/// an edge has eight off-board neighbours to hold a spawn row, where a corner has
/// only three. A corner would also funnel five respawning champions through two or
/// three hexes, reintroducing the single-file problem that removing lanes solved.</para>
/// <para>Symmetry is 180-degree rotation about the origin — <c>(q,r) → (-q,-r)</c>.
/// Mirror symmetry is rejected: a mirrored tier-4 pattern is a chirally different
/// shape that the six-facing rotation system cannot express, so the two teams would
/// not have access to the same abilities.</para>
/// <para>Coordinates are read as <b>rank</b> <c>R</c> (toward the enemy edge) and
/// <b>file</b> <c>Q - S</c> (across the board). File is zero along the axis joining
/// the two edge midpoints and reaches +/-8 at the side corners; both negate under
/// the symmetry map, so every zone rule is written on the absolute value.</para>
/// </remarks>
public static class BoardLayout
{
    /// <summary>Board radius in hexes. 61 playable hexes, 6.1 per champion.</summary>
    public const int Radius = 4;

    /// <summary>Jungle is every playable hex with <c>|file|</c> at or beyond this.</summary>
    public const int JungleFile = 5;

    /// <summary>What a hex is for.</summary>
    private enum Zone
    {
        Open,
        Jungle,
        Tower,
        Front,
        Spawn
    }

    private static readonly HexCoord CentreTower = new(0, 0);

    /// <summary>Team A's towers. Team B's are the antipodes.</summary>
    private static readonly HexCoord[] TeamATowers = { new(0, -2), new(2, -2) };

    public static void Run()
    {
        HexCoord[] play = BuildBoard(Radius);
        HexCoord[] spawn = SpawnRow().Concat(SpawnRow().Select(Antipode)).ToArray();
        Dictionary<HexCoord, Zone> zones = Zones(play, spawn);

        Console.WriteLine();
        Console.WriteLine($"AUGURY BOARD — radius {Radius}, {play.Length} playable hexes "
                          + $"({(double)play.Length / 10:F1} per champion) "
                          + $"+ {spawn.Length} off-board spawn hexes");
        Console.WriteLine();

        for (int r = -Radius - 1; r <= Radius + 1; r++)
        {
            var line = new char[4 * Radius + 4];
            Array.Fill(line, ' ');
            for (int q = -Radius - 1; q <= Radius + 1; q++)
            {
                var h = new HexCoord(q, r);
                if (!zones.TryGetValue(h, out Zone z)) continue;
                line[2 * q + r + 2 * Radius] = Glyph(z);
            }

            Console.WriteLine("    " + new string(line).TrimEnd());
        }

        Console.WriteLine();
        Console.WriteLine("    S spawn (off-board)   F front line   T tower   . jungle   · open");
        Console.WriteLine();

        var counts = new Dictionary<Zone, int>();
        foreach (Zone z in zones.Values) counts[z] = counts.GetValueOrDefault(z) + 1;
        Console.WriteLine($"    open {counts.GetValueOrDefault(Zone.Open),3}"
                          + $"   jungle {counts.GetValueOrDefault(Zone.Jungle),3}"
                          + $" ({(double)counts.GetValueOrDefault(Zone.Jungle) / play.Length,3:P0} of play)"
                          + $"   front {counts.GetValueOrDefault(Zone.Front),3}"
                          + $"   towers {counts.GetValueOrDefault(Zone.Tower),2}"
                          + $"   spawn {counts.GetValueOrDefault(Zone.Spawn),2}");
        Console.WriteLine();

        Invariants(play, spawn, zones);
    }

    /// <summary>Team A's spawn row: the off-board hexes behind its front edge.</summary>
    private static HexCoord[] SpawnRow()
    {
        var row = new List<HexCoord>();
        for (int q = 0; q <= Radius + 1; q++) row.Add(new HexCoord(q, -Radius - 1));
        return row.ToArray();
    }

    private static Dictionary<HexCoord, Zone> Zones(HexCoord[] play, HexCoord[] spawn)
    {
        var zones = new Dictionary<HexCoord, Zone>();
        foreach (HexCoord h in play)
        {
            zones[h] = Math.Abs(File(h)) >= JungleFile ? Zone.Jungle : Zone.Open;
        }

        foreach (HexCoord h in play)
        {
            if (Math.Abs(h.R) == Radius) zones[h] = Zone.Front;
        }

        zones[CentreTower] = Zone.Tower;
        foreach (HexCoord t in TeamATowers)
        {
            zones[t] = Zone.Tower;
            zones[Antipode(t)] = Zone.Tower;
        }

        foreach (HexCoord h in spawn) zones[h] = Zone.Spawn;
        return zones;
    }

    /// <summary>Position across the board. Zero on the centre axis, +/-8 at the sides.</summary>
    private static int File(HexCoord h) => h.Q - h.S;

    private static void Invariants(HexCoord[] play, HexCoord[] spawn,
                                   Dictionary<HexCoord, Zone> zones)
    {
        Console.WriteLine("    INVARIANTS");

        int asym = play.Concat(spawn).Count(h => zones[h] != zones[Antipode(h)]);
        Report("180-degree rotational symmetry, spawn rows included", asym == 0,
               $"{asym} mismatched hexes");

        HexCoord[] frontA = play.Where(h => h.R == -Radius).ToArray();
        Report("front edge holds exactly one hex per champion", frontA.Length == 5,
               $"{frontA.Length} hexes for 5 champions");

        HexCoord[] rowA = SpawnRow();
        Report("spawn row seats every champion with a spare", rowA.Length >= 5,
               $"{rowA.Length} hexes — 5 champions, jungler takes two");

        bool allAdjacent = rowA.All(s => Hex.Directions.ToArray()
            .Any(d => Hex.InBoard(s + d, Radius)));
        Report("every spawn hex touches the playable board", allAdjacent,
               "no champion is stranded off-board");

        bool spawnOffBoard = spawn.All(s => !Hex.InBoard(s, Radius));
        Report("no spawn hex is playable", spawnOffBoard,
               "spawn rows cost zero playable hexes, so density is unchanged");

        bool towersOpen = TeamATowers.Append(CentreTower)
            .All(t => Math.Abs(File(t)) < JungleFile);
        Report("no tower sits inside jungle", towersOpen, "every tower is contestable");

        int[] fromFront = TeamATowers
            .Select(t => frontA.Min(f => HexCoord.Distance(f, t))).ToArray();
        Report("both of a team's towers equidistant from its own front",
               fromFront.Distinct().Count() == 1, $"{string.Join(", ", fromFront)} hexes");

        int dA = play.Where(h => h.R == -Radius).Min(h => HexCoord.Distance(h, CentreTower));
        int dB = play.Where(h => h.R == Radius).Min(h => HexCoord.Distance(h, CentreTower));
        Report("centre tower equidistant from both fronts", dA == dB, $"{dA} hexes each");

        HexCoord[] walkable = play.Where(h => zones[h] != Zone.Jungle).ToArray();
        bool abreast = walkable.All(h => Hex.Directions.ToArray()
            .Any(d => zones.TryGetValue(h + d, out Zone z)
                      && z != Zone.Jungle && z != Zone.Spawn));
        Report("every walkable hex has a walkable neighbour (no single file)", abreast,
               "champions can always stand abreast");

        int span = play.Where(h => h.R == -Radius)
            .Min(a => play.Where(h => h.R == Radius).Min(b => HexCoord.Distance(a, b)));
        Console.WriteLine($"      · front-to-front distance: {span} hexes "
                          + "(Movement & Targeting sets the round cost)");
    }

    private static void Report(string name, bool ok, string detail)
        => Console.WriteLine($"      {(ok ? "PASS" : "FAIL")}  {name} — {detail}");

    private static HexCoord Antipode(HexCoord h) => new(-h.Q, -h.R);

    private static char Glyph(Zone zone) => zone switch
    {
        Zone.Spawn => 'S',
        Zone.Front => 'F',
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
