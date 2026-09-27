using System.Text.Json;

namespace Augury.Sim;

/// <summary>
/// Every tunable rule number, loaded from <c>assets/data/rules_config.json</c>. Values marked ⚠ in
/// <c>design/mvp-rules.md</c> live here so the balance pass edits data, not code.
/// </summary>
public sealed record RulesConfig
{
    /// <summary>Score that ends the match at round close (D-001).</summary>
    public int TargetScore { get; init; } = 50;

    /// <summary>Safety cap on rounds (D-018).</summary>
    public int RoundCap { get; init; } = 30;

    /// <summary>Points to the other team per death (D-001).</summary>
    public int KillPoints { get; init; } = 3;

    /// <summary>Points per owned tower at each round close (D-001).</summary>
    public int TowerPoints { get; init; } = 1;

    /// <summary>Tower HP; resets on capture.</summary>
    public int TowerHp { get; init; } = 24;

    /// <summary>Nexus HP, one pool across three hexes.</summary>
    public int NexusHp { get; init; } = 60;

    /// <summary>Damage a tower deals to each adjacent enemy in the status phase (D-024).</summary>
    public int TowerShot { get; init; } = 2;

    /// <summary>Basic-attack base damage before POW and ARM (D-008).</summary>
    public int BasicBase { get; init; } = 2;

    /// <summary>Ability base damage, ladder F3's <c>base_power</c>.</summary>
    public int AbilityBase { get; init; } = 3;

    /// <summary>Permille added to the defender divisor per defender (D-005).</summary>
    public int DefenderWeight { get; init; } = 500;

    /// <summary>POW a Dying champion reads, permille of normal (D-007).</summary>
    public int DyingPowPermille { get; init; } = 500;

    /// <summary>Enemy basic attacks a beacon survives (D-010).</summary>
    public int BeaconDurability { get; init; } = 2;

    /// <summary>Respawn timer = base + round / every (D-016).</summary>
    public int RespawnBase { get; init; } = 1;

    /// <summary>Rounds per extra respawn round.</summary>
    public int RespawnEvery { get; init; } = 8;

    /// <summary>Basics per team per half.</summary>
    public int BasicsPerHalf { get; init; } = 2;

    /// <summary>Whether friendly champions block movement (D-017).</summary>
    public bool FriendliesBlock { get; init; } = true;

    /// <summary>
    /// How many of its own home towers a team must have lost before its nexus can be
    /// damaged: 2 = both (the original D-002), 1 = either, 0 = always open.
    /// </summary>
    public int NexusGateTowers { get; init; } = 2;

    /// <summary>Team that opens round 1; the other places first in the opening (D-013).</summary>
    public string RoundOneOpener { get; init; } = "B";

    /// <summary>Loads from JSON; any omitted field keeps its default.</summary>
    public static RulesConfig Load(string path) =>
        JsonSerializer.Deserialize<RulesConfig>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip })
        ?? new RulesConfig();

    /// <summary>Loads <c>assets/data/rules_config.json</c> found by walking up from the start directory.</summary>
    public static RulesConfig LoadDefault()
    {
        string champions = Content.ContentLoader.FindChampionDirectory();
        string path = Path.Combine(Path.GetDirectoryName(champions)!, "rules_config.json");
        return File.Exists(path) ? Load(path) : new RulesConfig();
    }
}
