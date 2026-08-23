using Augury.Sim;

namespace Augury.Tools;

/// <summary>
/// Measures what a radius-1 beacon actually controls, and whether an opponent can still
/// contest a tower without entering the beacon's area.
/// </summary>
/// <remarks>
/// The question that decides whether beacons work: if refusing the zone is free, beacons
/// are decoration; if contesting the objective forces you into the zone, they are
/// oppressive. The answer turns out to depend on whether the beacon sits <b>on</b> the
/// tower or <b>beside</b> it, which makes placement a real decision rather than a formality.
/// </remarks>
public static class BeaconGeometry
{
    private const int Radius = BoardLayout.Radius;
    private const int BeaconRadius = 1;

    private static readonly HexCoord[] Towers =
    [
        new(0, 0), new(0, -2), new(2, -2), new(0, 2), new(-2, 2)
    ];

    public static void Run()
    {
        HexCoord[] play = Board();

        Console.WriteLine();
        Console.WriteLine($"BEACON GEOMETRY — radius {BeaconRadius} on a radius-{Radius} board");
        Console.WriteLine();

        Console.WriteLine("  ZONE SIZE BY PLACEMENT");
        Console.WriteLine("  A beacon near the board edge loses part of its area off-board, so a");
        Console.WriteLine("  beacon planted at home in the opening phase controls less than one");
        Console.WriteLine("  walked into the middle later.");
        Console.WriteLine();
        foreach ((string label, HexCoord at) in new[]
                 {
                     ("board centre  (0,0)", new HexCoord(0, 0)),
                     ("own tower     (0,-2)", new HexCoord(0, -2)),
                     ("lane mouth    (0,-4)", new HexCoord(0, -4)),
                     ("nexus hex     (2,-4)", new HexCoord(2, -4)),
                     ("side corner   (4,-4)", new HexCoord(4, -4))
                 })
        {
            int size = Zone(at).Count(h => Hex.InBoard(h, Radius));
            Console.WriteLine($"    {label,-22} {size} hexes  ({(double)size / play.Length,5:P1} of board)");
        }

        Console.WriteLine();
        Console.WriteLine("  CAN A TOWER BE CONTESTED FROM OUTSIDE THE ZONE?");
        Console.WriteLine("  'approaches' counts a tower's playable neighbours — the hexes you must");
        Console.WriteLine("  stand on to fight for it. 'free' counts those outside the beacon zone.");
        Console.WriteLine();
        Console.WriteLine("    tower      placement            approaches  free  verdict");
        Console.WriteLine("    ─────────  ───────────────────  ──────────  ────  ────────────────────");

        foreach (HexCoord t in Towers)
        {
            HexCoord[] approaches = Neighbours(t).Where(h => Hex.InBoard(h, Radius)).ToArray();

            int freeOn = approaches.Count(a => !InZone(a, t));
            Console.WriteLine($"    {Fmt(t),-9}  beacon ON the tower  {approaches.Length,10}  {freeOn,4}"
                              + $"  {(freeOn == 0 ? "unavoidable" : "avoidable")}");

            // Best the defender can do: the adjacent placement leaving most approaches free.
            int bestFree = -1;
            HexCoord bestSpot = default;
            foreach (HexCoord b in approaches)
            {
                int free = approaches.Count(a => !InZone(a, b)) + (InZone(t, b) ? 0 : 1);
                if (free > bestFree) { bestFree = free; bestSpot = b; }
            }

            Console.WriteLine($"    {"",-9}  beside it {Fmt(bestSpot),-10} {approaches.Length,10}  {bestFree,4}"
                              + $"  {(bestFree == 0 ? "unavoidable" : "avoidable")}");
        }

        HuddleRisk(play);

        Console.WriteLine();
        Draw(play, new HexCoord(0, 0), "Beacon ON the centre tower — every approach is inside");
        Draw(play, new HexCoord(0, 1), "Beacon BESIDE it — the far approaches stay open");

        Console.WriteLine("  READING IT");
        Console.WriteLine("    On the tower  → contesting the objective forces the enemy into your");
        Console.WriteLine("                    combo zone. Strongest, and utterly obvious.");
        Console.WriteLine("    Beside it     → they can approach from the far side and stay clear, so");
        Console.WriteLine("                    you are choosing WHICH approach to make expensive.");
        Console.WriteLine("    Either way the refusal is never free: the ground given up is the");
        Console.WriteLine("    ground that ticks score.");
        Console.WriteLine();
    }

