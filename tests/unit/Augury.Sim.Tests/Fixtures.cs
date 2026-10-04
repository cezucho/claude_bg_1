using Augury.Sim.Content;

namespace Augury.Sim.Tests;

/// <summary>
/// The rule tests' world: the first playable's placeholder roster (four abilities each), kept
/// as a fixture in <c>tests/fixtures/champions_v1</c>, on the classic board with walkable towers.
/// The core rules — ladder, chains, death check, siege — are tested here independently of the
/// shipped roster, which changes as content is designed. v2 content has its own tests.
/// </summary>
internal static class Fixtures
{
    /// <summary>Directory of the v1 fixture roster.</summary>
    public static readonly string V1Champions = Find(Path.Combine("tests", "fixtures", "champions_v1"));

    /// <summary>A game on the v1 fixture roster, classic board, walkable towers.</summary>
    public static readonly Game V1 = MakeV1();

    private static Game MakeV1()
    {
        Board.Use(BoardLayout.Classic);
        RulesConfig rules = RulesConfig.LoadDefault() with { Board = "classic", TowersBlock = false };
        return new Game(ContentLoader.LoadDirectory(V1Champions), rules);
    }

    private static string Find(string relative)
    {
        foreach (string root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(root); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, relative);
                if (Directory.Exists(candidate)) return candidate;
            }
        }

        throw new ContentException($"Could not locate {relative}.");
    }
}
