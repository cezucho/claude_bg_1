using Augury.Sim.Content;

namespace Augury.Sim.Tests.Rules;

/// <summary>
/// The load-bearing rules, each asserted directly. Cites <c>design/mvp-rules.md</c> sections
/// and the GDD acceptance criteria they implement.
/// </summary>
public class RulesTests
{
    private static readonly Game G = RandomPlayTests.Game;

    // Content indices follow file order: warden, stalker, oracle, ranger, lifeweaver.
    private static readonly int[] Picks = [0, 1, 2, 3, 4];

    private const int AWarden = 0, AOracle = 2, ARanger = 3;
    private const int BWarden = 5, BStalker = 6, BOracle = 7;

    /// <summary>A round-1 ladder with everyone parked out of each other's reach.</summary>
    internal static MatchState Arena()
    {
        MatchState s = G.NewMatch(Picks, Picks);
        HexCoord[] a = [new(-1, -3), new(0, -3), new(1, -3), new(2, -3), new(3, -3)];
        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            c.Flags = ChampFlags.OpeningDone;
            c.Pos = i < 5 ? a[i] : Hex.Mirror(a[i - 5]);
        }

        for (int b = 0; b < 12; b++) s.Beacons[b].Team = Team.None;
        s.Round = 1;
        s.Half = 1;
        s.RoundOpener = Team.A;
        s.Phase = Phase.Ladder;
        s.Active = Team.A;
        s.Ceiling = 4;
        return s;
    }

    /// <summary>Forces round close by declining a second-half Last Word.</summary>
    internal static List<GameEvent> CloseRound(ref MatchState s)
    {
        var log = new List<GameEvent>();
        s.Phase = Phase.LastWord;
        s.Half = 2;
        s.Active = Team.A;
        G.Apply(ref s, Command.DeclineCmd, log);
        return log;
    }

    [Fact]
    public void RoundClose_PoisonTakesChampionBelowZero_EntersDyingInsteadOfDying()
    {
        MatchState s = Arena();
        s.Champions[BWarden].Hp = 2;
        s.Champions[BWarden].PoisonAmount = 3;
        s.Champions[BWarden].PoisonRounds = 1;

        CloseRound(ref s);

        Assert.Equal(Presence.OnBoard, s.Champions[BWarden].Presence);
        Assert.True(s.Champions[BWarden].Has(ChampFlags.Dying));
        Assert.Equal(-1, s.Champions[BWarden].Hp);
    }

    [Fact]
    public void RoundClose_DyingChampionStillAtOrBelowZero_DiesAtTheNextDeathCheck()
    {
        MatchState s = Arena();
        s.Champions[BWarden].Hp = -1;
        s.Champions[BWarden].Flags |= ChampFlags.Dying;
        int before = s.NexusHp[1];

        CloseRound(ref s);

        Assert.Equal(Presence.Dead, s.Champions[BWarden].Presence);
        Assert.Equal(before - G.Rules.KillSiege - Game.TowersOwned(s, Team.A) * G.Rules.TowerSiege, s.NexusHp[1]);
    }

    [Fact]
    public void RoundClose_DeathCheckPrecedesStatusPhase_InEventOrder()
    {
        MatchState s = Arena();
        List<GameEvent> log = CloseRound(ref s);
        int death = log.FindIndex(e => e.Kind == EventKind.DeathCheck);
        int status = log.FindIndex(e => e.Kind == EventKind.StatusPhase);
        Assert.True(death >= 0 && status > death, "ADR-0006: death check must precede the status phase.");
    }

    [Fact]
    public void Ladder_ChampionAtZeroMidLadder_CanStillAct()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Hp = -3;
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BWarden].Pos = new HexCoord(0, 1);

        List<Command> legal = G.Legal(s);

        Assert.Contains(legal, c => c.Kind == CommandKind.Ability && c.Champion == AOracle);
    }

    [Fact]
    public void Ladder_Pass_GrantsTheOpponentALastWord()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BOracle].Pos = new HexCoord(0, 1);

        G.Apply(ref s, Command.PassCmd);

        Assert.Equal(Phase.LastWord, s.Phase);
        Assert.Equal(Team.B, s.Active);
    }

    [Fact]
    public void Ladder_ResponderWithNoLegalAnswer_EndsTheHalfWithoutALastWord()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BWarden].Pos = new HexCoord(0, 2);    // 3 away: in Oracle's reach, out of Warden's
        s.Towers[0].Owner = Team.B;                       // B cannot target its own centre tower
        s.Champions[BStalker].Flags |= ChampFlags.Acted;  // Lunge always has an empty hex to dash to
        s.Champions[9].Flags |= ChampFlags.Acted;         // Mend can always target itself
        Command spark = G.Legal(s).First(c => c.Champion == AOracle && c.Ability == 0 && c.Target.Kind == TargetKind.Champion);

        G.Apply(ref s, spark);

        Assert.Equal(2, s.Half);
        Assert.Equal(Phase.Basic, s.Phase);
    }

    [Fact]
    public void Ladder_EverySingleAbilityOffered_RespectsTheCeiling()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BWarden].Pos = new HexCoord(0, 1);
        s.Ceiling = 2;

        foreach (Command c in G.Legal(s).Where(c => c.Kind == CommandKind.Ability && !c.IsChain))
        {
            Assert.True(G.Def(s.Champions[c.Champion]).Abilities[c.Ability].Initiative <= 2);
        }
    }

    [Fact]
    public void Chain_SecondAbilityAscendsAboveTheCeiling_AndSetsTheCeilingToTheHigher()
    {
        // Ranger's Volley (initiative 1) has a slot for sigil I; a beacon lights it. Warden's
        // Cleave (initiative 3) carries a printed sigil I. At ceiling 1 the chain may still
        // finish at 3.
        MatchState s = Arena();
        s.Ceiling = 1;
        s.Champions[AWarden].Pos = new HexCoord(0, -1);
        s.Champions[ARanger].Pos = new HexCoord(-1, -1);
        s.Champions[BWarden].Pos = new HexCoord(1, -1);
        s.Beacons[0] = new Beacon { Pos = new HexCoord(-1, -1), Team = Team.A, Sigil = 0, Durability = 2 };

        Command chain = G.Legal(s).First(c => c.IsChain && c.Champion == ARanger && c.Ability == 0
                                              && c.Champion2 == AWarden && c.Ability2 == 2);
        G.Apply(ref s, chain);

        Assert.Equal(3, s.Ceiling);
        Assert.True(s.Champions[AWarden].Has(ChampFlags.Acted) && s.Champions[ARanger].Has(ChampFlags.Acted));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    public void Nexus_OpensOnlyOnceTheGatesHomeTowersAreLost(int gate)
    {
        MatchState s = Arena();
        s.NexusGate = (byte)gate;
        Assert.False(Game.NexusVulnerable(s, Team.B));

        s.Towers[3].Owner = Team.A;
        Assert.Equal(gate <= 1, Game.NexusVulnerable(s, Team.B));

        s.Towers[4].Owner = Team.A;
        Assert.True(Game.NexusVulnerable(s, Team.B));

        s.Towers[3].Owner = Team.B;   // retaking a home tower closes a both-towers gate again
        Assert.Equal(gate <= 1, Game.NexusVulnerable(s, Team.B));
    }

    [Fact]
    public void Tower_ReducedToZero_FlipsToTheAttackerAndResetsHp()
    {
        MatchState s = Arena();
        s.Phase = Phase.Basic;
        s.Towers[0].Hp = 1;
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        Command hit = G.Legal(s).First(c => c.Kind == CommandKind.BasicAttack && c.Target.Kind == TargetKind.Tower && c.Target.Index == 0);

        G.Apply(ref s, hit);

        Assert.Equal(Team.A, s.Towers[0].Owner);
        Assert.Equal(G.Rules.TowerHp, s.Towers[0].Hp);
    }

    [Fact]
    public void TierFour_FarTeamPattern_IsTheMirrorOfTheNearTeams()
    {
        MatchState s = Arena();
        s.Champions[AWarden].Pos = new HexCoord(0, 0);
        s.Champions[BWarden].Pos = new HexCoord(0, 0);   // only positions matter here
        AbilityDef earthbreaker = G.Def(s.Champions[AWarden]).Abilities[3];

        HexCoord[] near = G.PatternCells(s.Champions[AWarden], earthbreaker, 0);
        HexCoord[] far = G.PatternCells(s.Champions[BWarden], earthbreaker, 0);

        Assert.Equal(near.Select(Hex.Mirror).OrderBy(h => h.Q).ThenBy(h => h.R), far.OrderBy(h => h.Q).ThenBy(h => h.R));
        Assert.All(near, h => Assert.True(h.R > 0));   // A's pattern points toward B
    }

    [Fact]
    public void Cooldown_UsedInFirstHalf_IsUnavailableForTheRestOfTheRound()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BWarden].Pos = new HexCoord(0, 1);
        s.Champions[AOracle].Cooldowns[1] = 2;

        s.Half = 2;
        s.Phase = Phase.Ladder;
        s.Active = Team.A;

        Assert.DoesNotContain(G.Legal(s), c => c.Champion == AOracle && c.Ability == 1);
    }

    [Fact]
    public void Opening_WhileAnyAbilityIsAvailable_NoFallbackIsOffered()
    {
        MatchState s = G.NewMatch(Picks, Picks);
        List<Command> legal = G.Legal(s);
        Assert.NotEmpty(legal);
        Assert.All(legal, c => Assert.Equal(CommandKind.OpeningPlay, c.Kind));
    }

    [Fact]
    public void Opening_NoAvailableAbility_FallsBackToOneHex()
    {
        MatchState s = G.NewMatch(Picks, Picks);
        // Surround team A's front line with B champions so nothing can move forward.
        for (int i = 5; i < 10; i++) s.Champions[i].Pos = new HexCoord(i - 6, -3);
        List<Command> legal = G.Legal(s);
        Assert.All(legal, c => Assert.Equal(CommandKind.OpeningFallback, c.Kind));
    }

    [Fact]
    public void Basic_EachTeamTakesAtMostOnePerChampionPerHalf()
    {
        MatchState s = Arena();
        s.Phase = Phase.Basic;
        Command move = G.Legal(s).First(c => c.Kind == CommandKind.BasicMove && c.Champion == AOracle);
        G.Apply(ref s, move);
        s.Active = Team.A;
        Assert.DoesNotContain(G.Legal(s), c => c.Champion == AOracle);
    }

    [Fact]
    public void Ability_MoldsAfterItsEffect_NeverBeforeIt()
    {
        MatchState s = Arena();
        s.Champions[AOracle].Pos = new HexCoord(0, -1);
        s.Champions[BWarden].Pos = new HexCoord(0, 1);
        int powBefore = G.StatPermille(s.Champions[AOracle], Stat.Pow);
        int hpBefore = s.Champions[BWarden].Hp;
        Command spark = G.Legal(s).First(c => c.Champion == AOracle && c.Ability == 0 && c.Target.Kind == TargetKind.Champion);

        G.Apply(ref s, spark);

        int expected = Math.Max(1, 3 * 1000 * powBefore / 1_000_000 - G.Armour(s.Champions[BWarden]));
        Assert.Equal(hpBefore - expected, s.Champions[BWarden].Hp);
    }

    [Fact]
    public void Draft_UndraftedChampions_AreNotOnTheBoard()
    {
        // Regression: undrafted slots defaulted to OnBoard at (0,0) and the client drew them.
        MatchState s = G.NewMatch();
        Assert.Equal(-1, Game.ChampionAt(s, new HexCoord(0, 0)));
    }
}
