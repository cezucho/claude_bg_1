namespace Augury.Sim.Tests.Rules;

/// <summary>
/// v2 direction (<c>design/v2-direction.md</c>), owner decisions 2026-10-04: victory points
/// become damage to the enemy nexus, and towers are impassable.
/// </summary>
public class NexusRaceTests
{
    private static readonly Game G = RandomPlayTests.Game;

    /// <summary>The same game with impassable towers, which the shipped config leaves off for now (D-039).</summary>
    private static readonly Game Solid = new(G.Content, G.Rules with { TowersBlock = true });
    private const int AWarden = 0, BStalker = 6, Rebuke = 0;

    [Fact]
    public void Siege_EachTowerHeldAtRoundClose_DamagesTheEnemyNexus()
    {
        MatchState s = RulesTests.Arena();
        s.Towers[0].Owner = Team.A;   // A holds three towers, B two
        int a = s.NexusHp[0], b = s.NexusHp[1];

        RulesTests.CloseRound(ref s);

        Assert.Equal(b - 3 * G.Rules.TowerSiege, s.NexusHp[1]);
        Assert.Equal(a - 2 * G.Rules.TowerSiege, s.NexusHp[0]);
    }

    [Fact]
    public void Siege_NexusGateClosed_StillTakesSiegeDamage()
    {
        // The gate guards against direct attacks only; holding towers always counts.
        MatchState s = RulesTests.Arena();
        Assert.False(Game.NexusVulnerable(s, Team.B));
        int b = s.NexusHp[1];

        RulesTests.CloseRound(ref s);

        Assert.True(s.NexusHp[1] < b);
    }

    [Fact]
    public void Siege_NexusReducedToZeroAtRoundClose_EndsTheMatchBySiege()
    {
        MatchState s = RulesTests.Arena();
        s.NexusHp[1] = 1;

        RulesTests.CloseRound(ref s);

        Assert.Equal(Phase.MatchOver, s.Phase);
        Assert.Equal(Team.A, s.Winner);
        Assert.Equal(EndReason.Siege, s.EndReason);
    }

    [Fact]
    public void Siege_BothNexusesFallAtTheSameRoundClose_TheOneWithMoreLeftWins()
    {
        MatchState s = RulesTests.Arena();
        s.Towers[0].Owner = Team.A;   // A deals 3, B deals 2
        s.NexusHp[0] = 1;
        s.NexusHp[1] = 1;

        RulesTests.CloseRound(ref s);

        Assert.Equal(Phase.MatchOver, s.Phase);
        Assert.Equal(Team.A, s.Winner);
    }

    [Fact]
    public void Towers_NoChampionCanEndOnATowerHex_AcrossRandomMatches()
    {
        // Covers every way a champion moves: opening instructions, fallback, basic moves,
        // dashes, pushes and pulls.
        for (uint seed = 1; seed <= 40; seed++)
        {
            var rng = new RandomPlayTests.Lcg(seed);
            MatchState s = Solid.NewMatch();
            var legal = new List<Command>();
            for (int n = 0; s.Phase != Phase.MatchOver && n < 20_000; n++)
            {
                legal.Clear();
                Solid.Legal(s, legal);
                Solid.Apply(ref s, legal[rng.Below(legal.Count)]);
                for (int i = 0; i < 10; i++)
                {
                    Champion c = s.Champions[i];
                    Assert.False(c.OnBoard && Board.TowerAt(c.Pos) >= 0, $"seed {seed}: {G.Name(s, i)} stands on tower {c.Pos}");
                }
            }
        }
    }

    [Fact]
    public void Towers_AreNeverAMoveOrDashDestination()
    {
        MatchState s = RulesTests.Arena();
        for (int slot = 0; slot < 10; slot++)
        {
            Assert.DoesNotContain(Solid.MoveDestinations(s, slot), h => Board.TowerAt(h) >= 0);
        }

        // Stalker's Lunge dashes to an empty hex; the Arena parks it beside tower (0,−2).
        List<Target> dash = Solid.AbilityTargets(s, 1, 0);
        Assert.NotEmpty(dash);
        Assert.DoesNotContain(dash, t => t.Kind == TargetKind.Hex && Board.TowerAt(t.Hex) >= 0);
    }

    [Fact]
    public void Towers_APushNeverLandsOnATower()
    {
        int cases = 0;
        foreach (HexCoord p in Board.AllHexes)
        {
            for (int d = 0; d < 6; d++)
            {
                HexCoord t = p + Hex.Directions[d];
                MatchState s = RulesTests.Arena();
                if (!Board.Playable(t) || Board.TowerAt(p) >= 0 || Board.TowerAt(t) >= 0) continue;
                if (Game.ChampionAt(s, p) >= 0 || Game.ChampionAt(s, t) >= 0) continue;

                s.Champions[AWarden].Pos = p;
                s.Champions[BStalker].Pos = t;
                Solid.Apply(ref s, new Command(CommandKind.Ability, AWarden, Rebuke, Target.Champ(BStalker)));

                Assert.True(Board.TowerAt(s.Champions[BStalker].Pos) < 0);
                cases++;
            }
        }

        Assert.True(cases > 100);
    }
}
