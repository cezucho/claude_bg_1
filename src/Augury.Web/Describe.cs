using Augury.Sim;
using Augury.Sim.Content;

namespace Augury.Web;

/// <summary>Plain-language descriptions of content for tooltips and the draft screen.</summary>
public static class Describe
{
    /// <summary>One effect, e.g. <c>damage ×1.23</c>.</summary>
    public static string Effect(EffectDef e) => e.Kind switch
    {
        EffectKind.Damage => $"damage ×{e.Power / 1000.0:0.##}",
        EffectKind.Heal => $"heal ×{e.Power / 1000.0:0.##}",
        EffectKind.Shield => $"shield {e.Amount}",
        EffectKind.Poison => $"poison {e.Amount}/round for {e.Rounds}",
        EffectKind.Displace => e.Amount > 0 ? $"push {e.Amount}" : $"pull {-e.Amount}",
        EffectKind.Dash => $"dash up to {e.Amount}",
        _ => e.Kind.ToString(),
    };

    /// <summary>All of an ability's effects.</summary>
    public static string Effects(AbilityDef a) => string.Join(" + ", a.Effects.Select(Effect));

    /// <summary>How the ability is aimed, by initiative tier.</summary>
    public static string Targeting(AbilityDef a) => a.Initiative switch
    {
        <= 2 => a.Target switch
        {
            TargetRule.Ally => "free aim · ally",
            TargetRule.EmptyHex => "free aim · empty hex",
            _ => a.RangeBonus == 0 ? "free aim · enemy" : $"free aim · enemy · range {(a.RangeBonus > 0 ? "+" : "")}{a.RangeBonus}",
        },
        3 => $"rotatable {a.Pattern.Count}-hex pattern",
        _ => $"fixed {a.Pattern.Count}-hex pattern",
    };

    /// <summary>The molding line, e.g. <c>+POW 25 / −ARM 25</c>.</summary>
    public static string Mold(AbilityDef a) =>
        $"+{a.MoldUp.ToString().ToUpperInvariant()} {a.MoldUpDelta} / −{a.MoldDown.ToString().ToUpperInvariant()} {a.MoldDownDelta} (permille)";

    /// <summary>One opening instruction.</summary>
    public static string Instruction(OpeningInstruction i) => i.Kind == InstructionKind.Move
        ? $"{i.Role.ToString().ToLowerInvariant()} → {Directions.Names[i.Direction]}"
        : $"beacon {Game.SigilName(i.Sigil)} under {i.Role.ToString().ToLowerInvariant()}";

    /// <summary>A passive.</summary>
    public static string Passive(PassiveDef p)
    {
        string when = p.Trigger switch
        {
            PassiveTrigger.OnDamaged => "When damaged by an enemy",
            PassiveTrigger.OnEnemyEntersReach => "When an enemy moves into reach",
            PassiveTrigger.OnAllyDies => "When an ally dies",
            PassiveTrigger.OnRoundClose => "At every round close",
            _ => p.Trigger.ToString(),
        };
        string what = p.Effect switch
        {
            PassiveEffect.Retaliate => $"deal {p.Amount} back to an adjacent attacker",
            PassiveEffect.Strike => $"deal {p.Amount} to that enemy",
            PassiveEffect.HealSelf => $"heal {p.Amount}",
            PassiveEffect.ShieldSelf => $"gain {p.Amount} shield",
            PassiveEffect.EmpowerSelf => $"permanently gain {p.Amount} {p.Stat.ToString().ToUpperInvariant()} (permille)",
            _ => p.Effect.ToString(),
        };
        return $"{when}: {what}.";
    }
}
