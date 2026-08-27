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
    /// Half-turn about the origin: <c>(q,r) → (−q,−r)</c>. This is the board's own
    /// symmetry map and also the transform that reorients a tier-4 pattern into the
    /// far team's forward frame (ADR-0005, amended).
    /// </summary>
    /// <remarks>
    /// <para>It is exactly <c>Rotate(offset, 3)</c>. That equivalence is the whole
    /// reason team-relative tier-4 patterns are possible: the transform is a
    /// <b>rotation</b>, so the six-facing system already expresses it and the shape is
    /// preserved. Had the board been mirror-symmetric, the transform would have been a
    /// reflection, which no rotation reproduces — the two teams would then hold
    /// chirally different versions of the same ability.</para>
    /// </remarks>
    public static HexCoord HalfTurn(HexCoord h) => new(-h.Q, -h.R);

    /// <summary>
    /// Reorients a pattern offset authored in the canonical frame (forward = +R) into
    /// the acting team's frame. Tier 4 only; tiers 1–3 choose their own orientation.
    /// </summary>
    public static HexCoord ForForward(HexCoord offset, bool forwardIsPositiveR)
        => forwardIsPositiveR ? offset : HalfTurn(offset);

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
