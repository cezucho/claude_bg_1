namespace Augury.Sim.Tests.Foundation;

/// <summary>
/// ADR-0002 validation criteria, made executable.
/// </summary>
public class ArithTests
{
    [Theory]
    [InlineData(-7, 2, -4)]   // ADR-0002 states this explicitly: floor, not truncate
    [InlineData(7, 2, 3)]
    [InlineData(-8, 2, -4)]   // exact division: no adjustment
    [InlineData(8, 2, 4)]
    [InlineData(-1, 2, -1)]
    [InlineData(1, -2, -1)]
    [InlineData(-1, -2, 0)]
    [InlineData(0, 5, 0)]
    public void FloorDiv_RoundsTowardNegativeInfinity(long n, long d, long expected)
        => Assert.Equal(expected, Arith.FloorDiv(n, d));

    [Fact]
    public void FloorDiv_DiffersFromCSharpDivisionOnNegatives()
    {
        // The entire reason this helper exists. C# truncates toward zero.
        Assert.Equal(-3, -7 / 2);
        Assert.Equal(-4, Arith.FloorDiv(-7, 2));
    }

    [Fact]
    public void FloorDiv_ThrowsOnZeroDenominator()
        => Assert.Throws<DivideByZeroException>(() => Arith.FloorDiv(1, 0));

    [Theory]
    [InlineData(3, 1000, 3)]     // F3: M(1) = 1.0x
    [InlineData(3, 1300, 3)]     // F3: M(2) = 1.3x -> 3.9 floors to 3
    [InlineData(3, 2200, 6)]     // F3: M(3) = 2.2x -> 6.6 floors to 6
    [InlineData(3, 4400, 13)]    // F3: M(4) = 4.4x -> 13.2 floors to 13
    public void ScalePermille_MatchesInitiativePowerBudget(int value, int permille, int expected)
        => Assert.Equal(expected, Arith.ScalePermille(value, permille));

    [Fact]
    public void ScalePermille_FloorsNegativeValues()
    {
        // A debuff is a negative delta; it must floor like everything else.
        Assert.Equal(-4, Arith.ScalePermille(-3, 1300));   // -3.9 -> -4, not -3
    }

    [Fact]
    public void ScalePermille_UsesLongIntermediate_SoLargeValuesDoNotOverflow()
    {
        // int.MaxValue * 2200 overflows int but not long.
        Assert.Equal(int.MaxValue, Arith.ScalePermille(int.MaxValue, 1000));
    }
}
