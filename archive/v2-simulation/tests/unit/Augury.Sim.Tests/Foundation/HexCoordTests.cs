namespace Augury.Sim.Tests.Foundation;

/// <summary>
/// ADR-0005 validation criteria, made executable.
/// </summary>
public class HexCoordTests
{
    private static IEnumerable<HexCoord> Board(int radius)
    {
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                var h = new HexCoord(q, r);
                if (Hex.InBoard(h, radius))
                {
                    yield return h;
                }
            }
        }
    }

    [Fact]
    public void Distance_IsSymmetric_ForEveryPairOnTheBoard()
    {
        HexCoord[] board = Board(4).ToArray();
        foreach (HexCoord a in board)
        {
            foreach (HexCoord b in board)
            {
                Assert.Equal(HexCoord.Distance(a, b), HexCoord.Distance(b, a));
            }
        }
    }

    [Fact]
    public void Distance_ToSelf_IsZero()
        => Assert.All(Board(4), h => Assert.Equal(0, HexCoord.Distance(h, h)));

    [Fact]
    public void Neighbours_AreAllAtDistanceOne()
    {
        foreach (HexCoord d in Hex.Directions)
        {
            Assert.Equal(1, HexCoord.Distance(default, d));
        }
    }

    [Fact]
    public void Directions_ContainsExactlySixDistinctEntries()
    {
        HexCoord[] dirs = Hex.Directions.ToArray();
        Assert.Equal(6, dirs.Length);
        Assert.Equal(6, dirs.Distinct().Count());
    }

    [Fact]
    public void Directions_AreSymmetric_EveryDirectionHasItsOpposite()
    {
        // The Round 3 prototype bug was a DIRECTIONAL bias. If this set were
        // ever asymmetric, one team could move where the other could not.
        HexCoord[] dirs = Hex.Directions.ToArray();
        foreach (HexCoord d in dirs)
        {
            Assert.Contains(new HexCoord(-d.Q, -d.R), dirs);
        }
    }

    [Fact]
    public void Rotate_SixSteps_ReturnsIdentity()
        => Assert.All(Board(4), h => Assert.Equal(h, Hex.Rotate(h, 6)));

    [Fact]
    public void Rotate_PreservesDistanceFromOrigin()
    {
        foreach (HexCoord h in Board(4))
        {
            for (int s = 0; s < 6; s++)
            {
                Assert.Equal(h.Magnitude, Hex.Rotate(h, s).Magnitude);
            }
        }
    }

    [Fact]
    public void Rotate_ProducesSixDistinctFacings_ForAnAsymmetricOffset()
    {
        var offset = new HexCoord(2, -1);
        HexCoord[] facings = Enumerable.Range(0, 6).Select(s => Hex.Rotate(offset, s)).ToArray();
        Assert.Equal(6, facings.Distinct().Count());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(-13)]
    public void Rotate_NormalisesOutOfRangeStepCounts(int steps)
    {
        var offset = new HexCoord(2, -1);
        int normalised = ((steps % 6) + 6) % 6;
        Assert.Equal(Hex.Rotate(offset, normalised), Hex.Rotate(offset, steps));
    }

    [Fact]
    public void S_IsDerivedSoCubeCoordinatesAlwaysSumToZero()
        => Assert.All(Board(4), h => Assert.Equal(0, h.Q + h.R + h.S));
}
