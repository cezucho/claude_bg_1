using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Prices movement under the ladder's action economy, to settle Initiative Ladder
/// open question 3: does a move cost the champion's action for the half?
/// </summary>
/// <remarks>
/// A champion acts once per half and twice per round. If a move consumes that action,
/// then a champion crossing the board contributes nothing while it walks, and its
/// <b>threat range is exactly RCH</b> — never RCH + SPD, because it cannot move and
/// strike in the same half. That is the trade being priced here.
/// </remarks>
public static class MobilityCost
{
    private const int Radius = BoardLayout.Radius;
    private const int FrontToFront = 2 * Radius;

    /// <summary>⚠ Assumed match length, from the Objectives discussion.</summary>
    private const int MatchRounds = 16;

    private static readonly (string Name, HexCoord At)[] Landmarks =
    [
        ("own spawn hex", new HexCoord(0, -Radius - 1)),
        ("own front line", new HexCoord(0, -Radius)),
        ("own tower", new HexCoord(0, -2)),
        ("centre tower", new HexCoord(0, 0)),
        ("enemy tower", new HexCoord(0, 2)),
        ("enemy nexus", new HexCoord(-2, Radius))
    ];

    public static void Run()
    {
        HexCoord[] play = Board();

        Console.WriteLine();
        Console.WriteLine("MOBILITY COST — pricing movement against one action per half");
        Console.WriteLine($"Board radius {Radius}, {play.Length} hexes, front-to-front {FrontToFront}.");
        Console.WriteLine($"A champion acts once per half, twice per round. ⚠ {MatchRounds}-round match.");
        Console.WriteLine();

        Console.WriteLine("  IF A MOVE COSTS THE CHAMPION'S ACTION");
        Console.WriteLine();
        Console.WriteLine("    SPD  reach/move  cross board   as rounds   share of a match");
        Console.WriteLine("    ───  ──────────  ───────────   ─────────   ────────────────");
        foreach (int spd in new[] { 1, 2, 3, 4 })
        {
            int moves = Ceil(FrontToFront, spd);
            double rounds = moves / 2.0;
            Console.WriteLine($"    {spd,3}  {spd,4} hexes  {moves,6} moves {rounds,10:F1}"
                              + $"   {rounds / MatchRounds,15:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("    At the schema default SPD 2, crossing the board costs FOUR actions —");
        Console.WriteLine("    two full rounds in which that champion does nothing else at all.");
        Console.WriteLine();

        Console.WriteLine("  THE WALK BACK FROM DEATH (from the spawn hex, +1 action to enter play)");
        Console.WriteLine();
        Console.WriteLine("    destination        hexes   SPD1    SPD2    SPD3    SPD4");
        Console.WriteLine("    ─────────────────  ─────   ────    ────    ────    ────");
        HexCoord spawn = Landmarks[0].At;
        foreach ((string name, HexCoord at) in Landmarks.Skip(1))
        {
            int d = HexCoord.Distance(spawn, at);
            string cells = string.Join("", new[] { 1, 2, 3, 4 }
                .Select(spd => $"{Ceil(d, spd) + 1,6} "));
            Console.WriteLine($"    {name,-17}  {d,5}  {cells}");
        }

        Console.WriteLine();
        Console.WriteLine("    Read as actions, including the one spent entering play. At SPD 2 a dead");
        Console.WriteLine("    champion needs 4 actions — two rounds — to rejoin a fight at the centre,");
        Console.WriteLine("    on top of whatever the respawn timer already cost.");
        Console.WriteLine();

        Console.WriteLine("  WHAT ONE CHAMPION COVERS");
        Console.WriteLine("  Threat is RCH alone, because a champion cannot move and strike in one half.");
        Console.WriteLine("  Reach is where it could stand next half, having done nothing this half.");
        Console.WriteLine();
        Console.WriteLine("    value   from board centre       averaged over every hex");
        Console.WriteLine("    ─────   ────────────────────    ───────────────────────");
        foreach (int v in new[] { 1, 2, 3, 4 })
        {
            int centre = play.Count(h => HexCoord.Distance(h, new HexCoord(0, 0)) <= v);
            double mean = play.Average(o => play.Count(h => HexCoord.Distance(h, o) <= v));
            Console.WriteLine($"    {v,5}   {centre,6} hexes {(double)centre / play.Length,7:P1}"
                              + $"    {mean,10:F1} hexes {mean / play.Length,7:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("    RCH 4 covers the WHOLE board from the centre, and 63% of it on average.");
        Console.WriteLine("    A radius-4 board cannot express a reach of 4 as a meaningful limit —");
        Console.WriteLine("    a champion holding the middle with RCH 4 threatens every hex there is.");

        TwoEconomies();

        Console.WriteLine();
        Console.WriteLine("  DO BODIES BLOCK? — the cost of path-based movement");
        Console.WriteLine("  With one champion per hex and no impassable terrain, champions are the");
        Console.WriteLine("  only obstacles the board can have. This is what a 5-champion wall does.");
        Console.WriteLine();
        BlockingWall(play);
        Console.WriteLine();
    }

    /// <summary>
    /// Compares the abandoned economy (every champion may act each half) against the
    /// adopted one (the TEAM takes 2 basic actions per half, abilities run separately).
    /// </summary>
    private static void TwoEconomies()
    {
        const int BasicsPerHalf = 2;
        const int Team = 5;
        int basicsPerRound = BasicsPerHalf * 2;

        Console.WriteLine();
        Console.WriteLine("  TWO ECONOMIES COMPARED");
        Console.WriteLine("  OLD: a move consumed a champion's one action, so all 5 could move each half.");
        Console.WriteLine($"  NEW: the TEAM gets {BasicsPerHalf} basic actions per half"
                          + $" ({basicsPerRound} per round), whoever uses them.");
        Console.WriteLine();
        Console.WriteLine("    SPD   task                              old        new");
        Console.WriteLine("    ───   ────────────────────────────────  ─────────  ─────────");

        foreach (int spd in new[] { 2, 3 })
        {
            int crossMoves = Ceil(FrontToFront, spd);

            // One champion crossing: old = 1 move per half. New = may take every basic.
            double oldCross = crossMoves / 2.0;
            double newCross = (double)crossMoves / basicsPerRound;
            Console.WriteLine($"    {spd,3}   one champion crosses the board     "
                              + $"{oldCross,4:F1} rds  {newCross,4:F1} rds");

            // Whole team repositions one step each.
            double oldTeam = 0.5;
            double newTeam = (double)Team / basicsPerRound;
            Console.WriteLine($"    {spd,3}   all 5 champions move once          "
                              + $"{oldTeam,4:F1} rds  {newTeam,4:F1} rds");
        }

        Console.WriteLine();
        Console.WriteLine($"    A champion now moves {(double)basicsPerRound / Team:F1} times per round on average,"
                          + $" against 2.0 before.");
        Console.WriteLine("    The economy inverts: ONE champion can cross the board faster than before");
        Console.WriteLine("    by eating the whole budget, but the TEAM repositions far more slowly.");
        Console.WriteLine("    Fast or broad — never both. That is the decision the budget creates.");
    }

    /// <summary>
    /// A line of five champions across the board's waist, and what it costs to get past
    /// if movement must trace a path of free hexes rather than teleport within range.
    /// </summary>
    private static void BlockingWall(HexCoord[] play)
    {
        // Five champions across the middle rank, the widest a team can span.
        var wall = new HashSet<HexCoord>(
            play.Where(h => h.R == 0 && Math.Abs(h.Q) <= 2));

        HexCoord from = new(0, -3);
        HexCoord to = new(0, 3);

        int free = HexCoord.Distance(from, to);
        int walled = PathLength(play, wall, from, to);

        Console.WriteLine($"    wall of {wall.Count} champions on rank 0, from {Fmt(from)} to {Fmt(to)}:");
        Console.WriteLine($"      open board          {free} hexes");
        Console.WriteLine($"      around the wall     {(walled < 0 ? "unreachable" : walled + " hexes")}"
                          + $"   (+{walled - free} detour)");
        Console.WriteLine();
        Console.WriteLine("    A wall never seals the board — the hexagon is too wide to span with");
        Console.WriteLine("    five bodies — but it does tax the crossing, and the tax is paid in the");
        Console.WriteLine("    same currency as everything else: actions.");
    }

    /// <summary>Breadth-first shortest path avoiding blocked hexes. −1 when unreachable.</summary>
    private static int PathLength(HexCoord[] play, HashSet<HexCoord> blocked,
                                  HexCoord from, HexCoord to)
    {
        var open = new HashSet<HexCoord>(play.Where(h => !blocked.Contains(h)));
        var seen = new HashSet<HexCoord> { from };
        var frontier = new Queue<(HexCoord At, int Steps)>();
        frontier.Enqueue((from, 0));

        while (frontier.Count > 0)
        {
            (HexCoord at, int steps) = frontier.Dequeue();
            if (at == to) return steps;
            foreach (HexCoord d in Hex.Directions.ToArray())
            {
                HexCoord next = at + d;
                if (!open.Contains(next) || !seen.Add(next)) continue;
                frontier.Enqueue((next, steps + 1));
            }
        }

        return -1;
    }

    private static int Ceil(int distance, int per) => (distance + per - 1) / per;

    private static string Fmt(HexCoord h) => $"({h.Q},{h.R})";

    private static HexCoord[] Board()
    {
        var hexes = new List<HexCoord>();
        for (int q = -Radius; q <= Radius; q++)
        {
            for (int r = -Radius; r <= Radius; r++)
            {
                var h = new HexCoord(q, r);
                if (Hex.InBoard(h, Radius)) hexes.Add(h);
            }
        }

        return hexes.ToArray();
    }
}
