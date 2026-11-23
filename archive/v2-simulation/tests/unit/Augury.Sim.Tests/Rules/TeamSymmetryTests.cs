using Augury.Sim.Content;

namespace Augury.Sim.Tests.Rules;

/// <summary>
/// ADR-0005, second amendment (2026-09-28): the team symmetry is a mirror, so each role
/// starts opposite the same role — top faces top — and every team-relative shape is
/// mirrored for team B, which keeps the two teams playing the same game.
/// </summary>
public class TeamSymmetryTests
{
    private static readonly Game G = RandomPlayTests.Game;

    private static IEnumerable<HexCoord> Area(int radius) =>
        from q in Enumerable.Range(-radius, 2 * radius + 1)
        from r in Enumerable.Range(-radius, 2 * radius + 1)
        let h = new HexCoord(q, r)
        where Hex.InBoard(h, radius)
        select h;

    [Fact]
    public void Mirror_IsAnIntegerReflection_ThatPreservesDistance()
    {
        foreach (HexCoord a in Area(6))
        {
            Assert.Equal(a, Hex.Mirror(Hex.Mirror(a)));
            foreach (HexCoord b in Area(4))
            {
                Assert.Equal(HexCoord.Distance(a, b), HexCoord.Distance(Hex.Mirror(a), Hex.Mirror(b)));
            }
        }
    }

    [Fact]
    public void Mirror_MapsTheBoardAndItsStructuresOntoThemselves()
    {
        Assert.All(Board.AllHexes, h => Assert.True(Board.Playable(Hex.Mirror(h))));
        Assert.Equal(Board.TowerHexes.ToHashSet(), Board.TowerHexes.Select(Hex.Mirror).ToHashSet());
        Assert.Equal(Board.NexusHexes(Team.B).ToHashSet(), Board.NexusA.Select(Hex.Mirror).ToHashSet());

        // Across-board position is kept and rank is negated: nothing changes sides.
        Assert.All(Board.AllHexes, h =>
        {
            Assert.Equal(Board.File(h), Board.File(Hex.Mirror(h)));
            Assert.Equal(-h.R, Hex.Mirror(h).R);
        });
    }

    [Fact]
    public void StartHexes_EachRoleStartsOnTheSameSideAsItsCounterpart()
    {
        // The bug: top started bottom-left for A and top-right for B.
        foreach (Role role in Enum.GetValues<Role>())
        {
            HexCoord a = Board.StartHex(Team.A, role), b = Board.StartHex(Team.B, role);
            Assert.Equal(Hex.Mirror(a), b);
            Assert.Equal(Board.File(a), Board.File(b));
            Assert.Equal(Hex.Mirror(Board.SpawnHex(Team.A, role)), Board.SpawnHex(Team.B, role));
        }

        Assert.True(Board.File(Board.StartHex(Team.B, Role.Top)) < 0, "both tops start on the left");
        Assert.True(Board.File(Board.StartHex(Team.B, Role.Support)) > 0, "both supports start on the right");
    }

    [Fact]
    public void Patterns_TeamBCoversTheMirrorOfEveryPlacementTeamACanMake()
    {
        for (int def = 0; def < G.Content.Champions.Count; def++)
        {
            ChampionDef d = G.Content.Champions[def];
            foreach (AbilityDef ab in d.Abilities.Where(x => !x.IsFree))
            {
                foreach (HexCoord p in Board.AllHexes)
                {
                    var a = new Champion { Pos = p, Team = Team.A, Def = (byte)def };
                    var b = new Champion { Pos = Hex.Mirror(p), Team = Team.B, Def = (byte)def };
                    int facings = ab.Initiative == 3 ? 6 : 1;
                    for (int f = 0; f < facings; f++)
                    {
                        var wanted = G.PatternCells(a, ab, f).Select(Hex.Mirror).ToHashSet();
                        bool reachable = Enumerable.Range(0, facings).Any(g => G.PatternCells(b, ab, g).ToHashSet().SetEquals(wanted));
                        Assert.True(reachable, $"{d.Name} {ab.Name} facing {f} at {p} has no mirror for team B");
                    }
                }
            }
        }
    }

    [Fact]
    public void Opening_IdenticalDrafts_HaveIdenticalAvailableOpenings()
    {
        int[] picks = [0, 2, 4, 6, 8];
        MatchState s = G.NewMatch(picks, picks);

        for (int role = 0; role < 5; role++)
        {
            for (int ab = 0; ab < 4; ab++)
            {
                Assert.Equal(G.OpeningAvailable(s, role, ab), G.OpeningAvailable(s, 5 + role, ab));
            }
        }
    }

    [Fact]
    public void Draft_APickedChampion_LeavesThePoolForBothTeams()
    {
        MatchState s = G.NewMatch();
        Command first = G.Legal(s)[0];

        G.Apply(ref s, first);

        Assert.DoesNotContain(G.Legal(s), c => c.Ability == first.Ability);
    }

    [Fact]
    public void Draft_TenChampionsTwoPerRole_EveryDraftEndsWithTenDifferentChampions()
    {
        var rng = new RandomPlayTests.Lcg(11);
        for (int match = 0; match < 50; match++)
        {
            MatchState s = G.NewMatch();
            while (s.Phase == Phase.Draft)
            {
                List<Command> legal = G.Legal(s);
                G.Apply(ref s, legal[rng.Below(legal.Count)]);
            }

            var picked = Enumerable.Range(0, 10).Select(i => (int)s.Champions[i].Def).ToHashSet();
            Assert.Equal(10, picked.Count);
        }
    }

    [Fact]
    public void Displace_TiedPushDestinations_BreakTheSameWayForBothTeams()
    {
        // Regression: push ties broke by absolute direction order, which is not
        // mirror-symmetric, and self-play drifted from 48/52 to 54/46.
        const int AWarden = 0, AStalker = 1, BWarden = 5, BStalker = 6, Rebuke = 0;
        int checkedCases = 0;
        foreach (HexCoord p in Board.AllHexes)
        {
            for (int d = 0; d < 6; d++)
            {
                HexCoord t = p + Hex.Directions[d];
                MatchState a = RulesTests.Arena(), b = RulesTests.Arena();
                if (!Board.Playable(t) || Game.ChampionAt(a, p) >= 0 || Game.ChampionAt(a, t) >= 0) continue;
                if (Game.ChampionAt(b, Hex.Mirror(p)) >= 0 || Game.ChampionAt(b, Hex.Mirror(t)) >= 0) continue;

                a.Champions[AWarden].Pos = p;
                a.Champions[BStalker].Pos = t;
                G.Apply(ref a, new Command(CommandKind.Ability, AWarden, Rebuke, Target.Champ(BStalker)));

                b.Active = Team.B;
                b.Champions[BWarden].Pos = Hex.Mirror(p);
                b.Champions[AStalker].Pos = Hex.Mirror(t);
                G.Apply(ref b, new Command(CommandKind.Ability, BWarden, Rebuke, Target.Champ(AStalker)));

                Assert.Equal(Hex.Mirror(a.Champions[BStalker].Pos), b.Champions[AStalker].Pos);
                checkedCases++;
            }
        }

        Assert.True(checkedCases > 100);
    }
}
