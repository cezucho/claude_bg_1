using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>Fixed board geometry from Map &amp; Terrain and the Opening Phase.</summary>
public static class Board
{
    /// <summary>Board radius: 61 playable hexes.</summary>
    public const int Radius = 4;

    /// <summary>Tower positions: 0 centre, 1–2 team A's home towers, 3–4 team B's.</summary>
    public static readonly HexCoord[] TowerHexes =
    [
        new(0, 0), new(0, -2), new(2, -2), new(0, 2), new(-2, 2)
    ];

    /// <summary>Team A's nexus hexes; team B's are their mirror images.</summary>
    public static readonly HexCoord[] NexusA = [new(1, -4), new(2, -4), new(3, -4)];

    /// <summary>True when on the playable board.</summary>
    public static bool Playable(HexCoord h) => Hex.InBoard(h, Radius);

    /// <summary>
    /// Reorients a canonical-frame hex or offset for a team: identity for A, the mirror
    /// for B (ADR-0005, second amendment). Each role starts opposite the same role.
    /// </summary>
    public static HexCoord Frame(HexCoord offset, Team team) => Hex.ForForward(offset, team == Team.A);

    /// <summary>
    /// Index into <see cref="Hex.Directions"/> of direction <paramref name="k"/> in a team's
    /// frame. Anything that iterates directions, and could break a tie by iteration order,
    /// iterates in the acting team's frame so the tie breaks the same way for both teams.
    /// </summary>
    public static int FrameDirection(int k, Team team) => team == Team.B ? (6 - k) % 6 : k;

    /// <summary>
    /// All playable hexes in a team's frame: team A's order, mirrored for team B. Used where
    /// enumeration order could break a tie.
    /// </summary>
    public static HexCoord[] HexesInFrame(Team team) => team == Team.B ? AllHexesMirrored : AllHexes;

    /// <summary>Start hex for a role on the front line.</summary>
    public static HexCoord StartHex(Team team, Role role) => Frame(new HexCoord((int)role, -Radius), team);

    /// <summary>Off-board spawn hex behind the role's start hex.</summary>
    public static HexCoord SpawnHex(Team team, Role role) => Frame(new HexCoord((int)role, -Radius - 1), team);

    /// <summary>A team's three nexus hexes.</summary>
    public static IEnumerable<HexCoord> NexusHexes(Team team) =>
        NexusA.Select(h => Frame(h, team));

    /// <summary>True when the hex is one of a team's nexus hexes.</summary>
    public static bool IsNexusHex(HexCoord h, Team team)
    {
        foreach (HexCoord n in NexusA)
        {
            if (Frame(n, team) == h) return true;
        }

        return false;
    }

    /// <summary>Tower index at a hex, or −1.</summary>
    public static int TowerAt(HexCoord h) => Array.IndexOf(TowerHexes, h);

    /// <summary>Across-board reading: 0 on the centre axis, ±8 at the side corners.</summary>
    public static int File(HexCoord h) => 2 * h.Q + h.R;

    /// <summary>All playable hexes, in a fixed order.</summary>
    public static readonly HexCoord[] AllHexes = Build();

    private static readonly HexCoord[] AllHexesMirrored = AllHexes.Select(Hex.Mirror).ToArray();

    private static HexCoord[] Build()
    {
        var list = new List<HexCoord>();
        for (int r = -Radius; r <= Radius; r++)
        {
            for (int q = -Radius; q <= Radius; q++)
            {
                var h = new HexCoord(q, r);
                if (Playable(h)) list.Add(h);
            }
        }

        return list.ToArray();
    }
}
