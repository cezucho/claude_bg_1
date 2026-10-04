using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>
/// Board geometry for the current <see cref="BoardLayout"/>: the original hexagon unless
/// <see cref="Use"/> chose another.
/// </summary>
/// <remarks>
/// The layout is process-wide and set once at startup, before any match — a deliberate
/// simplification for the v2 board-size experiment (D-040). It keeps every rule a static
/// read, as before. If the game ever needs two boards in one process, the layout moves onto
/// <see cref="Game"/>.
/// </remarks>
public static class Board
{
    private const int Span = 12;   // lookup covers q, r in [−Span, Span]
    private static BoardLayout _layout = BoardLayout.Classic;
    private static bool[] _playable = Index(BoardLayout.Classic);
    private static HexCoord[] _mirrored = BoardLayout.Classic.Hexes.Select(Hex.Mirror).ToArray();

    /// <summary>The layout in use.</summary>
    public static BoardLayout Layout => _layout;

    /// <summary>
    /// Switches the board. Call once at startup, before creating matches; states made on one
    /// board are meaningless on another.
    /// </summary>
    public static void Use(BoardLayout layout)
    {
        layout.Validate(layout.Name);
        _playable = Index(layout);
        _mirrored = layout.Hexes.Select(Hex.Mirror).ToArray();
        _layout = layout;
    }

    /// <summary>Tower positions: 0 centre, 1–2 team A's home towers, 3–4 team B's.</summary>
    public static HexCoord[] TowerHexes => _layout.Towers;

    /// <summary>Team A's nexus hexes; team B's are their mirror images.</summary>
    public static HexCoord[] NexusA => _layout.NexusA;

    /// <summary>All playable hexes, in a fixed order (row, then column).</summary>
    public static HexCoord[] AllHexes => _layout.Hexes;

    /// <summary>True when on the playable board.</summary>
    public static bool Playable(HexCoord h) =>
        h.Q >= -Span && h.Q <= Span && h.R >= -Span && h.R <= Span && _playable[Key(h)];

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
    public static HexCoord[] HexesInFrame(Team team) => team == Team.B ? _mirrored : _layout.Hexes;

    /// <summary>Start hex for a role on the front line.</summary>
    public static HexCoord StartHex(Team team, Role role) => Frame(_layout.StartA[(int)role], team);

    /// <summary>Off-board spawn hex behind the role's start hex.</summary>
    public static HexCoord SpawnHex(Team team, Role role) => Frame(_layout.SpawnA[(int)role], team);

    /// <summary>A team's nexus hexes.</summary>
    public static IEnumerable<HexCoord> NexusHexes(Team team) =>
        _layout.NexusA.Select(h => Frame(h, team));

    /// <summary>True when the hex is one of a team's nexus hexes.</summary>
    public static bool IsNexusHex(HexCoord h, Team team)
    {
        foreach (HexCoord n in _layout.NexusA)
        {
            if (Frame(n, team) == h) return true;
        }

        return false;
    }

    /// <summary>Tower index at a hex, or −1.</summary>
    public static int TowerAt(HexCoord h) => Array.IndexOf(_layout.Towers, h);

    /// <summary>Across-board reading: 0 on the centre axis, negative toward the top lane.</summary>
    public static int File(HexCoord h) => 2 * h.Q + h.R;

    private static int Key(HexCoord h) => (h.Q + Span) * (2 * Span + 1) + h.R + Span;

    private static bool[] Index(BoardLayout layout)
    {
        var map = new bool[(2 * Span + 1) * (2 * Span + 1)];
        foreach (HexCoord h in layout.Hexes)
        {
            if (Math.Abs(h.Q) > Span || Math.Abs(h.R) > Span) throw new ContentException($"{layout.Name}: hex {h} is outside the supported area.");
            map[Key(h)] = true;
        }

        return map;
    }
}
