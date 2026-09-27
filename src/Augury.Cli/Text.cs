using Augury.Sim;
using Augury.Sim.Content;

namespace Augury.Cli;

/// <summary>ANSI colour, switchable off with <c>--no-color</c> or <c>NO_COLOR</c>.</summary>
internal static class Ansi
{
    public static bool On = Environment.GetEnvironmentVariable("NO_COLOR") is null;

    public static string A(string s) => Wrap(s, "36;1");        // team A: bright cyan
    public static string B(string s) => Wrap(s, "31;1");        // team B: bright red
    public static string Team(Sim.Team t, string s) => t == Sim.Team.A ? A(s) : t == Sim.Team.B ? B(s) : s;
    public static string Dim(string s) => Wrap(s, "2");
    public static string Bold(string s) => Wrap(s, "1");
    public static string Hi(string s) => Wrap(s, "33;1");       // highlights: yellow
    public static string Green(string s) => Wrap(s, "32");
    public static string Purple(string s) => Wrap(s, "35");

    private static string Wrap(string s, string code) => On ? $"\u001b[{code}m{s}\u001b[0m" : s;
}

/// <summary>Human-readable descriptions of content.</summary>
internal static class Describe
{
    public static string Effect(EffectDef e) => e.Kind switch
    {
        EffectKind.Damage => $"damage ×{e.Power / 1000.0:0.##}",
        EffectKind.Heal => $"heal ×{e.Power / 1000.0:0.##}",
        EffectKind.Shield => $"shield {e.Amount}",
        EffectKind.Poison => $"poison {e.Amount}/rd for {e.Rounds}",
        EffectKind.Displace => e.Amount > 0 ? $"push {e.Amount}" : $"pull {-e.Amount}",
        EffectKind.Dash => $"dash ≤{e.Amount}",
        _ => e.Kind.ToString(),
    };

    public static string Targeting(AbilityDef a) => a.Initiative switch
    {
        <= 2 => a.Target switch
        {
            TargetRule.Ally => "free, ally",
            TargetRule.EmptyHex => "free, empty hex",
            _ => a.RangeBonus == 0 ? "free, enemy" : $"free, enemy, range {(a.RangeBonus > 0 ? "+" : "")}{a.RangeBonus}",
        },
        3 => $"rotatable {a.Pattern.Count}-hex pattern",
        _ => $"FIXED {a.Pattern.Count}-hex pattern",
    };

    public static string Sigils(AbilityDef a)
    {
        var parts = new List<string>();
        if (a.PrintedSigil >= 0) parts.Add($"sigil {Game.SigilName(a.PrintedSigil)}");
        if (a.SlotSigil >= 0) parts.Add($"slot {Game.SigilName(a.SlotSigil)}");
        return parts.Count == 0 ? "" : " · " + string.Join(", ", parts);
    }

    public static string Ability(AbilityDef a) =>
        $"{a.Name} [init {a.Initiative}, cd {a.Cooldown}] {Targeting(a)} — {string.Join(" + ", a.Effects.Select(Effect))}"
        + Sigils(a)
        + $" · molds +{a.MoldUp} −{a.MoldDown}";

    public static string Instruction(OpeningInstruction i) => i.Kind == InstructionKind.Move
        ? $"{i.Role.ToString().ToLowerInvariant()} {Directions.Names[i.Direction]}"
        : $"beacon {Game.SigilName(i.Sigil)} on {i.Role.ToString().ToLowerInvariant()}";

    public static string Opening(AbilityDef a) => string.Join(", ", a.Opening.Select(Instruction));

    public static string Passive(PassiveDef p) => $"{p.Name}: {p.Trigger} → {p.Effect} {p.Amount}"
        + (p.Effect == PassiveEffect.EmpowerSelf ? $" {p.Stat}" : "");
}
