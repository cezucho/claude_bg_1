using Augury.Sim.Content;

namespace Augury.Sim.Tests.Content;

/// <summary>Schema load-time invariants (Champion &amp; Ability Schema rule 9).</summary>
public class ContentLoaderTests
{
    private static ContentDb Roster() => ContentLoader.LoadDirectory(ContentLoader.FindChampionDirectory());

    [Fact]
    public void ShippedRoster_LoadsAndCoversEveryRole()
    {
        ContentDb db = Roster();
        foreach (Role role in Enum.GetValues<Role>())
        {
            Assert.NotEmpty(db.ForRole(role));
        }
    }

    [Fact]
    public void ShippedRoster_SigilRatesMatchTheMeasuredTarget()
    {
        // sigils-and-beacons.md rule 8: 3 printed, 5 slotted, 12 plain across one team.
        ContentDb db = Roster();
        var team = Enum.GetValues<Role>().Select(r => db.Champions[db.ForRole(r).First()]).ToList();
        var all = team.SelectMany(c => c.Abilities).ToList();
        Assert.Equal(3, all.Count(a => a.PrintedSigil >= 0));
        Assert.Equal(5, all.Count(a => a.SlotSigil >= 0));
    }

    [Fact]
    public void InitiativeTrade_AdjustsTheDeclaredStat()
    {
        // Stalker's kit sums to 9, so its trade stat (POW) gains 150.
        ContentDb db = Roster();
        ChampionDef stalker = db.Champions[db.IndexOf("stalker")];
        Assert.Equal(9, stalker.Abilities.Sum(a => a.Initiative));
        Assert.Equal(1100 + ContentLoader.StatTradeRate, stalker.Base(Stat.Pow));
    }

    [Theory]
    [InlineData("\"moldUp\": [\"pow\", 25]", "cross rule")]
    [InlineData("\"initiative\": 1, \"cooldown\": 9", "cooldown")]
    public void InvalidAbility_FailsLoudly(string replacement, string expectedFragment)
    {
        string json = File.ReadAllText(Path.Combine(ContentLoader.FindChampionDirectory(), "1-warden.json"));
        string broken = replacement.StartsWith("\"moldUp\"")
            ? json.Replace("\"moldUp\": [\"arm\", 25]", replacement)
            : json.Replace("\"initiative\": 1, \"cooldown\": 1", replacement);

        var ex = Assert.Throws<ContentException>(() => ContentLoader.ParseChampion(broken));
        Assert.Contains(expectedFragment, ex.Message);
    }

    [Fact]
    public void OpeningInstruction_ParsesBothKinds()
    {
        OpeningInstruction m = ContentLoader.ParseInstruction("move jungle forward-left");
        Assert.Equal(InstructionKind.Move, m.Kind);
        Assert.Equal(Role.Jungle, m.Role);
        Assert.Equal(4, m.Direction);

        OpeningInstruction b = ContentLoader.ParseInstruction("beacon support 2");
        Assert.Equal(InstructionKind.PlaceBeacon, b.Kind);
        Assert.Equal(2, b.Sigil);
    }
}
