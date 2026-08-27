namespace Augury.Sim.Tests.Foundation;

/// <summary>
/// ADR-0005 amendment: tier-4 patterns are oriented to the owning team's forward
/// direction, not to world space.
/// </summary>
/// <remarks>
/// <para>Map &amp; Terrain fixes the board's symmetry as a <b>180-degree rotation</b>,
/// <c>(q,r) → (−q,−r)</c>. The two teams therefore face opposite ways, and a tier-4
/// pattern applied verbatim in world space would point toward the enemy for one team
/// and toward its own nexus for the other.</para>
/// <para>These tests prove the fix is sound and, more importantly, prove <i>why</i> it
/// only works on a rotationally symmetric board — a mirrored board would need a
/// reflection, and no rotation reproduces one.</para>
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
    /// The property that makes the amendment correct: a tier-4 pattern played by the
    /// far team at the antipodal origin covers exactly the antipodal hexes. The two
    /// teams get the same ability, seen from opposite ends of the board.
    /// </summary>
    [Fact]
    public void TeamRelativePattern_CoversAntipodalHexes()
    {
        foreach (HexCoord origin in Board(4))
        {
            var near = Chiral
                .Select(o => origin + Hex.ForForward(o, forwardIsPositiveR: true))
                .ToHashSet();

            HexCoord farOrigin = Hex.HalfTurn(origin);
            var far = Chiral
                .Select(o => farOrigin + Hex.ForForward(o, forwardIsPositiveR: false))
                .ToHashSet();

            Assert.Equal(near.Select(Hex.HalfTurn).ToHashSet(), far);
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
    /// The counter-case, and the reason the board's symmetry had to be rotational.
    /// A mirrored chiral pattern is reachable by <b>no</b> rotation, so on a
    /// mirror-symmetric board the two teams would hold differently-shaped versions of
    /// the same ability.
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
