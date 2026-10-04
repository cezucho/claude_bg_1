using System.Text.Json;

namespace Augury.Sim;

/// <summary>
/// A board's geometry as data: playable hexes, towers, team A's nexus, start and spawn hexes.
/// Team B's are always the mirror images (ADR-0005, second amendment), and a layout that is
/// not mirror-symmetric is rejected on load, because it would not be fair.
/// </summary>
/// <remarks>
/// Introduced for the v2 board-size experiment (<c>design/board-layouts.md</c>). The built-in
/// <see cref="Classic"/> is the original radius-4 hexagon; candidates live in
/// <c>assets/data/boards/board_[name].json</c>.
/// </remarks>
public sealed record BoardLayout
{
    /// <summary>Short identifier, e.g. <c>classic</c>.</summary>
    public required string Name { get; init; }

    /// <summary>One line for people.</summary>
    public string Description { get; init; } = "";

    /// <summary>Every playable hex, sorted by row then column.</summary>
    public required HexCoord[] Hexes { get; init; }

    /// <summary>Exactly five: 0 the neutral centre, 1–2 team A's home towers, 3–4 team B's.</summary>
    public required HexCoord[] Towers { get; init; }

    /// <summary>Team A's nexus hexes (one HP pool).</summary>
    public required HexCoord[] NexusA { get; init; }

    /// <summary>Team A's start hex per role, top to support, left to right.</summary>
    public required HexCoord[] StartA { get; init; }

    /// <summary>Team A's off-board spawn hex per role.</summary>
    public required HexCoord[] SpawnA { get; init; }

    /// <summary>Lane hexes. Presentation only: no rule reads zones.</summary>
    public HexCoord[] Lanes { get; init; } = [];

    /// <summary>Jungle hexes. Presentation only.</summary>
    public HexCoord[] Jungle { get; init; } = [];

    /// <summary>The original board: a hexagon of radius 4, 61 hexes, front lines 8 apart.</summary>
    public static readonly BoardLayout Classic = BuildClassic();

    /// <summary>Loads <c>assets/data/boards/board_[name].json</c>, or returns <see cref="Classic"/>.</summary>
    public static BoardLayout Load(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "classic") return Classic;
        string champions = Content.ContentLoader.FindChampionDirectory();
        string path = Path.Combine(Path.GetDirectoryName(champions)!, "boards", $"board_{name}.json");
        if (!File.Exists(path)) throw new Content.ContentException($"Board layout '{name}' not found at {path}.");
        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>Parses and validates a layout file.</summary>
    public static BoardLayout Parse(string json, string source)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        HexCoord[] Hexes(string key, bool required = true)
        {
            if (!root.TryGetProperty(key, out JsonElement arr))
            {
                if (required) throw new Content.ContentException($"{source}: missing '{key}'.");
                return [];
            }

            return arr.EnumerateArray().Select(e => new HexCoord(e[0].GetInt32(), e[1].GetInt32())).ToArray();
        }

        var layout = new BoardLayout
        {
            Name = root.GetProperty("name").GetString() ?? "",
            Description = root.TryGetProperty("description", out JsonElement d) ? d.GetString() ?? "" : "",
            Hexes = Hexes("hexes").OrderBy(h => h.R).ThenBy(h => h.Q).ToArray(),
            Towers = Hexes("towers"),
            NexusA = Hexes("nexusA"),
            StartA = Hexes("startA"),
            SpawnA = Hexes("spawnA"),
            Lanes = Hexes("lanes", required: false),
            Jungle = Hexes("jungle", required: false),
        };
        layout.Validate(source);
        return layout;
    }

    /// <summary>Throws unless the layout is complete and mirror-symmetric.</summary>
    public void Validate(string source)
    {
        var set = Hexes.ToHashSet();
        void Fail(string why) => throw new Content.ContentException($"{source}: {why}");

        if (set.Count != Hexes.Length) Fail("duplicate hexes.");
        if (set.Any(h => !set.Contains(Hex.Mirror(h)))) Fail("hexes are not mirror-symmetric (ADR-0005).");
        if (Towers.Length != 5 || Towers.Distinct().Count() != 5 || Towers.Any(t => !set.Contains(t))) Fail("needs five distinct playable towers.");
        if (Hex.Mirror(Towers[0]) != Towers[0]) Fail("the centre tower must lie on the mirror axis (row 0).");
        if (!new[] { Hex.Mirror(Towers[1]), Hex.Mirror(Towers[2]) }.ToHashSet().SetEquals([Towers[3], Towers[4]])) Fail("towers 3–4 must mirror towers 1–2.");
        if (Towers[1].R >= 0 || Towers[2].R >= 0) Fail("team A's home towers must be on A's half (row < 0).");
        if (NexusA.Length is < 1 or > 4 || NexusA.Any(h => !set.Contains(h) || h.R >= 0 || Towers.Contains(h))) Fail("nexus needs 1–4 playable hexes on A's half, not on a tower.");
        if (StartA.Length != 5 || StartA.Distinct().Count() != 5 || StartA.Any(h => !set.Contains(h) || h.R >= 0 || Towers.Contains(h))) Fail("needs five distinct start hexes on A's half, not on towers.");
        for (int i = 1; i < 5; i++)
        {
            if (Board.File(StartA[i]) <= Board.File(StartA[i - 1])) Fail("start hexes must run top to support, left to right.");
        }

        if (SpawnA.Length != 5 || SpawnA.Distinct().Count() != 5 || SpawnA.Any(set.Contains)) Fail("needs five distinct off-board spawn hexes.");
        foreach (HexCoord s in SpawnA)
        {
            bool touches = false;
            foreach (HexCoord dir in Hex.Directions) touches |= set.Contains(s + dir);
            if (!touches) Fail($"spawn {s} does not touch the board.");
        }
    }

    private static BoardLayout BuildClassic()
    {
        var hexes = new List<HexCoord>();
        for (int r = -4; r <= 4; r++)
        {
            for (int q = -4; q <= 4; q++)
            {
                var h = new HexCoord(q, r);
                if (Hex.InBoard(h, 4)) hexes.Add(h);
            }
        }

        return new BoardLayout
        {
            Name = "classic",
            Description = "The original board: a hexagon of radius 4. 61 hexes, front lines 8 rows apart.",
            Hexes = hexes.ToArray(),
            Towers = [new(0, 0), new(0, -2), new(2, -2), new(0, 2), new(-2, 2)],
            NexusA = [new(1, -4), new(2, -4), new(3, -4)],
            StartA = [new(0, -4), new(1, -4), new(2, -4), new(3, -4), new(4, -4)],
            SpawnA = [new(0, -5), new(1, -5), new(2, -5), new(3, -5), new(4, -5)],
            Lanes = hexes.Where(h => h.Q == 0 || h.Q + h.R == 0).ToArray(),
            Jungle = hexes.Where(h => Math.Abs(2 * h.Q + h.R) >= 5).ToArray(),   // |file| ≥ 5; not via Board, which is still initialising
        };
    }
}
