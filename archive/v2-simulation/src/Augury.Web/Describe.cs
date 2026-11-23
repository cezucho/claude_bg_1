using Augury.Sim;
using Augury.Sim.Content;

namespace Augury.Web;

/// <summary>Plain-language descriptions of content for tooltips and the draft screen.</summary>
public static class Describe
{
    /// <summary>One effect, e.g. <c>damage ×1.23</c>.</summary>
    public static string Effect(EffectDef e) => EffectText.Of(e);

    /// <summary>All of an ability's effects.</summary>
    public static string Effects(AbilityDef a) => EffectText.Effects(a);

    /// <summary>How the ability is aimed.</summary>
    public static string Targeting(AbilityDef a) => a.IsFree
        ? a.Target switch
        {
            TargetRule.Ally => "free aim · ally",
            TargetRule.EmptyHex => "free aim · empty hex",
            TargetRule.Self => "self",
            TargetRule.Structure => "a tower or open nexus in range",
            TargetRule.TeleportHex => "beside a friendly tower or beacon, from anywhere",
            _ => a.RangeBonus == 0 ? "free aim · enemy" : $"free aim · enemy · range {(a.RangeBonus > 0 ? "+" : "")}{a.RangeBonus}",
        }
        : a.Initiative == 3 ? $"rotatable {a.Pattern.Count}-hex pattern" : $"fixed {a.Pattern.Count}-hex pattern";

    /// <summary>The molding line, e.g. <c>+POW 25 / −ARM 25</c>.</summary>
    public static string Mold(AbilityDef a) =>
        $"+{a.MoldUp.ToString().ToUpperInvariant()} {a.MoldUpDelta} / −{a.MoldDown.ToString().ToUpperInvariant()} {a.MoldDownDelta} (permille)";

    /// <summary>One opening instruction.</summary>
    public static string Instruction(OpeningInstruction i) => EffectText.Instruction(i);

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
