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

    /// <summary>Team A's nexus hexes; team B's are the half-turns.</summary>
    public static readonly HexCoord[] NexusA = [new(1, -4), new(2, -4), new(3, -4)];

    /// <summary>True when on the playable board.</summary>
    public static bool Playable(HexCoord h) => Hex.InBoard(h, Radius);

    /// <summary>Reorients a canonical-frame offset for a team (ADR-0005, amended).</summary>
    public static HexCoord Frame(HexCoord offset, Team team) => Hex.ForForward(offset, team == Team.A);

    /// <summary>Start hex for a role on the front line.</summary>
    public static HexCoord StartHex(Team team, Role role) => Frame(new HexCoord((int)role, -Radius), team);

    /// <summary>Off-board spawn hex behind the role's start hex.</summary>
    public static HexCoord SpawnHex(Team team, Role role) => Frame(new HexCoord((int)role, -Radius - 1), team);

    /// <summary>A team's three nexus hexes.</summary>
    public static IEnumerable<HexCoord> NexusHexes(Team team) =>
        team == Team.A ? NexusA : NexusA.Select(Hex.HalfTurn);

    /// <summary>True when the hex is one of a team's nexus hexes.</summary>
    public static bool IsNexusHex(HexCoord h, Team team)
    {
        foreach (HexCoord n in NexusA)
        {
            if ((team == Team.A ? n : Hex.HalfTurn(n)) == h) return true;
        }

        return false;
    }

    /// <summary>Tower index at a hex, or −1.</summary>
    public static int TowerAt(HexCoord h) => Array.IndexOf(TowerHexes, h);

    /// <summary>Across-board reading: 0 on the centre axis, ±8 at the side corners.</summary>
    public static int File(HexCoord h) => 2 * h.Q + h.R;

    /// <summary>All playable hexes, in a fixed order.</summary>
    public static readonly HexCoord[] AllHexes = Build();

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
