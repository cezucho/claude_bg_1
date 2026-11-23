namespace Augury.Sim.Tests.Foundation;

/// <summary>
/// ADR-0005 amendments: team-relative patterns. Geometry facts about rotation and
/// reflection that the team frame relies on.
/// </summary>
/// <remarks>
/// <para>The first amendment made the team symmetry a 180-degree rotation and reoriented
/// tier-4 patterns by a half-turn. The second (2026-09-28) replaced it with a mirror,
/// <see cref="Hex.Mirror"/>, so that each role starts opposite the same role. Because a
/// reflection is not reachable by rotation (proved below), the mirror is applied to
/// tier-3 patterns as well as tier-4 ones — see <c>TeamSymmetryTests</c>.</para>
/// </remarks>
public class PatternOrientationTests
{
    /// <summary>A deliberately chiral pattern: an L with a hook, no axis of symmetry.</summary>
    private static readonly HexCoord[] Chiral =
    [
        new(0, 0), new(1, 0), new(2, 0), new(2, -1)
    ];

    private static IEnumerable<HexCoord> Board(int radius)
    {
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                var h = new HexCoord(q, r);
                if (Hex.InBoard(h, radius)) yield return h;
            }
        }
    }

    /// <summary>Reflection across the q axis: in cube terms, swapping y and z.</summary>
    private static HexCoord Mirror(HexCoord h) => new(h.Q, h.S);

    [Fact]
    public void EngineMirror_IsAlsoUnreachableByRotation()
    {
        var mirrored = Normalise(Chiral.Select(Hex.Mirror));
        Assert.False(Enumerable.Range(0, 6).Any(k => Normalise(Chiral.Select(c => Hex.Rotate(c, k))).SetEquals(mirrored)));
    }

    private static HashSet<HexCoord> Normalise(IEnumerable<HexCoord> cells)
    {
        HexCoord[] set = cells.ToArray();
        // Translate so the lowest (Q, then R) cell sits at the origin, making shapes
        // comparable regardless of where they were authored.
        HexCoord anchor = set.OrderBy(c => c.Q).ThenBy(c => c.R).First();
        return set.Select(c => c - anchor).ToHashSet();
    }

    [Fact]
    public void HalfTurn_IsExactlyThreeRotations_ForEveryOffsetOnTheBoard()
    {
        foreach (HexCoord h in Board(6))
        {
            Assert.Equal(Hex.Rotate(h, 3), Hex.HalfTurn(h));
        }
    }

    [Fact]
    public void HalfTurn_IsItsOwnInverse()
    {
        foreach (HexCoord h in Board(6))
        {
            Assert.Equal(h, Hex.HalfTurn(Hex.HalfTurn(h)));
        }
    }

    [Fact]
    public void HalfTurn_PreservesDistance_SoTheShapeIsUnchanged()
    {
        foreach (HexCoord a in Board(4))
        {
            foreach (HexCoord b in Board(4))
            {
                Assert.Equal(
                    HexCoord.Distance(a, b),
                    HexCoord.Distance(Hex.HalfTurn(a), Hex.HalfTurn(b)));
            }
        }
    }

    [Fact]
    public void ForForward_LeavesTheCanonicalTeamUntouched()
    {
        foreach (HexCoord h in Board(4))
        {
            Assert.Equal(h, Hex.ForForward(h, forwardIsPositiveR: true));
        }
    }

    /// <summary>
    /// A pattern played by the far team at the mirrored origin covers exactly the
    /// mirrored hexes: the same ability, seen from the other end of the board.
    /// </summary>
    [Fact]
    public void TeamRelativePattern_CoversMirroredHexes()
    {
        foreach (HexCoord origin in Board(4))
        {
            var near = Chiral
                .Select(o => origin + Hex.ForForward(o, forwardIsPositiveR: true))
                .ToHashSet();

            HexCoord farOrigin = Hex.Mirror(origin);
            var far = Chiral
                .Select(o => farOrigin + Hex.ForForward(o, forwardIsPositiveR: false))
                .ToHashSet();

            Assert.Equal(near.Select(Hex.Mirror).ToHashSet(), far);
        }
    }

    /// <summary>
    /// A half-turn is a rotation, so it is reachable by the six-facing system that
    /// tier 3 already uses. Nothing new is needed to express it.
    /// </summary>
    [Fact]
    public void HalfTurnedPattern_IsReachableByRotation()
    {
        var turned = Normalise(Chiral.Select(Hex.HalfTurn));
        bool reachable = Enumerable.Range(0, 6)
            .Any(steps => Normalise(Chiral.Select(c => Hex.Rotate(c, steps))).SetEquals(turned));

        Assert.True(reachable, "a half-turn must be one of the six facings");
    }

    /// <summary>
    /// A mirrored chiral pattern is reachable by <b>no</b> rotation. This is why, on the
    /// mirror-symmetric board, tier-3 patterns are mirrored for team B before rotating
    /// rather than rotated as authored: otherwise team B could not answer a placement
    /// with its mirror image.
    /// </summary>
    [Fact]
    public void MirroredPattern_IsReachableByNoRotation()
    {
        var mirrored = Normalise(Chiral.Select(Mirror));
        bool reachable = Enumerable.Range(0, 6)
            .Any(steps => Normalise(Chiral.Select(c => Hex.Rotate(c, steps))).SetEquals(mirrored));

        Assert.False(reachable,
            "if a mirrored pattern were rotation-reachable this test proves nothing — "
            + "pick a genuinely chiral shape");
    }

    [Fact]
    public void Mirror_PreservesDistance_SoChiralityIsTheOnlyDifference()
    {
        foreach (HexCoord a in Board(4))
        {
            foreach (HexCoord b in Board(4))
            {
                Assert.Equal(
                    HexCoord.Distance(a, b),
                    HexCoord.Distance(Mirror(a), Mirror(b)));
            }
        }
    }
}
