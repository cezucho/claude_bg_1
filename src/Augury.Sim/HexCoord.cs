namespace Augury.Sim;

/// <summary>
/// An axial hex coordinate, flat-top layout. The cube coordinate <c>S</c> is
/// derived rather than stored.
/// </summary>
/// <remarks>
/// ADR-0005. Embedded inside <c>MatchState</c>, which the AI clones roughly
/// 19,000 times per round, so this stays a small value type with no reference
/// members. All arithmetic is integer-exact; there is no trigonometry anywhere
/// in the simulation.
/// </remarks>
public readonly record struct HexCoord(int Q, int R)
{
    /// <summary>The third cube coordinate, derived: <c>-Q - R</c>.</summary>
    public int S => -Q - R;

    /// <summary>Hex distance between two coordinates.</summary>
    public static int Distance(HexCoord a, HexCoord b)
        => (Math.Abs(a.Q - b.Q) + Math.Abs(a.S - b.S) + Math.Abs(a.R - b.R)) / 2;

    /// <summary>Distance from the board origin.</summary>
    public int Magnitude => Distance(this, default);

    /// <summary>Adds two coordinates, treating the second as an offset.</summary>
    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);

    /// <summary>Subtracts one coordinate from another, yielding an offset.</summary>
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
}

/// <summary>Hex grid operations. Pure functions; no state, no engine types.</summary>
public static class Hex
{
    /// <summary>
    /// The six neighbour directions, in canonical order.
    /// </summary>
    /// <remarks>
    /// <para><b>Callers must consume all six.</b> Never truncate a generated
    /// action or target set along this ordered axis.</para>
    /// <para>This is not a style note. Capping move targets to the first six
    /// entries of a direction-ordered list made one team unable to move toward
    /// the map objectives, and read as a 70% first-mover advantage for three
    /// rounds of prototype investigation. See
    /// <c>prototypes/initiative-ladder/REPORT.md</c>, Round 3 addendum.</para>
    /// </remarks>
    public static ReadOnlySpan<HexCoord> Directions => DirectionsBacking;

    private static readonly HexCoord[] DirectionsBacking =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    /// <summary>True when the coordinate lies within a hex board of the given radius.</summary>
    public static bool InBoard(HexCoord h, int radius) => h.Magnitude <= radius;

    /// <summary>
    /// Half-turn about the origin: <c>(q,r) → (−q,−r)</c>. Exactly <c>Rotate(h, 3)</c>.
    /// </summary>
    /// <remarks>
    /// This was the board's team symmetry until 2026-09-28. It placed team B's top lane on
    /// the opposite side of the board from team A's, so the teams' roles did not face each
    /// other. The team symmetry is now <see cref="Mirror"/> (ADR-0005, second amendment).
    /// </remarks>
    public static HexCoord HalfTurn(HexCoord h) => new(-h.Q, -h.R);

    /// <summary>
    /// Reflection across the board's horizontal axis: <c>(q,r) → (q+r, −r)</c>, in cube
    /// terms <c>(q,r,s) → (−s,−r,−q)</c>. The board's team symmetry map (ADR-0005, second
    /// amendment): it swaps the two front lines, keeps every hex on its own side of the
    /// board, and so puts each role opposite the same role.
    /// </summary>
    /// <remarks>
    /// <para>A reflection reverses chirality, and no rotation reproduces it. That is why
    /// <b>every</b> team-relative shape passes through this map — start and spawn hexes,
    /// opening directions, and tier-3 as well as tier-4 patterns. A chiral pattern the far
    /// team could only rotate would reach the mirror image of none of the near team's
    /// placements, and the teams would not play the same game.</para>
    /// <para>Integer-exact, distance-preserving, and its own inverse.</para>
    /// </remarks>
    public static HexCoord Mirror(HexCoord h) => new(h.Q + h.R, -h.R);

    /// <summary>
    /// Reorients an offset authored in the canonical frame (forward = +R, team A) into the
    /// acting team's frame: identity for team A, <see cref="Mirror"/> for team B.
    /// </summary>
    public static HexCoord ForForward(HexCoord offset, bool forwardIsPositiveR)
        => forwardIsPositiveR ? offset : Mirror(offset);

    /// <summary>
    /// Rotates an offset clockwise by 60 degrees per step. Integer-exact.
    /// Six steps return the identity.
    /// </summary>
    /// <remarks>
    /// Tier-3 abilities rotate their pattern to any of six facings; tier-4
    /// abilities apply theirs in the owning team's forward frame (ADR-0005, amended).
    /// </remarks>
    public static HexCoord Rotate(HexCoord offset, int steps)
    {
        int n = ((steps % 6) + 6) % 6;
        HexCoord h = offset;
        for (int i = 0; i < n; i++)
        {
            h = new HexCoord(-h.R, -h.S);
        }

        return h;
    }
}
