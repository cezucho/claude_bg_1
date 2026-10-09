namespace Augury.Sim;

/// <summary>
/// Receives who did what to whom while a command resolves — for statistics in the harness.
/// Passed per <see cref="Game.Apply(ref MatchState, in Command, List{GameEvent}?, IMatchObserver?)"/> call, so the AI's search on copies of the state reports
/// nothing. The rules never read anything back from it.
/// </summary>
public interface IMatchObserver
{
    /// <summary>A champion lost <paramref name="hpLost"/> HP (after shields) and its shield absorbed
    /// <paramref name="absorbed"/>. <paramref name="source"/> is the champion slot that caused it, or
    /// −1 for poison, burn and tower shots.</summary>
    void ChampionDamaged(int source, int target, int hpLost, int absorbed);

    /// <summary>A champion gained <paramref name="amount"/> HP (actual, after the cap).</summary>
    void ChampionHealed(int source, int target, int amount);

    /// <summary>A shield was given.</summary>
    void Shielded(int source, int target, int amount);

    /// <summary>A tower or nexus lost <paramref name="amount"/> HP to <paramref name="source"/> (−1: siege).</summary>
    void StructureDamaged(int source, int amount, bool nexus);

    /// <summary>A synergy payoff (status bonus) landed.</summary>
    void Payoff(int source, int target);

    /// <summary>A push was stopped short and slammed.</summary>
    void Slam(int source, int target);

    /// <summary>A champion died at the death check.</summary>
    void Died(int victim);
}