    /// <summary>
    /// Prices the cost of a slot+slot chain: both champions must stand inside one 7-hex
    /// zone, which is very close to the footprint of a fixed tier-4 pattern.
    /// </summary>
    private static void HuddleRisk(HexCoord[] play)
    {
        // Fixed (non-rotatable) 5-hex tier-4 patterns. Shapes are illustrative — the
        // Ability Definition Schema owns the real ones; 5 hexes is the measured target.
        (string Name, HexCoord[] Cells)[] patterns =
        [
            ("line",  [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)]),
            ("blob",  [new(0, 0), new(1, 0), new(1, -1), new(0, -1), new(-1, 0)]),
            ("wedge", [new(0, 0), new(1, 0), new(2, 0), new(1, -1), new(2, -1)])
        ];

        HexCoord[] zone = Zone(new HexCoord(0, 0)).ToArray();
        var zonePairs = Pairs(zone).ToArray();
        var boardPairs = Pairs(play).ToArray();

        Console.WriteLine();
        Console.WriteLine("  THE HUDDLE TAX — what a slot+slot chain costs");
        Console.WriteLine("  A printed-sigil chain lets both champions stand anywhere. A slot+slot");
        Console.WriteLine("  chain forces both inside one 7-hex zone. A fixed tier-4 pattern is 5");
        Console.WriteLine("  hexes. So: how much easier are they to catch, and is anywhere safe?");
        Console.WriteLine();
        Console.WriteLine("    tier-4 shape   pairs in zone caught   pairs anywhere caught   safe spots");
        Console.WriteLine("    ─────────────  ────────────────────   ─────────────────────   ──────────");

        foreach ((string name, HexCoord[] cells) in patterns)
        {
            int inZone = zonePairs.Count(p => Catchable(p.A, p.B, cells));
            int anywhere = boardPairs.Count(p => Catchable(p.A, p.B, cells));
            int safe = zonePairs.Length - inZone;
            Console.WriteLine($"    {name,-13}  {inZone,6} / {zonePairs.Length,-3} {(double)inZone / zonePairs.Length,8:P0}"
                              + $"   {anywhere,6} / {boardPairs.Length,-4} {(double)anywhere / boardPairs.Length,7:P0}"
                              + $"   {safe,4} of {zonePairs.Length}");
        }

        Console.WriteLine();
        Console.WriteLine("    A pair standing anywhere on the board is rarely catchable, because most");
        Console.WriteLine("    pairs are simply far apart. Inside a beacon zone they never are — which");
        Console.WriteLine("    is the price of manufacturing a chain, and it is paid in exposure.");
        Console.WriteLine("    'safe spots' says whether positioning still matters INSIDE the zone.");
    }

    private static IEnumerable<(HexCoord A, HexCoord B)> Pairs(HexCoord[] hexes)
    {
        for (int i = 0; i < hexes.Length; i++)
        {
            for (int j = i + 1; j < hexes.Length; j++) yield return (hexes[i], hexes[j]);
        }
    }

    /// <summary>True when some translation of the fixed pattern covers both hexes.</summary>
    private static bool Catchable(HexCoord a, HexCoord b, HexCoord[] cells)
    {
        foreach (HexCoord anchor in cells)
        {
            // Place the pattern so that 'anchor' lands on 'a', then test for 'b'.
            HexCoord offset = a - anchor;
            if (cells.Any(c => c + offset == b)) return true;
        }

        return false;
    }

    private static bool InZone(HexCoord h, HexCoord beacon) =>
        HexCoord.Distance(h, beacon) <= BeaconRadius;

    private static IEnumerable<HexCoord> Zone(HexCoord centre)
    {
        yield return centre;
        foreach (HexCoord n in Neighbours(centre)) yield return n;
    }

    private static IEnumerable<HexCoord> Neighbours(HexCoord h)
    {
        foreach (HexCoord d in Hex.Directions.ToArray()) yield return h + d;
    }

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

    private static void Draw(HexCoord[] play, HexCoord beacon, string caption)
    {
        Console.WriteLine($"  {caption}");
        Console.WriteLine();
        for (int r = -Radius; r <= Radius; r++)
        {
            var line = new char[4 * Radius + 2];
            Array.Fill(line, ' ');
            foreach (HexCoord h in play.Where(h => h.R == r))
            {
                char g = h == beacon ? 'B'
                       : InZone(h, beacon) ? 'o'
                       : Towers.Contains(h) ? 'T'
                       : '·';
                line[2 * h.Q + h.R + 2 * Radius] = g;
            }

            Console.WriteLine("    " + new string(line).TrimEnd());
        }

        Console.WriteLine();
        Console.WriteLine("      B beacon   o in zone   T tower   · outside");
        Console.WriteLine();
    }
}
