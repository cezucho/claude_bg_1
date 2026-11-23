namespace Augury.Sim;

/// <summary>
/// All scaling and division in the simulation goes through these helpers.
/// There is exactly one rounding rule in this game and it lives here.
/// </summary>
/// <remarks>
/// ADR-0002: the simulation is integer-only. No <c>float</c>, <c>double</c> or
/// <c>decimal</c> may appear anywhere in this assembly. Fractional multipliers
/// are expressed in permille — <c>2200</c> means 2.2x.
/// </remarks>
public static class Arith
{
    /// <summary>Permille denominator. A multiplier of 1000 is 1.0x.</summary>
    public const int PermilleScale = 1000;

    /// <summary>
    /// Floor division: rounds toward negative infinity, unlike C#'s <c>/</c>
    /// operator which truncates toward zero.
    /// </summary>
    /// <remarks>
    /// The difference is not academic. A champion at negative HP is a real state
    /// in this game (the Dying round), and every debuff is a negative delta, so
    /// truncation and flooring disagree exactly where the rules are subtlest.
    /// </remarks>
    public static long FloorDiv(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException(
                "Arith.FloorDiv: denominator was zero. The simulation has no defined " +
                "behaviour for division by zero; this is a content or logic error.");
        }

        long quotient = numerator / denominator;
        bool signsDiffer = (numerator < 0) != (denominator < 0);
        if (signsDiffer && quotient * denominator != numerator)
        {
            quotient--;
        }

        return quotient;
    }

    /// <summary>
    /// Applies a permille scalar. This is THE canonical scaling operation —
    /// every multiplier in the game passes through it.
    /// </summary>
    /// <param name="value">The value to scale.</param>
    /// <param name="permille">Parts per thousand. 2200 means 2.2x.</param>
    public static int ScalePermille(int value, int permille)
        => checked((int)FloorDiv((long)value * permille, PermilleScale));
}
