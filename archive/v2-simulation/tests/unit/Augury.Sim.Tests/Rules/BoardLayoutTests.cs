using Augury.Sim.Content;

namespace Augury.Sim.Tests.Rules;

/// <summary>Switching the process-wide board must never overlap another test.</summary>
[CollectionDefinition("Board switching", DisableParallelization = true)]
public class BoardSwitchingCollection;

/// <summary>
/// v2 board-size experiment (<c>design/board-layouts.md</c>): every candidate layout loads,
/// is fair, and plays to a result.
/// </summary>
[Collection("Board switching")]
public class BoardLayoutTests
{
    private static readonly Game G = RandomPlayTests.Game;

    public static TheoryData<string> Candidates() =>
        new(Directory.GetFiles(Path.Combine(ContentLoader.FindDataDirectory(), "boards"), "board_*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)["board_".Length..]));

    [Fact]
    public void Classic_IsTheOriginalRadiusFourHexagon()
    {
        BoardLayout c = BoardLayout.Classic;
        c.Validate("classic");
        Assert.Equal(61, c.Hexes.Length);
        Assert.Equal(new HexCoord(0, -4), c.StartA[0]);
    }

    [Theory]
    [MemberData(nameof(Candidates))]
    public void Candidate_LoadsAndIsMirrorSymmetric(string name)
    {
        BoardLayout layout = BoardLayout.Load(name);   // Load validates; this throws if unfair

        Assert.All(layout.Hexes, h => Assert.Contains(Hex.Mirror(h), layout.Hexes));
        Assert.All(layout.Lanes.Concat(layout.Jungle), h => Assert.Contains(h, layout.Hexes));
    }

    [Theory]
    [MemberData(nameof(Candidates))]
    public void Candidate_RandomMatchesPlayToAResult_WithTowersSolidAndWalkable(string name)
    {
        try
        {
            Board.Use(BoardLayout.Load(name));
            foreach (bool solid in new[] { false, true })
            {
                var game = new Game(G.Content, G.Rules with { TowersBlock = solid });
                for (uint seed = 1; seed <= 15; seed++)
                {
                    var rng = new RandomPlayTests.Lcg(seed);
                    MatchState s = game.NewMatch();
                    var legal = new List<Command>();
                    int n = 0;
                    while (s.Phase != Phase.MatchOver)
                    {
                        legal.Clear();
                        game.Legal(s, legal);
                        Assert.True(legal.Count > 0, $"{name}: no legal command in {s.Phase} (seed {seed})");
                        game.Apply(ref s, legal[rng.Below(legal.Count)]);
                        Assert.True(++n < 20_000, $"{name}: match did not end (seed {seed})");
                        for (int i = 0; i < 10; i++)
                        {
                            if (s.Champions[i].OnBoard) Assert.True(Board.Playable(s.Champions[i].Pos), $"{name}: champion off the board");
                        }
                    }
                }
            }
        }
        finally
        {
            Board.Use(BoardLayout.Classic);
        }
    }

    [Fact]
    public void Parse_AsymmetricLayout_IsRejected()
    {
        string json = """
            { "name": "lopsided", "hexes": [[0,0],[1,0],[0,-1]], "towers": [[0,0],[1,0],[0,-1],[0,0],[0,0]],
              "nexusA": [[0,-1]], "startA": [], "spawnA": [] }
            """;
        Assert.Throws<ContentException>(() => BoardLayout.Parse(json, "test"));
    }
}
