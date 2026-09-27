using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Augury.Sim.Tests.Rules;

/// <summary>
/// Whole matches of seeded random legal play. Finds crashes, rule loops and matches that
/// never end — the structural bugs aggregate metrics hide (ADR-0001's reason for a harness).
/// </summary>
public class RandomPlayTests
{
    internal static readonly Game Game = Game.LoadDefault();

    /// <summary>Deterministic LCG: a fixed seed, never a time-based one.</summary>
    internal sealed class Lcg(uint seed)
    {
        private uint _s = seed;
        public int Below(int n) => (int)(((_s = _s * 1664525u + 1013904223u) >> 8) % (uint)n);
    }

    internal static MatchState PlayRandom(uint seed, out int decisions, List<GameEvent>? log = null)
    {
        var rng = new Lcg(seed);
        MatchState s = Game.NewMatch();
        decisions = 0;
        var legal = new List<Command>();
        while (s.Phase != Phase.MatchOver)
        {
            legal.Clear();
            Game.Legal(s, legal);
            Assert.True(legal.Count > 0, $"No legal command in {s.Phase} for {s.Active}, round {s.Round} (seed {seed}).");
            Command pick = legal[rng.Below(legal.Count)];
            Game.Apply(ref s, pick, log);
            decisions++;
            Assert.True(decisions < 20_000, $"Match did not end (seed {seed}).");
        }

        return s;
    }

    [Fact]
    public void RandomMatches_AlwaysTerminateWithAResult()
    {
        for (uint seed = 1; seed <= 60; seed++)
        {
            MatchState s = PlayRandom(seed, out _);
            Assert.Equal(Phase.MatchOver, s.Phase);
            Assert.NotEqual(EndReason.None, s.EndReason);
        }
    }

    [Fact]
    public void SameSeed_ProducesByteIdenticalFinalState()
    {
        MatchState a = PlayRandom(12345, out int da);
        MatchState b = PlayRandom(12345, out int db);
        Assert.Equal(da, db);
        Assert.Equal(Bytes(a), Bytes(b));
    }

    [Fact]
    public void MatchState_IsSmallEnoughToCloneFreely()
    {
        Assert.True(Unsafe.SizeOf<MatchState>() < 1024, $"MatchState is {Unsafe.SizeOf<MatchState>()} bytes.");
    }

    internal static byte[] Bytes(MatchState s)
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref s, 1);
        return MemoryMarshal.AsBytes(span).ToArray();
    }
}
